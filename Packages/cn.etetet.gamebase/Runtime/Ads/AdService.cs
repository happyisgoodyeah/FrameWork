using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ET
{
    /// <summary>
    /// 在所属 Fiber 上管理广告状态，渠道回调统一排入该 Fiber
    /// </summary>
    public sealed class AdService : IDisposable
    {
        private sealed class AdUnit
        {
            public string Id;
            public IRewardedAd ReadyAd;
            public LoadRequest Loading;
        }

        private sealed class LoadRequest
        {
            public readonly List<ETTask<AdOperationResult>> Waiters = new();
            public bool CallbackReceived;
        }

        private sealed class ShowRequest
        {
            public Guid Id;
            public IRewardedAd Ad;
            public ETTask<RewardedAdResult> Completion;
            public AdRewardStatus RewardStatus;
            public long? ClosedAt;
        }

        private readonly IAdProvider provider;
        private readonly ThreadSynchronizationContext context;
        private readonly Dictionary<string, AdUnit> placements = new(StringComparer.Ordinal);
        private readonly Dictionary<string, AdUnit> adUnits = new(StringComparer.Ordinal);
        private readonly Queue<ShowRequest> closedShows = new();
        private readonly double rewardTimeoutTicks;
        private List<ETTask<AdOperationResult>> initialization;
        private ShowRequest activeShow;
        private bool initialized;
        private bool disposed;

        /// <summary>
        /// 创建服务并固定本次运行使用的广告位映射
        /// </summary>
        /// <param name="provider">创建成功后由服务独占的渠道适配器</param>
        /// <param name="placementBindings">逻辑广告位到渠道广告单元的映射</param>
        /// <param name="context">所属 Fiber 的执行上下文</param>
        public AdService(IAdProvider provider, IReadOnlyDictionary<string, string> placementBindings, ThreadSynchronizationContext context)
        {
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            TimeSpan rewardTimeout = provider.RewardConfirmationTimeout;
            if (rewardTimeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(provider), "奖励确认时限须大于或等于零");
            }

            this.rewardTimeoutTicks = rewardTimeout.TotalSeconds * Stopwatch.Frequency;
            if (placementBindings == null)
            {
                throw new ArgumentNullException(nameof(placementBindings));
            }

            foreach (KeyValuePair<string, string> binding in placementBindings)
            {
                if (string.IsNullOrWhiteSpace(binding.Key) || string.IsNullOrWhiteSpace(binding.Value))
                {
                    throw new ArgumentException("广告位和渠道广告单元标识不能为空", nameof(placementBindings));
                }

                if (!this.adUnits.TryGetValue(binding.Value, out AdUnit unit))
                {
                    unit = new AdUnit { Id = binding.Value };
                    this.adUnits.Add(unit.Id, unit);
                }

                this.placements.Add(binding.Key, unit);
            }
        }

        /// <summary>
        /// 初始化渠道并合并尚未完成的初始化请求
        /// </summary>
        /// <returns>当前调用者独立等待的初始化结果</returns>
        public ETTask<AdOperationResult> InitializeAsync()
        {
            if (this.disposed)
            {
                return Completed(AdOperationResult.Disposed);
            }

            if (this.initialized)
            {
                return Completed(AdOperationResult.Success);
            }

            ETTask<AdOperationResult> waiter = ETTask<AdOperationResult>.Create();
            if (this.initialization != null)
            {
                this.initialization.Add(waiter);
                return waiter;
            }

            List<ETTask<AdOperationResult>> request = new() { waiter };
            this.initialization = request;
            try
            {
                this.provider.Initialize(success => this.context.Post(() => this.FinishInitialization(request, success)));
            }
            catch (Exception e)
            {
                Log.Error(e);
                this.context.Post(() => this.FinishInitialization(request, false));
            }

            return waiter;
        }

        /// <summary>
        /// 查询广告位当前的展示可用性
        /// </summary>
        /// <param name="placementId">项目定义的逻辑广告位</param>
        /// <returns>当前广告位状态</returns>
        public AdAvailability GetAvailability(string placementId)
        {
            if (this.disposed)
            {
                return AdAvailability.Disposed;
            }

            if (placementId == null || !this.placements.TryGetValue(placementId, out AdUnit unit))
            {
                return AdAvailability.UnknownPlacement;
            }

            if (!this.initialized)
            {
                return AdAvailability.NotInitialized;
            }

            if (this.activeShow != null)
            {
                return AdAvailability.Busy;
            }

            if (unit.ReadyAd != null)
            {
                return AdAvailability.Ready;
            }

            return unit.Loading != null ? AdAvailability.Loading : AdAvailability.NotLoaded;
        }

        /// <summary>
        /// 预加载激励广告，同一渠道广告单元共享底层加载
        /// </summary>
        /// <param name="placementId">项目定义的逻辑广告位</param>
        /// <returns>当前调用者独立等待的加载结果</returns>
        public ETTask<AdOperationResult> PreloadAsync(string placementId)
        {
            if (this.disposed)
            {
                return Completed(AdOperationResult.Disposed);
            }

            if (placementId == null || !this.placements.TryGetValue(placementId, out AdUnit unit))
            {
                return Completed(AdOperationResult.UnknownPlacement);
            }

            if (!this.initialized)
            {
                return Completed(AdOperationResult.NotInitialized);
            }

            if (unit.ReadyAd != null)
            {
                return Completed(AdOperationResult.Success);
            }

            ETTask<AdOperationResult> waiter = ETTask<AdOperationResult>.Create();
            if (unit.Loading != null)
            {
                unit.Loading.Waiters.Add(waiter);
                return waiter;
            }

            LoadRequest request = new();
            request.Waiters.Add(waiter);
            unit.Loading = request;
            try
            {
                this.provider.LoadRewardedAd(unit.Id, ad => this.context.Post(() => this.FinishLoad(unit, request, ad)));
            }
            catch (Exception e)
            {
                Log.Error(e);
                this.context.Post(() => this.FinishLoad(unit, request, null));
            }

            return waiter;
        }

        /// <summary>
        /// 消费一条已加载广告并等待展示与激励确认结果
        /// </summary>
        /// <param name="placementId">项目定义的逻辑广告位</param>
        /// <returns>展示与资格确认结果</returns>
        public ETTask<RewardedAdResult> ShowRewardedAsync(string placementId)
        {
            switch (this.GetAvailability(placementId))
            {
                case AdAvailability.Disposed:
                    return Completed(new RewardedAdResult(AdShowStatus.Disposed));
                case AdAvailability.UnknownPlacement:
                    return Completed(new RewardedAdResult(AdShowStatus.UnknownPlacement));
                case AdAvailability.Busy:
                    return Completed(new RewardedAdResult(AdShowStatus.Busy));
                case AdAvailability.Ready:
                    break;
                default:
                    return Completed(new RewardedAdResult(AdShowStatus.NotReady));
            }

            AdUnit unit = this.placements[placementId];
            ETTask<RewardedAdResult> completion = ETTask<RewardedAdResult>.Create();
            ShowRequest request = new() { Id = Guid.NewGuid(), Ad = unit.ReadyAd, Completion = completion };
            unit.ReadyAd = null;
            this.activeShow = request;
            try
            {
                request.Ad.Show(signal => this.context.Post(() => this.HandleShowSignal(request, signal)));
            }
            catch (Exception e)
            {
                Log.Error(e);
                this.context.Post(() => this.HandleShowSignal(request, AdShowSignal.Failed));
            }

            return completion;
        }

        /// <summary>
        /// 在已入队的渠道回调之后检查关闭请求的奖励等待时限
        /// </summary>
        public void Update()
        {
            if (this.closedShows.Count > 0)
            {
                ShowRequest request = this.closedShows.Peek();
                if (request.Completion == null || this.IsRewardWaitExpired(request))
                {
                    this.context.Post(this.ExpireRewards);
                }
            }
        }

        /// <summary>
        /// 结束服务，解绑渠道并向仍在等待的调用者报告释放状态
        /// </summary>
        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            List<ETTask<AdOperationResult>> initializing = this.initialization;
            this.initialization = null;
            if (initializing != null)
            {
                CompleteWaiters(initializing, AdOperationResult.Disposed);
            }

            foreach (AdUnit unit in this.adUnits.Values)
            {
                LoadRequest pending = unit.Loading;
                IRewardedAd readyAd = unit.ReadyAd;
                unit.Loading = null;
                unit.ReadyAd = null;
                Release(readyAd);
                if (pending != null)
                {
                    CompleteWaiters(pending.Waiters, AdOperationResult.Disposed);
                }
            }

            ShowRequest playing = this.activeShow;
            this.activeShow = null;
            if (playing != null)
            {
                FinishShow(playing, AdShowStatus.Disposed);
            }

            while (this.closedShows.Count > 0)
            {
                ShowRequest request = this.closedShows.Dequeue();
                if (request.Completion != null)
                {
                    FinishShow(request, AdShowStatus.Disposed);
                }
            }

            Release(this.provider);
        }

        /// <summary>
        /// 提交当前初始化结果并唤醒各调用方
        /// </summary>
        /// <param name="request">发起本次初始化时登记的等待列表</param>
        /// <param name="success">渠道初始化是否成功</param>
        private void FinishInitialization(List<ETTask<AdOperationResult>> request, bool success)
        {
            if (this.initialization != request)
            {
                return;
            }

            this.initialization = null;
            this.initialized = success;
            CompleteWaiters(request, success ? AdOperationResult.Success : AdOperationResult.Failed);
        }

        /// <summary>
        /// 接收已加载广告并完成本次加载的所有等待者
        /// </summary>
        /// <param name="unit">发起加载的广告单元</param>
        /// <param name="request">加载请求及其等待列表</param>
        /// <param name="ad">已加载广告，加载失败时为空</param>
        private void FinishLoad(AdUnit unit, LoadRequest request, IRewardedAd ad)
        {
            if (request.CallbackReceived)
            {
                return;
            }

            request.CallbackReceived = true;
            if (this.disposed)
            {
                Release(ad);
                return;
            }

            unit.Loading = null;
            unit.ReadyAd = ad;
            CompleteWaiters(request.Waiters, ad != null ? AdOperationResult.Success : AdOperationResult.Failed);
        }

        /// <summary>
        /// 推进指定展示的关闭与奖励确认状态
        /// </summary>
        /// <param name="request">回调绑定的展示请求</param>
        /// <param name="signal">渠道报告的展示或奖励状态</param>
        private void HandleShowSignal(ShowRequest request, AdShowSignal signal)
        {
            if (request.Completion == null)
            {
                return;
            }

            switch (signal)
            {
                case AdShowSignal.Closed when request.ClosedAt == null:
                    this.activeShow = null;
                    request.ClosedAt = Stopwatch.GetTimestamp();
                    if (request.RewardStatus == AdRewardStatus.Unknown)
                    {
                        this.closedShows.Enqueue(request);
                        return;
                    }

                    FinishShow(request, AdShowStatus.Closed);
                    break;
                case AdShowSignal.Failed when request.ClosedAt == null:
                    this.activeShow = null;
                    request.RewardStatus = AdRewardStatus.Unknown;
                    FinishShow(request, AdShowStatus.Failed);
                    break;
                case AdShowSignal.RewardEarned when request.RewardStatus == AdRewardStatus.Unknown:
                case AdShowSignal.RewardNotEarned when request.RewardStatus == AdRewardStatus.Unknown:
                    request.RewardStatus = signal == AdShowSignal.RewardEarned ? AdRewardStatus.Earned : AdRewardStatus.NotEarned;
                    if (request.ClosedAt != null)
                    {
                        FinishShow(request, AdShowStatus.Closed);
                    }

                    break;
            }
        }

        /// <summary>
        /// 判断已关闭的展示是否超过渠道奖励确认时限
        /// </summary>
        /// <param name="request">已记录关闭时间的展示请求</param>
        /// <returns>是否达到奖励确认时限</returns>
        private bool IsRewardWaitExpired(ShowRequest request)
        {
            return Stopwatch.GetTimestamp() - request.ClosedAt.Value >= this.rewardTimeoutTicks;
        }

        /// <summary>
        /// 按关闭顺序清理已结算请求并结束到期的等待
        /// </summary>
        private void ExpireRewards()
        {
            while (this.closedShows.Count > 0)
            {
                ShowRequest request = this.closedShows.Peek();
                if (request.Completion == null)
                {
                    this.closedShows.Dequeue();
                    continue;
                }

                if (!this.IsRewardWaitExpired(request))
                {
                    break;
                }

                this.closedShows.Dequeue();
                FinishShow(request, AdShowStatus.Closed);
            }
        }

        /// <summary>
        /// 消费展示任务并释放句柄，确保重复回调无法再次结算
        /// </summary>
        /// <param name="request">尚未完成的展示请求</param>
        /// <param name="status">最终展示状态</param>
        private static void FinishShow(ShowRequest request, AdShowStatus status)
        {
            ETTask<RewardedAdResult> completion = request.Completion;
            request.Completion = null;
            IRewardedAd ad = request.Ad;
            request.Ad = null;
            Release(ad);
            completion.SetResult(new RewardedAdResult(status, request.Id, request.RewardStatus));
        }

        /// <summary>
        /// 完成已从运行状态取出的等待列表
        /// </summary>
        /// <param name="waiters">当前操作的等待列表</param>
        /// <param name="result">分发给每个等待者的操作结果</param>
        private static void CompleteWaiters(List<ETTask<AdOperationResult>> waiters, AdOperationResult result)
        {
            foreach (ETTask<AdOperationResult> waiter in waiters)
            {
                waiter.SetResult(result);
            }

            waiters.Clear();
        }

        /// <summary>
        /// 创建当前调用者专用的已完成任务
        /// </summary>
        /// <typeparam name="T">操作结果类型</typeparam>
        /// <param name="result">任务结果</param>
        /// <returns>携带指定结果的已完成任务</returns>
        private static ETTask<T> Completed<T>(T result)
        {
            ETTask<T> task = ETTask<T>.Create();
            task.SetResult(result);
            return task;
        }

        /// <summary>
        /// 释放渠道资源并隔离第三方清理异常
        /// </summary>
        /// <param name="resource">可为空的渠道或广告实例</param>
        private static void Release(IDisposable resource)
        {
            try
            {
                resource?.Dispose();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
