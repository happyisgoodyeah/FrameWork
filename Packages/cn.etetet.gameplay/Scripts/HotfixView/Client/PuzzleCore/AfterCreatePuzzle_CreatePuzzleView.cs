namespace ET.Client
{
    /// <summary>
    /// Puzzle 数据实体创建完成后，为其创建对应的 PuzzleView。
    /// </summary>
    [Event(SceneType.Current)]
    public class AfterCreatePuzzle_CreatePuzzleView : AEvent<Scene, AfterCreatePuzzle>
    {
        /// <summary>
        /// 在 Puzzle 数据创建完成后立即加载 PuzzleView
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

            UnityEngine.Vector3 worldPosition = PuzzleTrayHelper.ResolveTrayWorldPosition(scene, puzzle);
            puzzle.AddComponent<PuzzleView, UnityEngine.GameObject, UnityEngine.Vector3>(prefab, worldPosition);

            if (puzzle.GetComponent<PuzzleDragComponent>() == null)
            {
                puzzle.AddComponent<PuzzleDragComponent>();
            }
        }
    }
}
