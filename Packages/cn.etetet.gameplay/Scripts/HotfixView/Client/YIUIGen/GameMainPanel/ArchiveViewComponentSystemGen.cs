using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [FriendOf(typeof(YIUIChild))]
    [FriendOf(typeof(YIUIWindowComponent))]
    [FriendOf(typeof(YIUIViewComponent))]
    [EntitySystemOf(typeof(ArchiveViewComponent))]
    public static partial class ArchiveViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ArchiveViewComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this ArchiveViewComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this ArchiveViewComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();
            self.u_UIWindow = self.UIBase.GetComponent<YIUIWindowComponent>();
            self.u_UIView = self.UIBase.GetComponent<YIUIViewComponent>();
            self.UIWindow.WindowOption = EWindowOption.None;
            self.UIView.ViewWindowType = EViewWindowType.View;
            self.UIView.StackOption = EViewStackOption.VisibleTween;

            self.u_ComU_SelectPuzzleImage = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Image>("u_ComU_SelectPuzzleImage");
            self.u_ComU_SelectPuzzleNameText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComU_SelectPuzzleNameText");
            self.u_ComU_SelectPuzzleDescText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComU_SelectPuzzleDescText");
            self.u_ComU_UnlockCount = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComU_UnlockCount");
            self.u_ComU_BackButton = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComU_BackButton");
            self.u_EventU_BackButtonClick = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventU_BackButtonClick");
            self.u_EventU_BackButtonClickHandle = self.u_EventU_BackButtonClick.Add(self,ArchiveViewComponent.OnEventU_BackButtonClickInvoke);

        }
    }
}
