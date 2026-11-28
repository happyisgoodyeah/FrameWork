using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{

    /// <summary>
    /// 由YIUI工具自动创建 请勿修改
    /// </summary>
    [YIUI(EUICodeType.View)]
    [ComponentOf(typeof(YIUIChild))]
    public partial class ArchiveViewComponent : Entity, IDestroy, IAwake, IYIUIBind, IYIUIInitialize, IYIUIOpen
    {
        public const string PkgName = "GameMainPanel";
        public const string ResName = "ArchiveView";

        public EntityRef<YIUIChild> u_UIBase;
        public YIUIChild UIBase => u_UIBase;
        public EntityRef<YIUIWindowComponent> u_UIWindow;
        public YIUIWindowComponent UIWindow => u_UIWindow;
        public EntityRef<YIUIViewComponent> u_UIView;
        public YIUIViewComponent UIView => u_UIView;
        public UnityEngine.UI.Image u_ComU_SelectPuzzleImage;
        public TMPro.TextMeshProUGUI u_ComU_SelectPuzzleNameText;
        public TMPro.TextMeshProUGUI u_ComU_SelectPuzzleDescText;
        public TMPro.TextMeshProUGUI u_ComU_UnlockCount;
        public UnityEngine.UI.Button u_ComU_BackButton;
        public UITaskEventP0 u_EventU_BackButtonClick;
        public UITaskEventHandleP0 u_EventU_BackButtonClickHandle;
        public const string OnEventU_BackButtonClickInvoke = "ArchiveViewComponent.OnEventU_BackButtonClickInvoke";

    }
}