using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 把配表中的托盘锚点语义（Tray + SlotIndex）解析为实际世界坐标。
    /// 解析放在表现层，因为结果依赖相机的可见范围，而可见范围随屏幕比例变化。
    /// </summary>
    public static class PuzzleTrayHelper
    {
        /// <summary>
        /// 计算指定 Puzzle 待放置时应停靠的世界坐标
        /// </summary>
        /// <param name="scene">当前场景</param>
        /// <param name="puzzle">目标 Puzzle</param>
        /// <returns>该 Puzzle 在托盘中的世界坐标</returns>
        public static Vector3 ResolveTrayWorldPosition(Scene scene, Puzzle puzzle)
        {
            float halfWidth = GetVisibleHalfWidth(scene);

            // 托盘贴住视锥左右边缘，向内让出半个拼图，保证整块可见
            float x = halfWidth - PuzzleLayoutConst.PuzzleHalfExtent;
            if (puzzle.Tray == PuzzleLayoutConst.TrayLeft)
            {
                x = -x;
            }

            // 整列槽位垂直居中：槽位数为 n 时，首个槽位在 (n-1)/2 * pitch 处向下排列
            int slotCount = puzzle.TraySlotCount > 0 ? puzzle.TraySlotCount : 1;
            float topY = (slotCount - 1) * PuzzleLayoutConst.TraySlotPitch * 0.5f;
            float y = topY - puzzle.TraySlotIndex * PuzzleLayoutConst.TraySlotPitch;

            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// 获取当前相机的可见半宽，取不到相机时回退到设计基准值
        /// </summary>
        /// <param name="scene">当前场景</param>
        /// <returns>可见半宽</returns>
        private static float GetVisibleHalfWidth(Scene scene)
        {
            PuzzleSceneRoot puzzleSceneRoot = scene?.GetComponent<PuzzleSceneRoot>();
            Camera gameCamera = puzzleSceneRoot != null ? puzzleSceneRoot.GameCamera : null;
            if (gameCamera == null)
            {
                gameCamera = Camera.main;
            }

            if (gameCamera == null || !gameCamera.orthographic || gameCamera.aspect <= 0f)
            {
                return PuzzleLayoutConst.RequiredHalfWidth;
            }

            // 相机已按内容范围拟合，因此可见半宽不会小于设计基准
            return gameCamera.orthographicSize * gameCamera.aspect;
        }
    }
}
