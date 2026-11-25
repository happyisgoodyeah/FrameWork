using DG.Tweening;

namespace ET.Client
{
    /// <summary>
    /// 处理拼图旋转和关卡场景中的拼图解锁点击。
    /// </summary>
    [Event(SceneType.Current)]
    public class PointerClick_InteractWithPuzzle : AEvent<Scene, PointerClick>
    {
        /// <summary>
        /// 优先旋转命中的拼图，按下和松开命中同一场景目标时向解谜模型提交一次点击。
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

            PuzzleInputResolveResult press = PuzzleInputResolver.ResolvePress(args.Context);
            if (press.Puzzle != null)
            {
                this.TryRotatePuzzle(scene, press.Puzzle);
            }
            else if (press.Reveal != null && press.GridSlot == null && press.Reveal.GetParent<Puzzle>().GetParent<Grid>() == grid)
            {
                PuzzleInputResolveResult release = PuzzleInputResolver.Resolve(args.Context);
                if (release.Reveal == press.Reveal && release.Puzzle == null && release.GridSlot == null)
                {
                    press.Reveal.RegisterClick();
                }
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
