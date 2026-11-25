namespace ET.Client
{
    /// <summary>
    /// 负责根据配表搭建 PuzzleCore 运行时数据
    /// </summary>
    public static class PuzzleBootstrapHelper
    {
        /// <summary>
        /// 在当前场景中创建默认 Grid、Grid Slot 和 Puzzle 数据
        /// </summary>
        /// <param name="scene">当前场景</param>
        /// <returns>创建完成的 Grid</returns>
        public static async ETTask<Grid> CreateDefaultGridAsync(Scene scene)
        {
            return await CreateGridAsync(scene, PuzzleCoreConst.DefaultGridConfigId);
        }

        /// <summary>
        /// 按指定配置创建关卡，并在必需的客户端表现全部就绪后返回
        /// </summary>
        /// <param name="scene">当前场景</param>
        /// <param name="gridConfigId">Grid 配置 id</param>
        /// <returns>创建完成的 Grid</returns>
        public static async ETTask<Grid> CreateGridAsync(Scene scene, int gridConfigId)
        {
            Grid existedGrid = scene.GetGrid();
            if (existedGrid != null)
            {
                if (!existedGrid.IsReady || existedGrid.GridConfigId != gridConfigId || existedGrid.CompletionPublished)
                {
                    throw new System.InvalidOperationException($"grid creation is not allowed in the current state: {gridConfigId}");
                }
                return existedGrid;
            }

            GridConfig gridConfig = PuzzleCoreHelper.GetGridConfig(gridConfigId);
            int width = gridConfig.X;
            int height = gridConfig.Y;
            ValidateGridConfig(gridConfig, width, height);

            Grid grid = scene.AddChildWithId<Grid, int, int>(PuzzleCoreConst.DefaultGridId, width, height);
            EntityRef<Grid> gridRef = grid;
            grid.GridConfigId = gridConfig.Id;
            try
            {
                await EventSystem.Instance.PublishAsync(scene, new AfterCreateGrid() { Grid = grid });
                grid = gridRef;
                if (grid == null)
                {
                    return null;
                }

                EventSystem.Instance.Invoke(new PuzzleViewCreationCheck() { Entity = grid });

                await CreateGridSlotsAsync(grid, gridConfig, width, height);
                grid = gridRef;
                if (grid == null)
                {
                    return null;
                }

                await CreatePuzzlesAsync(grid, gridConfig);
                grid = gridRef;
                if (grid == null)
                {
                    return null;
                }

                grid.RefreshStatistics();
                grid.IsReady = true;
                Log.Info($"create grid success: scene={scene.Name} gridConfigId={gridConfig.Id} size={width}x{height} placeable={grid.PlaceableCount} occupied={grid.OccupiedCount} puzzleCount={gridConfig.PuzzleList.Count}");
                return grid;
            }
            catch
            {
                ((Grid)gridRef)?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 根据配置创建棋盘格并等待每个槽位的表现检查完成
        /// </summary>
        /// <param name="grid">目标棋盘</param>
        /// <param name="gridConfig">当前关卡配置</param>
        /// <param name="width">棋盘列数</param>
        /// <param name="height">棋盘行数</param>
        private static async ETTask CreateGridSlotsAsync(Grid grid, GridConfig gridConfig, int width, int height)
        {
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int index = y * width + x;
                    grid.AddSlot(x, y, gridConfig.SlotList[index]);
                }
            }

            await PublishSlotsCreatedAsync(grid);
        }

        /// <summary>
        /// 根据棋盘配表创建各托盘拼图及其可选的解谜组件。
        /// </summary>
        /// <param name="grid">目标棋盘</param>
        /// <param name="gridConfig">当前关卡配置</param>
        private static async ETTask CreatePuzzlesAsync(Grid grid, GridConfig gridConfig)
        {
            EntityRef<Grid> gridRef = grid;
            PuzzleRevealRule[] revealRules = ResolveRevealRules(gridConfig);
            // 先统计每个托盘的槽位总数，供表现层把整列槽位垂直居中
            System.Collections.Generic.Dictionary<int, int> traySlotCounts = new();
            foreach (puzzle info in gridConfig.PuzzleList)
            {
                traySlotCounts.TryGetValue(info.Tray, out int count);
                traySlotCounts[info.Tray] = count + 1;
            }

            for (int index = 0; index < gridConfig.PuzzleList.Count; ++index)
            {
                grid = gridRef;
                if (grid == null)
                {
                    return;
                }

                puzzle puzzleInfo = gridConfig.PuzzleList[index];
                long puzzleEntityId = PuzzleCoreConst.DefaultPuzzleEntityIdStart + index + 1;
                traySlotCounts.TryGetValue(puzzleInfo.Tray, out int traySlotCount);
                await CreatePuzzleAsync(grid, puzzleEntityId, puzzleInfo, traySlotCount, revealRules[index]);
            }
        }

        /// <summary>
        /// 按拼图配置和托盘位置关联本关的解谜规则，并检查每条规则对应唯一实例。
        /// </summary>
        private static PuzzleRevealRule[] ResolveRevealRules(GridConfig gridConfig)
        {
            var rules = new PuzzleRevealRule[gridConfig.PuzzleList.Count];
            foreach (PuzzleRevealRule rule in gridConfig.RevealRules)
            {
                int targetIndex = -1;
                for (int i = 0; i < gridConfig.PuzzleList.Count; ++i)
                {
                    puzzle piece = gridConfig.PuzzleList[i];
                    if (piece.Id != rule.PuzzleId || piece.Tray != rule.Tray || piece.SlotIndex != rule.SlotIndex) continue;
                    if (targetIndex >= 0) throw new System.InvalidOperationException($"关卡 {gridConfig.Id} 的解谜目标重复：托盘 {rule.Tray} 槽位 {rule.SlotIndex}");
                    targetIndex = i;
                }

                if (targetIndex < 0 || rules[targetIndex] != null || rule.RequiredClicks < 1 || rule.HitPaths.Count == 0)
                {
                    throw new System.InvalidOperationException($"关卡 {gridConfig.Id} 的解谜配置无效：拼图 {rule.PuzzleId}，托盘 {rule.Tray}，槽位 {rule.SlotIndex}");
                }
                rules[targetIndex] = rule;
            }
            return rules;
        }

        /// <summary>
        /// 创建拼图、形状格和可选解锁状态，完成后再发布拼图创建事件。
        /// </summary>
        /// <param name="grid">目标棋盘</param>
        /// <param name="puzzleId">目标 Puzzle 实体 id</param>
        /// <param name="puzzleInfo">当前关卡内的 Puzzle 实例配置</param>
        /// <param name="traySlotCount">该 Puzzle 所属托盘的槽位总数</param>
        /// <param name="revealRule">本实例的点击解锁规则，普通拼图为空</param>
        /// <returns>创建完成的 Puzzle</returns>
        private static async ETTask<Puzzle> CreatePuzzleAsync(Grid grid, long puzzleId, puzzle puzzleInfo, int traySlotCount, PuzzleRevealRule revealRule)
        {
            Puzzle existedPuzzle = grid.GetPuzzle(puzzleId);
            if (existedPuzzle != null)
            {
                return existedPuzzle;
            }

            PuzzleConfig puzzleConfig = PuzzleCoreHelper.GetPuzzleConfig(puzzleInfo.Id);
            Puzzle puzzleEntity = grid.AddChildWithId<Puzzle, int>(puzzleId, puzzleConfig.Id);
            puzzleEntity.Tray = puzzleInfo.Tray;
            puzzleEntity.TraySlotIndex = puzzleInfo.SlotIndex;
            puzzleEntity.TraySlotCount = traySlotCount;
            if (revealRule != null)
            {
                puzzleEntity.AddComponent<PuzzleRevealComponent, PuzzleRevealRule>(revealRule);
            }

            foreach (System.Collections.Generic.List<int> slotOffset in puzzleConfig.SlotOffset)
            {
                ValidatePuzzleSlotOffset(puzzleConfig, slotOffset);
                puzzleEntity.AddSlot(slotOffset[0], slotOffset[1], false);
            }

            EntityRef<Puzzle> puzzleRef = puzzleEntity;
            await EventSystem.Instance.PublishAsync(grid.Scene(), new AfterCreatePuzzle() { Puzzle = puzzleEntity });
            puzzleEntity = puzzleRef;
            if (puzzleEntity == null)
            {
                return null;
            }

            EventSystem.Instance.Invoke(new PuzzleViewCreationCheck() { Entity = puzzleEntity });
            await PublishSlotsCreatedAsync(puzzleEntity);
            return puzzleRef;
        }

        /// <summary>
        /// 等待父实体下所有槽位的创建事件完成，再确认必需表现全部成功
        /// </summary>
        /// <param name="parent">持有棋盘格或形状格的父实体</param>
        private static async ETTask PublishSlotsCreatedAsync(Entity parent)
        {
            EntityRef<Entity> parentRef = parent;
            System.Collections.Generic.List<ETTask> slotCreated = new();
            foreach (Entity child in parent.Children.Values)
            {
                if (child is Slot slot)
                {
                    slotCreated.Add(EventSystem.Instance.PublishAsync(parent.Scene(), new AfterCreateSlot() { Slot = slot }));
                }
            }

            await ETTaskHelper.WaitAll(slotCreated);
            parent = parentRef;
            if (parent == null)
            {
                return;
            }

            // PublishAsync 和 WaitAll 只等待结束，成功状态由主链直接检查
            foreach (Entity child in parent.Children.Values)
            {
                if (child is Slot slot)
                {
                    EventSystem.Instance.Invoke(new PuzzleViewCreationCheck() { Entity = slot });
                }
            }
        }

        /// <summary>
        /// 校验当前 Grid 配置和尺寸定义是否一致
        /// </summary>
        /// <param name="gridConfig">要校验的 Grid 配置</param>
        /// <param name="width">当前采用的棋盘列数</param>
        /// <param name="height">当前采用的棋盘行数</param>
        private static void ValidateGridConfig(GridConfig gridConfig, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new System.Exception($"grid config size is invalid: id={gridConfig.Id} size={width}x{height}");
            }

            if (gridConfig.SlotList == null || gridConfig.SlotList.Count != width * height)
            {
                throw new System.Exception($"grid config slot list count mismatch: id={gridConfig.Id} expected={width * height} actual={gridConfig.SlotList?.Count ?? 0}");
            }

            if (gridConfig.PuzzleList == null)
            {
                throw new System.Exception($"grid config puzzle list is null: id={gridConfig.Id}");
            }
        }

        /// <summary>
        /// 校验单个 Puzzle 格偏移数据是否合法
        /// </summary>
        /// <param name="puzzleConfig">所属 Puzzle 配置</param>
        /// <param name="slotOffset">当前格子的偏移定义</param>
        private static void ValidatePuzzleSlotOffset(PuzzleConfig puzzleConfig, System.Collections.Generic.List<int> slotOffset)
        {
            if (slotOffset == null || slotOffset.Count != 2)
            {
                throw new System.Exception($"puzzle config slot offset is invalid: id={puzzleConfig.Id}");
            }
        }
    }
}
