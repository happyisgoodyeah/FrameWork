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
    [EntitySystemOf(typeof(ArchiveItemComponent))]
    public static partial class ArchiveItemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ArchiveItemComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this ArchiveItemComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this ArchiveItemComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_ComU_Button = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Button>("u_ComU_Button");
            self.u_ComU_NameText = self.UIBase.ComponentTable.FindComponent<TMPro.TextMeshProUGUI>("u_ComU_NameText");
            self.u_ComU_PuzzleImage = self.UIBase.ComponentTable.FindComponent<UnityEngine.UI.Image>("u_ComU_PuzzleImage");
            self.u_ComU_Select = self.UIBase.ComponentTable.FindComponent<UnityEngine.RectTransform>("u_ComU_Select");
            self.u_EventU_ButtonClick = self.UIBase.EventTable.FindEvent<UITaskEventP0>("u_EventU_ButtonClick");
            self.u_EventU_ButtonClickHandle = self.u_EventU_ButtonClick.Add(self,ArchiveItemComponent.OnEventU_ButtonClickInvoke);

        }
    }
}
