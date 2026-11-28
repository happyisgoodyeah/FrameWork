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
    [EntitySystemOf(typeof(ArchiveItemContentComponent))]
    public static partial class ArchiveItemContentComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ArchiveItemContentComponent self)
        {
        }

        [EntitySystem]
        private static void YIUIBind(this ArchiveItemContentComponent self)
        {
            self.UIBind();
        }

        private static void UIBind(this ArchiveItemContentComponent self)
        {
            self.u_UIBase = self.GetParent<YIUIChild>();

            self.u_UIWhite3 = self.UIBase.CDETable.FindUIOwner<ET.Client.ArchiveItemComponent>("White3");
            self.u_UIWhite2 = self.UIBase.CDETable.FindUIOwner<ET.Client.ArchiveItemComponent>("White2");
            self.u_UIWhite1 = self.UIBase.CDETable.FindUIOwner<ET.Client.ArchiveItemComponent>("White1");

        }
    }
}
