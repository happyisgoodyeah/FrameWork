namespace ET.Client
{
    /// <summary>
    /// Puzzle 数据实体创建完成后，为其创建对应的 PuzzleView。
    /// </summary>
    [Event(SceneType.Current)]
    public class AfterCreatePuzzle_CreatePuzzleView : AEvent<Scene, AfterCreatePuzzle>
    {
        /// <summary>
        /// 创建拼图的表现和拖拽组件，并为已有解谜模型绑定场景表现。
        /// </summary>
        protected override async ETTask Run(Scene scene, AfterCreatePuzzle args)
        {
            Puzzle puzzle = args.Puzzle;
            if (scene.IsDisposed || puzzle == null || puzzle.IsDisposed || puzzle.GetComponent<PuzzleView>() != null)
            {
                return;
            }

            EntityRef<Puzzle> puzzleRef = puzzle;
            PuzzleConfig puzzleConfig = PuzzleCoreHelper.GetPuzzleConfig(puzzle.PuzzleConfigId);
            string assetLocation = ResourceLocationHelper.ToAssetLocation(puzzleConfig.PrefabPath);
            UnityEngine.GameObject prefab = await scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<UnityEngine.GameObject>(assetLocation);
            puzzle = puzzleRef;
            if (puzzle == null || puzzle.GetComponent<PuzzleView>() != null)
            {
                return;
            }

            puzzle.AddComponent<PuzzleView, UnityEngine.GameObject>(prefab);
            PuzzleRevealComponent reveal = puzzle.GetComponent<PuzzleRevealComponent>();
            if (reveal != null)
            {
                LevelView levelView = puzzle.GetParent<Grid>().GetComponent<LevelView>();
                reveal.AddComponent<PuzzleRevealView, UnityEngine.GameObject>(levelView.SceneObject);
            }

            if (puzzle.GetComponent<PuzzleDragComponent>() == null)
            {
                puzzle.AddComponent<PuzzleDragComponent>();
            }
        }
    }
}
