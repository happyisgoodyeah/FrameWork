using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.Common)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class ArchiveItemContentComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "GameMainPanel";
        public const string ResName = "ArchiveItemContent";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<ET.Client.ArchiveItemComponent> u_UIWhite3;
        public ET.Client.ArchiveItemComponent UIWhite3 => u_UIWhite3;
        public EntityRef<ET.Client.ArchiveItemComponent> u_UIWhite2;
        public ET.Client.ArchiveItemComponent UIWhite2 => u_UIWhite2;
        public EntityRef<ET.Client.ArchiveItemComponent> u_UIWhite1;
        public ET.Client.ArchiveItemComponent UIWhite1 => u_UIWhite1;

    }
}