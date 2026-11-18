namespace ET.Client
{
    /// <summary>
    /// PuzzleCore 布局的唯一数值来源。
    /// 运行时表现层与编辑器烘焙工具都必须从这里取值，避免两侧各自硬编码后静默失配。
    /// </summary>
    public static class PuzzleLayoutConst
    {
        /// <summary>
        /// Grid 每个 Slot 在世界空间中的间距。
        /// 编辑器按此值把 Slot 位置烘焙进 Grid 预制体，运行时按同一值反算格坐标，
        /// 两者一旦不一致，吸附判定就会整体偏移。
        /// </summary>
        public const float GridCellSize = 3f;

        /// <summary>
        /// 待放置拼图停靠区的世界 X 绝对值，仅作为相机拟合的内容边界基准。
        /// 实际停靠位置由 <see cref="RequiredHalfWidth"/> 与拟合后的视锥反推，不再直接使用此值定位。
        /// </summary>
        public const float TrayWorldX = 15f;

        /// <summary>
        /// 待放置拼图停靠区的世界 Y 绝对值上限，取自关卡配置中 Trans.y 的最大绝对值。
        /// </summary>
        public const float TrayWorldY = 7.5f;

        /// <summary>
        /// 单块拼图碰撞体的半边长，用于把停靠点扩展成实际占用范围。
        /// </summary>
        public const float PuzzleHalfExtent = GridCellSize * 0.5f;

        /// <summary>
        /// 相机必须保证可见的内容半宽。
        /// </summary>
        public const float RequiredHalfWidth = TrayWorldX + PuzzleHalfExtent;

        /// <summary>
        /// 相机必须保证可见的内容半高。
        /// </summary>
        public const float RequiredHalfHeight = TrayWorldY + PuzzleHalfExtent;

        /// <summary>
        /// 左侧托盘编号，与 Grid 表 PuzzleList 的 Tray 字段取值对应。
        /// </summary>
        public const int TrayLeft = 0;

        /// <summary>
        /// 右侧托盘编号，与 Grid 表 PuzzleList 的 Tray 字段取值对应。
        /// </summary>
        public const int TrayRight = 1;

        /// <summary>
        /// 同一托盘内相邻槽位的垂直间距。
        /// </summary>
        public const float TraySlotPitch = 7.5f;
    }
}
