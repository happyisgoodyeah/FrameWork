namespace ET.Client
{
    /// <summary>
    /// Grid 数据实体创建完成后，为其创建对应的 GridView。
    /// </summary>
    [Event(SceneType.Current)]
    public class AfterCreateGrid_CreateGridView : AEvent<Scene, AfterCreateGrid>
    {
        private const long SceneReadyTimeoutMilliseconds = 30000;

        /// <summary>
        /// 等待 Unity 场景就绪后加载 GridView，取消时不再创建表现
        /// </summary>
        protected override async ETTask Run(Scene scene, AfterCreateGrid args)
        {
            Grid grid = args.Grid;
            if (scene.IsDisposed || grid == null || grid.IsDisposed || grid.GetComponent<GridView>() != null)
            {
                return;
            }

            EntityRef<Grid> gridRef = grid;
            EntityRef<Scene> sceneRef = scene;
            long deadline = TimeInfo.Instance.ClientFrameTime() + SceneReadyTimeoutMilliseconds;
            while (!scene.IsPuzzleViewReady())
            {
                if (TimeInfo.Instance.ClientFrameTime() >= deadline)
                {
                    throw new System.TimeoutException($"puzzle Unity scene did not become ready: {scene.Name}");
                }

                await scene.Root().GetComponent<TimerComponent>().WaitFrameAsync();
                scene = sceneRef;
                grid = gridRef;
                if (scene == null || grid == null)
                {
                    return;
                }
            }

            GridConfig gridConfig = PuzzleCoreHelper.GetGridConfig(grid.GridConfigId);
            string assetLocation = ResourceLocationHelper.ToAssetLocation(gridConfig.PrefabPath);
            UnityEngine.GameObject prefab = await scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<UnityEngine.GameObject>(assetLocation);
            grid = gridRef;
            if (grid == null || grid.GetComponent<GridView>() != null)
            {
                return;
            }

            grid.AddComponent<GridView, UnityEngine.GameObject, UnityEngine.Vector3, float>(prefab, UnityEngine.Vector3.zero, PuzzleLayoutConst.GridCellSize);
        }
    }

    /// <summary>
    /// 检查初始化阶段的必需表现，避免广播异常被记录后仍把关卡标记为就绪
    /// </summary>
    [Invoke]
    public class PuzzleViewCreationCheckHandler : AInvokeHandler<PuzzleViewCreationCheck>
    {
        /// <summary>
        /// 验证实体的场景对象和交互结构，缺失时向创建主链抛出异常
        /// </summary>
        /// <param name="args">当前阶段刚完成创建的实体</param>
        public override void Handle(PuzzleViewCreationCheck args)
        {
            bool ready;
            switch (args.Entity)
            {
                case Grid grid:
                    GridView gridView = grid.GetComponent<GridView>();
                    ready = gridView != null && gridView.IsInitialized && gridView.GameObject != null && gridView.PuzzleRoot != null &&
                        gridView.SlotRoot != null && gridView.CompositeCollider2D != null;
                    break;
                case Puzzle puzzle:
                    PuzzleView puzzleView = puzzle.GetComponent<PuzzleView>();
                    ready = puzzleView != null && puzzleView.IsInitialized && puzzleView.GameObject != null && puzzleView.SpriteRenderer != null &&
                        puzzleView.BodyCollider2D != null && puzzle.GetComponent<PuzzleDragComponent>() != null;
                    break;
                case Slot slot:
                    SlotView slotView = slot.GetComponent<SlotView>();
                    ready = slotView != null && slotView.IsInitialized && slotView.GameObject != null && slotView.Transform != null &&
                        (!slot.IsGridSlot() || slotView.Collider2D != null);
                    break;
                default:
                    throw new System.InvalidOperationException("unexpected puzzle view creation target");
            }

            if (!ready)
            {
                throw new System.InvalidOperationException($"required puzzle view was not created: {args.Entity.GetType().Name} id={args.Entity.Id}");
            }
        }
    }
}
