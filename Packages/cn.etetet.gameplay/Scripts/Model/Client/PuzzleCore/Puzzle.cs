namespace ET.Client
{
    /// <summary>
    /// 运行时拼图实体，持有形状格子、旋转和当前摆放状态
    /// </summary>
    [ChildOf(typeof(Grid))]
    public class Puzzle : Entity, IAwake<int>
    {
        /// <summary>
        /// 对应拼图配置表的 id。
        /// </summary>
        public int PuzzleConfigId { get; set; }

        /// <summary>
        /// 当前拼图待放置时所在的托盘编号，取值见 PuzzleLayoutConst.TrayLeft / TrayRight。
        /// </summary>
        public int Tray { get; set; }

        /// <summary>
        /// 当前拼图在所属托盘内的槽位序号，0 为最上方。
        /// </summary>
        public int TraySlotIndex { get; set; }

        /// <summary>
        /// 所属托盘内的槽位总数，用于把整列槽位垂直居中。
        /// </summary>
        public int TraySlotCount { get; set; }

        /// <summary>
        /// 当前拼图采用的离散旋转状态。
        /// </summary>
        public PuzzleRotation Rotation { get; set; }

        /// <summary>
        /// 当前拼图处于托盘、拖拽中或已放置等状态。
        /// </summary>
        public PuzzleState State { get; set; }

        /// <summary>
        /// 拼图锚点在 Grid 坐标系中的 X 坐标。
        /// </summary>
        public int AnchorX { get; set; }

        /// <summary>
        /// 拼图锚点在 Grid 坐标系中的 Y 坐标。
        /// </summary>
        public int AnchorY { get; set; }

        /// <summary>
        /// 当前拼图原点 Slot 的实体 Id。
        /// </summary>
        public long OriginSlotId { get; set; }
    }
}
