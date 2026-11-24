namespace ET.Client
{
    /// <summary>拼图等待场景点击解锁期间的计数，解锁后随即移除。</summary>
    [ComponentOf(typeof(Puzzle))]
    public class PuzzleRevealComponent : Entity, IAwake<int>
    {
        public int RemainingClicks { get; set; }
    }
}
