using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 按棋盘和拼图的边缘计算托盘初始位置
    /// </summary>
    public static class PuzzleTrayHelper
    {
        /// <summary>
        /// 按托盘序号纵向排列拼图，并让拼图内侧与棋盘保持半格间距
        /// </summary>
        /// <param name="view">已绑定主图和初始朝向的拼图表现</param>
        /// <returns>拼图根节点的初始世界坐标</returns>
        public static Vector3 ResolveTrayWorldPosition(PuzzleView view)
        {
            Puzzle puzzle = view.GetParent<Puzzle>();
            Grid grid = puzzle.GetParent<Grid>();
            GridView gridView = grid.GetComponent<GridView>();
            bool isLeft = puzzle.Tray == PuzzleLayoutConst.TrayLeft;
            float direction = isLeft ? -1f : 1f;

            int slotCount = puzzle.TraySlotCount > 0 ? puzzle.TraySlotCount : 1;
            float y = ((slotCount - 1) * 0.5f - puzzle.TraySlotIndex) * PuzzleLayoutConst.TraySlotPitch;

            float gridHalfWidth = grid.Width * gridView.CellSize * 0.5f;
            float gap = gridView.CellSize * 0.5f;
            Vector3 position = gridView.SlotRoot.TransformPoint(new Vector3(direction * (gridHalfWidth + gap), y, 0f));

            // 主图相对根节点可能偏移，按靠近棋盘的一侧对齐
            Bounds bounds = view.SpriteRenderer.bounds;
            float innerEdgeX = isLeft ? bounds.max.x : bounds.min.x;
            position.x += view.Transform.position.x - innerEdgeX;
            return position;
        }
    }
}
