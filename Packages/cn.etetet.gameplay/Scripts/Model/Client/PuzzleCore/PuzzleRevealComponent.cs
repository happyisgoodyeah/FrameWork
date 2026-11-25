namespace ET.Client
{
    /// <summary>本关拼图实例的解谜规则与剩余点击数，随拼图创建和释放。</summary>
    [ComponentOf(typeof(Puzzle))]
    public class PuzzleRevealComponent : Entity, IAwake<PuzzleRevealRule>
    {
        public PuzzleRevealRule Rule { get; set; }
        public int RemainingClicks { get; set; }
        public bool IsLocked => RemainingClicks > 0;
    }
}
