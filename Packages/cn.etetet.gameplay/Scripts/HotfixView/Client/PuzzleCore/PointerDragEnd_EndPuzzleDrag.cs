using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 在通用拖拽结束事件中结算 Puzzle 拖拽
    /// </summary>
    [Event(SceneType.Current)]
    public class PointerDragEnd_EndPuzzleDrag : AEvent<Scene, PointerDragEnd>
    {
        /// <summary>
        /// 根据当前 Puzzle 交互状态完成拖拽落位，失败时回初始托盘和默认朝向
        /// </summary>
        /// <param name="scene">当前关卡场景</param>
        /// <param name="args">松手输入上下文</param>
        protected override async ETTask Run(Scene scene, PointerDragEnd args)
        {
            DragInteractionStateComponent interactionStateComponent = scene.GetComponent<DragInteractionStateComponent>();
            Puzzle draggingPuzzle = interactionStateComponent?.GetDraggingTarget() as Puzzle;
            if (draggingPuzzle == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            this.HandleDragRelease(scene, draggingPuzzle, args.Context.WorldPosition);
            interactionStateComponent.ClearDraggingTarget();
            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 结算正式拖拽的松手位置，非法落点清除占格并回初始托盘和默认朝向
        /// </summary>
        /// <param name="scene">当前场景</param>
        /// <param name="draggingPuzzle">本次正在拖拽的 Puzzle</param>
        /// <param name="pointerWorldPosition">松手时指针所在的世界坐标</param>
        private void HandleDragRelease(Scene scene, Puzzle draggingPuzzle, Vector3 pointerWorldPosition)
        {
            PuzzleDragComponent dragComponent = draggingPuzzle.GetComponent<PuzzleDragComponent>();
            PuzzleView puzzleView = draggingPuzzle.GetComponent<PuzzleView>();
            Grid grid = scene.GetGrid();
            GridView gridView = grid?.GetComponent<GridView>();
            if (dragComponent == null || puzzleView == null)
            {
                return;
            }

            if (dragComponent.IsInGridSnapMode())
            {
                if (dragComponent.CanFinalizeGridSnap() && gridView != null && dragComponent.SnapToAnchor(grid))
                {
                    this.CommitLanding(grid, puzzleView);
                }
                else
                {
                    dragComponent.RestoreInitialPosition();
                }

                return;
            }

            if (grid == null || gridView == null)
            {
                dragComponent.RestoreInitialPosition();
                return;
            }

            Vector2 gridCoordinate = gridView.WorldToGridCoordinate(pointerWorldPosition);
            int anchorX = Mathf.RoundToInt(gridCoordinate.x);
            int anchorY = Mathf.RoundToInt(gridCoordinate.y);
            if (grid.Contains(anchorX, anchorY) && dragComponent.SnapToAnchor(grid, anchorX, anchorY))
            {
                this.CommitLanding(grid, puzzleView);
                return;
            }

            dragComponent.RestoreInitialPosition();
        }

        /// <summary>为本次落位的拼图播放特效，再检测关卡完成状态</summary>
        /// <param name="grid">本次操作的棋盘</param>
        /// <param name="puzzleView">本次成功落位的拼图表现</param>
        private void CommitLanding(Grid grid, PuzzleView puzzleView)
        {
            puzzleView.PlayPlacementEffect();

            grid.TryPublishCompletion();
        }
    }
}
