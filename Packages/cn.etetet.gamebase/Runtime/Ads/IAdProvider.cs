using System;

namespace ET
{
    /// <summary>
    /// 客户端广告渠道适配器，由一个广告服务独占，释放时停止原生回调并回收剩余资源
    /// </summary>
    public interface IAdProvider : IDisposable
    {
        /// <summary>
        /// 获取广告关闭后等待奖励确认的最长时间，渠道可按回调契约设置
        /// </summary>
        TimeSpan RewardConfirmationTimeout { get; }

        /// <summary>
        /// 初始化渠道并回传一次结果，释放时清理未完成的原生操作
        /// </summary>
        /// <param name="completed">初始化是否成功，可在任意线程通知</param>
        void Initialize(Action<bool> completed);

        /// <summary>
        /// 加载单次展示的激励广告并回传一次结果
        /// </summary>
        /// <param name="adUnitId">渠道广告单元标识</param>
        /// <param name="completed">成功时交付一个广告句柄，失败时通知空值</param>
        void LoadRewardedAd(string adUnitId, Action<IRewardedAd> completed);
    }

    /// <summary>
    /// 已加载且仅消费一次的渠道广告句柄，支持重复释放
    /// </summary>
    public interface IRewardedAd : IDisposable
    {
        /// <summary>
        /// 发起展示，收到原生关闭或打开失败时报告展示终态
        /// </summary>
        /// <param name="notify">此广告实例的回调，可在任意线程通知，允许关闭早于奖励</param>
        void Show(Action<AdShowSignal> notify);
    }

    /// <summary>
    /// 一次展示的标准化渠道回调
    /// </summary>
    public enum AdShowSignal
    {
        /// <summary>
        /// 原生全屏已关闭
        /// </summary>
        Closed,
        /// <summary>
        /// 原生全屏打开失败
        /// </summary>
        Failed,
        /// <summary>
        /// 渠道已确认取得激励资格
        /// </summary>
        RewardEarned,
        /// <summary>
        /// 渠道已明确确认未取得激励资格
        /// </summary>
        RewardNotEarned,
    }
}
