using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 关卡内游戏视图
    /// </summary>
    public partial class GameViewComponent : Entity
    {
        /// <summary>当前关卡的屏幕主题装饰挂点</summary>
        public RectTransform ThemeUIRoot { get; set; }

        /// <summary>当前视图承载 UI 主题的关卡表现</summary>
        public EntityRef<LevelView> ThemeLevelView { get; set; }
    }
}
