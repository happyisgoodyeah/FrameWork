using UnityEngine;

namespace PuzzleGame
{
    /// <summary>在关卡主题中指定可点击的场景物件及其解锁的托盘拼图。</summary>
    [DisallowMultipleComponent]
    public sealed class PuzzleRevealTrigger : MonoBehaviour
    {
        [SerializeField] private int puzzleConfigId;
        [SerializeField, Tooltip("托盘编号，左侧为 0，右侧为 1。")] private int tray;
        [SerializeField, Min(0), Tooltip("托盘槽位从上向下编号，从 0 开始。")] private int traySlotIndex;
        [SerializeField, Min(1)] private int requiredClicks = 3;
        [SerializeField, Tooltip("同一场景物件的可点击区域，共享点击次数。")] private Collider2D[] hitAreas;

        public int RequiredClicks => requiredClicks;
        public Collider2D[] HitAreas => hitAreas;

        /// <summary>按配置和托盘位置识别目标，允许同关使用多块相同拼图。</summary>
        public bool Matches(int configId, int trayIndex, int slotIndex)
        {
            return puzzleConfigId == configId && tray == trayIndex && traySlotIndex == slotIndex;
        }

        /// <summary>在关卡初始化时检查点击次数和命中区域，避免隐藏无法解锁的拼图。</summary>
        public void Validate()
        {
            if (requiredClicks < 1 || hitAreas == null || hitAreas.Length == 0)
                throw new UnityException($"拼图解锁需要正数点击次数和至少一个命中区域：{name}");

            foreach (Collider2D area in hitAreas)
            {
                if (area == null || !area.isActiveAndEnabled || !area.transform.IsChildOf(transform))
                    throw new UnityException($"拼图解锁的命中区域必须在当前主题下启用：{name}");
            }
        }
    }
}
