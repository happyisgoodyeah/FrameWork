namespace ET.Client
{
    [EntitySystemOf(typeof(PuzzleRevealComponent))]
    public static partial class PuzzleRevealComponentSystem
    {
        /// <summary>在拼图数据创建时装入本关规则并从完整点击次数开始。</summary>
        [EntitySystem]
        private static void Awake(this PuzzleRevealComponent self, PuzzleRevealRule rule)
        {
            self.Rule = rule;
            self.RemainingClicks = rule.RequiredClicks;
        }

        /// <summary>在可交互棋盘上消费一次点击，先更新解锁状态再通知表现层。</summary>
        public static void RegisterClick(this PuzzleRevealComponent self)
        {
            if (self.IsDisposed || !self.IsLocked)
            {
                return;
            }

            Grid grid = self.GetParent<Puzzle>().GetParent<Grid>();
            if (!grid.IsReady || grid.CompletionPublished)
            {
                return;
            }

            --self.RemainingClicks;
            EventSystem.Instance.Publish(grid.Scene(), new PuzzleRevealProgressChanged { Reveal = self });
        }
    }
}
