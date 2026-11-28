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
    public partial class ArchiveItemComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize
    {
        public const string PkgName = "GameMainPanel";
        public const string ResName = "ArchiveItem";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public UnityEngine.UI.Button u_ComU_Button;
        public TMPro.TextMeshProUGUI u_ComU_NameText;
        public UnityEngine.UI.Image u_ComU_PuzzleImage;
        public UnityEngine.RectTransform u_ComU_Select;
        public UITaskEventP0 u_EventU_ButtonClick;
        public UITaskEventHandleP0 u_EventU_ButtonClickHandle;
        public const string OnEventU_ButtonClickInvoke = "ArchiveItemComponent.OnEventU_ButtonClickInvoke";

    }
}