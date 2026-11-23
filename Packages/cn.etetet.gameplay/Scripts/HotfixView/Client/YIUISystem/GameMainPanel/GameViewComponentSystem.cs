using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 关卡内视图逻辑
    /// </summary>
    [FriendOf(typeof(GameViewComponent))]
    public static partial class GameViewComponentSystem
    {
        /// <summary>
        /// 创建关卡主题 UI 挂点并关闭装饰的输入拦截
        /// </summary>
        /// <param name="self">关卡内视图</param>
        [EntitySystem]
        private static void YIUIInitialize(this GameViewComponent self)
        {
            GameObject themeRoot = new GameObject("ThemeUIRoot", typeof(RectTransform), typeof(CanvasGroup));
            themeRoot.layer = self.UIBase.OwnerGameObject.layer;
            self.ThemeUIRoot = themeRoot.GetComponent<RectTransform>();
            self.ThemeUIRoot.SetParent(self.UIBase.OwnerRectTransform, false);
            self.ThemeUIRoot.anchorMin = Vector2.zero;
            self.ThemeUIRoot.anchorMax = Vector2.one;
            self.ThemeUIRoot.offsetMin = Vector2.zero;
            self.ThemeUIRoot.offsetMax = Vector2.zero;
            self.ThemeUIRoot.SetAsFirstSibling();
            CanvasGroup canvasGroup = themeRoot.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            self.ThemeLevelView = default;
        }

        /// <summary>
        /// 释放当前视图承载的主题 UI 和动态挂点
        /// </summary>
        /// <param name="self">关卡内视图</param>
        [EntitySystem]
        private static void Destroy(this GameViewComponent self)
        {
            LevelView levelView = self.ThemeLevelView;
            if (levelView != null) levelView.ReleaseUI();
            if (self.ThemeUIRoot != null) UnityEngine.Object.Destroy(self.ThemeUIRoot.gameObject);
            self.ThemeUIRoot = null;
            self.ThemeLevelView = default;
        }

        /// <summary>
        /// 在关卡就绪后打开视图并挂载本关的 UI 主题
        /// </summary>
        /// <param name="self">关卡内视图</param>
        /// <returns>是否打开成功</returns>
        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this GameViewComponent self)
        {
            Grid grid = self.Root().CurrentScene().GetGrid();
            if (grid == null || !grid.IsReady)
            {
                throw new InvalidOperationException("打开关卡 UI 前必须完成关卡创建");
            }
            LevelView levelView = grid.GetComponent<LevelView>();
            levelView.ShowUI(self.ThemeUIRoot);
            self.ThemeLevelView = levelView;
            await ETTask.CompletedTask;
            return true;
        }

        /// <summary>
        /// 放弃当前关卡并返回关卡选择
        /// </summary>
        /// <param name="self">关卡内视图</param>
        [YIUIInvoke(GameViewComponent.OnEventBackBtnClickInvoke)]
        private static async ETTask OnEventBackBtnClickInvoke(this GameViewComponent self)
        {
            PuzzleLevelRuntimeHelper.ClearCurrentLevel(self.Root().CurrentScene());
            await self.YIUIRoot().OpenPanelAsync<GameMainPanelPanelComponent, EGameMainPanelPanelViewEnum>(EGameMainPanelPanelViewEnum.SelectLevelView);
        }
    }
}
