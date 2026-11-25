namespace ET.Client
{
    /// <summary>
    /// 在进入下一初始化阶段前确认必需的客户端表现已创建成功
    /// </summary>
    public struct PuzzleViewCreationCheck
    {
        /// <summary>刚完成生命周期事件的 Grid、Puzzle 或 Slot</summary>
        public Entity Entity;
    }

    /// <summary>
    /// Grid 数据实体创建完成事件。
    /// </summary>
    public struct AfterCreateGrid
    {
        /// <summary>
        /// 本次创建完成的 Grid 数据实体。
        /// </summary>
        public Grid Grid;
    }

    /// <summary>
    /// Puzzle 数据实体创建完成事件。
    /// </summary>
    public struct AfterCreatePuzzle
    {
        /// <summary>
        /// 本次创建完成的 Puzzle 数据实体。
        /// </summary>
        public Puzzle Puzzle;
    }

    /// <summary>一次有效点击已更新拼图解谜进度，剩余次数为零时拼图可用。</summary>
    public struct PuzzleRevealProgressChanged
    {
        public PuzzleRevealComponent Reveal;
    }

    /// <summary>
    /// Slot 数据实体创建完成事件。
    /// </summary>
    public struct AfterCreateSlot
    {
        /// <summary>
        /// 本次创建完成的 Slot 数据实体
        /// </summary>
        public Slot Slot;
    }

    /// <summary>
    /// 当前关卡完成事件
    /// </summary>
    public struct PuzzleLevelCompleted
    {
        /// <summary>
        /// 已完成的 Grid 数据实体
        /// </summary>
        public Grid Grid;

        /// <summary>
        /// 已完成的 Grid 配置 id
        /// </summary>
        public int GridConfigId;
    }
}
