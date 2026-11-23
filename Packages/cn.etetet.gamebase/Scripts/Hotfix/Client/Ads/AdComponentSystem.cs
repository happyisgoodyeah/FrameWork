using System;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(AdComponent))]
    [FriendOf(typeof(AdComponent))]
    public static partial class AdComponentSystem
    {
        /// <summary>
        /// 在客户端宿主场景安装独占指定渠道的广告组件
        /// </summary>
        /// <param name="scene">广告组件所属场景</param>
        /// <param name="provider">项目选定的渠道适配器</param>
        /// <param name="placementBindings">逻辑广告位到渠道广告单元的映射</param>
        /// <returns>等待项目初始化的广告组件</returns>
        public static AdComponent InstallAds(this Scene scene, IAdProvider provider, IReadOnlyDictionary<string, string> placementBindings)
        {
            if (scene.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(scene));
            }

            if (scene.GetComponent<AdComponent>() != null)
            {
                throw new InvalidOperationException("当前场景已安装广告组件");
            }

            AdService service = new(provider, placementBindings, scene.Fiber().ThreadSynchronizationContext);
            return scene.AddComponent<AdComponent, AdService>(service);
        }

        /// <summary>
        /// 接管已完成配置校验的广告服务
        /// </summary>
        /// <param name="self">广告组件</param>
        /// <param name="service">已绑定所属 Fiber 的服务</param>
        [EntitySystem]
        private static void Awake(this AdComponent self, AdService service)
        {
            self.Service = service;
        }

        /// <summary>
        /// 驱动已关闭广告的奖励确认时限
        /// </summary>
        /// <param name="self">广告组件</param>
        [EntitySystem]
        private static void LateUpdate(this AdComponent self)
        {
            self.Service.Update();
        }

        /// <summary>
        /// 释放广告服务与渠道资源
        /// </summary>
        /// <param name="self">广告组件</param>
        [EntitySystem]
        private static void Destroy(this AdComponent self)
        {
            AdService service = self.Service;
            self.Service = null;
            service.Dispose();
        }

        /// <summary>
        /// 初始化广告渠道
        /// </summary>
        /// <param name="self">广告组件</param>
        /// <returns>当前调用者的初始化结果</returns>
        public static ETTask<AdOperationResult> InitializeAsync(this AdComponent self)
        {
            return self.Service.InitializeAsync();
        }

        /// <summary>
        /// 查询指定广告位的可用性
        /// </summary>
        /// <param name="self">广告组件</param>
        /// <param name="placementId">项目定义的逻辑广告位</param>
        /// <returns>当前广告位状态</returns>
        public static AdAvailability GetAvailability(this AdComponent self, string placementId)
        {
            return self.Service.GetAvailability(placementId);
        }

        /// <summary>
        /// 预加载指定广告位的激励广告
        /// </summary>
        /// <param name="self">广告组件</param>
        /// <param name="placementId">项目定义的逻辑广告位</param>
        /// <returns>当前调用者的加载结果</returns>
        public static ETTask<AdOperationResult> PreloadAsync(this AdComponent self, string placementId)
        {
            return self.Service.PreloadAsync(placementId);
        }

        /// <summary>
        /// 展示当前可用的激励广告并等待结论
        /// </summary>
        /// <param name="self">广告组件</param>
        /// <param name="placementId">项目定义的逻辑广告位</param>
        /// <returns>广告展示结果与渠道确认的奖励资格</returns>
        public static ETTask<RewardedAdResult> ShowRewardedAsync(this AdComponent self, string placementId)
        {
            return self.Service.ShowRewardedAsync(placementId);
        }
    }
}
