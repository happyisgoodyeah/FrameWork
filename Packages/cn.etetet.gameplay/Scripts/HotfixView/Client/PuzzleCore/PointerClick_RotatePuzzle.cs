using DG.Tweening;

namespace ET.Client
{
    /// <summary>
    /// 在通用点击事件中处理 Puzzle 旋转。
    /// </summary>
    [Event(SceneType.Current)]
    public class PointerClick_RotatePuzzle : AEvent<Scene, PointerClick>
    {
        /// <summary>
        /// 在关卡就绪且没有拖拽手势时尝试旋转静止拼图
        /// </summary>
        /// <param name="scene">当前关卡场景</param>
        /// <param name="args">点击输入上下文</param>
        protected override async ETTask Run(Scene scene, PointerClick args)
        {
            Grid grid = scene.GetGrid();
            if (grid == null || !grid.IsReady || grid.CompletionPublished)
            {
                return;
            }

            DragInteractionStateComponent interactionStateComponent = scene.GetComponent<DragInteractionStateComponent>();
            if (interactionStateComponent?.GetDraggingTarget() != null)
            {
                await ETTask.CompletedTask;
                return;
            }

            PuzzleInputResolveResult resolveResult = PuzzleInputResolver.ResolvePress(args.Context);
            Puzzle puzzle = resolveResult.Puzzle;
            if (puzzle != null)
            {
                this.TryRotatePuzzle(scene, puzzle);
            }

            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 仅在拼图未拖拽且移动已结束时执行旋转，并校验已放置拼图的合法性
        /// </summary>
        /// <param name="scene">当前场景</param>
        /// <param name="puzzle">要旋转的 Puzzle</param>
        private void TryRotatePuzzle(Scene scene, Puzzle puzzle)
        {
            PuzzleView puzzleView = puzzle.GetComponent<PuzzleView>();
            PuzzleDragComponent dragComponent = puzzle.GetComponent<PuzzleDragComponent>();
            if (puzzleView == null || dragComponent == null)
            {
                return;
            }

            if (dragComponent.IsDragging ||
                (dragComponent.MoveTween != null && dragComponent.MoveTween.IsActive() && !dragComponent.MoveTween.IsComplete()))
            {
                return;
            }

            Grid grid = scene.GetGrid();
            PuzzleRotation previousRotation = puzzle.Rotation;
            PuzzleState previousState = puzzle.State;
            int previousAnchorX = puzzle.AnchorX;
            int previousAnchorY = puzzle.AnchorY;
            if (previousState == PuzzleState.Placed && grid != null)
            {
                puzzle.ClearPlacement(grid);
            }

            puzzle.RotateClockwise();
            bool rotationApplied = previousState != PuzzleState.Placed || grid == null || puzzle.TryPlaceOnGrid(grid, previousAnchorX, previousAnchorY);
            if (!rotationApplied)
            {
                puzzle.SetRotation(previousRotation);
                if (previousState == PuzzleState.Placed && grid != null)
                {
                    puzzle.TryPlaceOnGrid(grid, previousAnchorX, previousAnchorY);
                }

                puzzleView.PlayBlockedRotation();
                puzzleView.RestoreVisualPriority();
                return;
            }

            puzzleView.RefreshRotation(true);
            puzzleView.RestoreVisualPriority();
        }
    }
}
