using System;

namespace ET.Client
{
    /// <summary>
    /// PuzzleCore 对通用输入命中结果的业务解析结果。
    /// </summary>
    public struct PuzzleInputResolveResult
    {
        /// <summary>
        /// 当前命中的 PuzzleView。
        /// </summary>
        public PuzzleView PuzzleView;

        /// <summary>
        /// 当前命中的 Puzzle。
        /// </summary>
        public Puzzle Puzzle;

        /// <summary>当前命中的场景解锁物件，同一物件可以有多个碰撞区域。</summary>
        public PuzzleRevealComponent Reveal;

        /// <summary>
        /// 当前命中的 SlotView。
        /// </summary>
        public SlotView SlotView;

        /// <summary>
        /// 当前命中的 Slot。
        /// </summary>
        public Slot Slot;

        /// <summary>
        /// 当前命中的 Puzzle SlotView。
        /// </summary>
        public SlotView PuzzleSlotView;

        /// <summary>
        /// 当前命中的 Puzzle Slot。
        /// </summary>
        public Slot PuzzleSlot;

        /// <summary>
        /// 当前命中的 Grid SlotView。
        /// </summary>
        public SlotView GridSlotView;

        /// <summary>
        /// 当前命中的 Grid Slot。
        /// </summary>
        public Slot GridSlot;
    }

    /// <summary>
    /// PuzzleCore 输入解析辅助方法，负责从通用输入结果中筛选 PuzzleCore 相关对象。
    /// </summary>
    public static class PuzzleInputResolver
    {
        /// <summary>
        /// 从当前指针位置解析拼图、格子和场景解锁物件。
        /// </summary>
        /// <param name="pointerContext">输入层发布的通用输入上下文。</param>
        /// <returns>当前命中的 PuzzleCore 解析结果。</returns>
        public static PuzzleInputResolveResult Resolve(PointerInputContext pointerContext)
        {
            return Resolve(pointerContext.HitResults);
        }

        /// <summary>
        /// 仅解析按压起点命中，按下空白处后移到拼图上不会转移选中对象
        /// </summary>
        /// <param name="pointerContext">输入层发布的通用输入上下文</param>
        /// <param name="requireInteractive">是否要求关卡仍可交互，松手清理表现时可关闭</param>
        /// <returns>按压起始时命中的 PuzzleCore 解析结果</returns>
        public static PuzzleInputResolveResult ResolvePress(PointerInputContext pointerContext, bool requireInteractive = true)
        {
            return Resolve(pointerContext.PressHitResults, requireInteractive);
        }

        /// <summary>
        /// 从指定命中中解析拼图和格子，仅将解谜表现已绑定的触发器识别为解谜区域。释放表现时允许访问已通关棋盘。
        /// </summary>
        /// <param name="hitResults">要解析的输入命中结果数组</param>
        /// <param name="requireInteractive">是否只接受可交互棋盘中的命中</param>
        /// <returns>当前命中的 PuzzleCore 解析结果</returns>
        private static PuzzleInputResolveResult Resolve(InputHitResult[] hitResults, bool requireInteractive = true)
        {
            PuzzleInputResolveResult result = default;
            if (hitResults == null || hitResults.Length == 0)
            {
                return result;
            }

            for (int i = 0; i < hitResults.Length; ++i)
            {
                InputHitResult hit = hitResults[i];
                Entity entity = hit.Entity;
                if (entity == null)
                {
                    continue;
                }

                if (entity is PuzzleRevealView revealView && hit.Collider2D != null && hit.Collider2D.isTrigger &&
                    revealView.HitAreas != null && Array.IndexOf(revealView.HitAreas, hit.Collider2D) >= 0)
                {
                    result.Reveal ??= revealView.GetParent<PuzzleRevealComponent>();
                    continue;
                }

                if (entity is PuzzleView puzzleView && (!requireInteractive || IsInteractive(puzzleView.GetParent<Puzzle>().GetParent<Grid>())) &&
                    result.PuzzleView == null)
                {
                    result.PuzzleView = puzzleView;
                    result.Puzzle = puzzleView.GetParent<Puzzle>();
                }

                if (entity is SlotView slotView)
                {
                    Slot slot = slotView.GetParent<Slot>();
                    Grid grid = slot.Parent is Grid parentGrid ? parentGrid : slot.GetParent<Puzzle>().GetParent<Grid>();
                    if (requireInteractive && !IsInteractive(grid))
                    {
                        continue;
                    }

                    result.SlotView = result.SlotView ?? slotView;
                    result.Slot = result.Slot ?? slot;

                    if (slot != null && slot.IsPuzzleSlot())
                    {
                        result.PuzzleSlotView ??= slotView;
                        result.PuzzleSlot ??= slot;
                        if (result.Puzzle == null && slot.Parent is Puzzle puzzle)
                        {
                            result.Puzzle = puzzle;
                            result.PuzzleView = puzzle.GetComponent<PuzzleView>();
                        }
                    }

                    if (slot != null && slot.IsGridSlot())
                    {
                        result.GridSlotView ??= slotView;
                        result.GridSlot ??= slot;
                    }
                }

                if (result.PuzzleView != null && result.PuzzleSlotView != null && result.GridSlotView != null)
                {
                    return result;
                }
            }

            return result;
        }

        /// <summary>判断命中所属棋盘是否允许玩法交互</summary>
        /// <param name="grid">命中对象所属棋盘</param>
        /// <returns>是否已完成加载且尚未提交通关</returns>
        private static bool IsInteractive(Grid grid)
        {
            return grid.IsReady && !grid.CompletionPublished;
        }
    }
}
