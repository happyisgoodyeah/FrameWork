namespace ET.Client
{
    [EntitySystemOf(typeof(PuzzleRevealComponent))]
    public static partial class PuzzleRevealComponentSystem
    {
        /// <summary>用本关要求的点击次数初始化隐藏拼图。</summary>
        [EntitySystem]
        private static void Awake(this PuzzleRevealComponent self, int requiredClicks)
        {
            self.RemainingClicks = requiredClicks;
        }

        /// <summary>记录一次有效点击，达到指定次数时解除拼图的放置限制。</summary>
        /// <returns>本次点击是否解锁了拼图。</returns>
        public static bool RegisterClick(this PuzzleRevealComponent self)
        {
            if (--self.RemainingClicks > 0)
            {
                return false;
            }

            self.GetParent<Puzzle>().RemoveComponent<PuzzleRevealComponent>();
            return true;
        }
    }
}
