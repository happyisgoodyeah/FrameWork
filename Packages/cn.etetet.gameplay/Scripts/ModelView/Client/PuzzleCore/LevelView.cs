using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 当前关卡的场景主题、UI 主题和落位特效资源
    /// </summary>
    [ComponentOf(typeof(Grid))]
    public class LevelView : Entity, IAwake, IDestroy
    {
        public bool IsInitialized { get; set; }
        public GameObject SceneObject { get; set; }
        public PuzzleGame.PuzzleRevealTrigger RevealTrigger { get; set; }
        public GameObject UIPrefab { get; set; }
        public GameObject UIObject { get; set; }
        public GameObject PlacementEffectPrefab { get; set; }
        public EntityRef<PuzzleView> LastPlacementView { get; set; }
    }
}
