using System;

namespace ET
{
    /// <summary>
    /// 广告初始化或预加载结果
    /// </summary>
    public enum AdOperationResult
    {
        Failed,
        Success,
        NotInitialized,
        UnknownPlacement,
        Disposed,
    }

    /// <summary>
    /// 广告位在当前时刻的可用状态
    /// </summary>
    public enum AdAvailability
    {
        NotInitialized,
        UnknownPlacement,
        NotLoaded,
        Loading,
        Ready,
        Busy,
        Disposed,
    }

    /// <summary>
    /// 全屏广告请求的展示结果
    /// </summary>
    public enum AdShowStatus
    {
        Failed,
        Closed,
        Busy,
        NotReady,
        UnknownPlacement,
        Disposed,
    }

    /// <summary>
    /// 渠道对激励资格的确认结果
    /// </summary>
    public enum AdRewardStatus
    {
        Unknown,
        NotEarned,
        Earned,
    }

    /// <summary>
    /// 激励广告的展示与奖励确认结果
    /// </summary>
    public readonly struct RewardedAdResult
    {
        public Guid RequestId { get; }
        public AdShowStatus ShowStatus { get; }
        public AdRewardStatus RewardStatus { get; }

        /// <summary>
        /// 记录展示状态和独立确认的激励资格
        /// </summary>
        /// <param name="showStatus">展示结果</param>
        /// <param name="requestId">已发起的请求标识，展示前被拒绝时为空</param>
        /// <param name="rewardStatus">渠道确认的激励资格</param>
        internal RewardedAdResult(AdShowStatus showStatus, Guid requestId = default, AdRewardStatus rewardStatus = AdRewardStatus.Unknown)
        {
            this.RequestId = requestId;
            this.ShowStatus = showStatus;
            this.RewardStatus = rewardStatus;
        }
    }
}
