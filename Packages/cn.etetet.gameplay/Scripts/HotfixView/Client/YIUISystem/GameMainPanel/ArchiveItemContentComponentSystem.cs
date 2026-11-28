using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 图鉴单行的条目填充与留白布局
    /// </summary>
    [FriendOf(typeof(ArchiveItemContentComponent))]
    public static partial class ArchiveItemContentComponentSystem
    {
        /// <summary>初始化这一行可复用的拼图条目列表</summary>
        [EntitySystem]
        private static void YIUIInitialize(this ArchiveItemContentComponent self)
        {
            self.Items = new List<EntityRef<ArchiveItemComponent>>(ArchiveItemContentComponent.Capacity);
        }

        /// <summary>释放这一行持有的条目引用</summary>
        [EntitySystem]
        private static void Destroy(this ArchiveItemContentComponent self)
        {
            self.Items.Clear();
        }

        /// <summary>用列表的连续一段填充当前行，每侧最多放三个拼图</summary>
        /// <param name="self">图鉴行</param>
        /// <param name="puzzles">按解锁顺序排列的拼图配置与主图</param>
        /// <param name="startIndex">当前行在拼图列表中的起始索引</param>
        public static void Fill(this ArchiveItemContentComponent self, IReadOnlyList<(PuzzleConfig Config, Sprite Sprite)> puzzles, int startIndex)
        {
            int count = Math.Min(puzzles.Count - startIndex, ArchiveItemContentComponent.Capacity);
            for (int i = 0; i < count; ++i)
            {
                ArchiveItemComponent item = self.GetOrCreateItem(i);
                var puzzle = puzzles[startIndex + i];
                item.BindPuzzle(puzzle.Config, puzzle.Sprite);
                item.UIBase.SetActive(true);
            }

            for (int i = count; i < self.Items.Count; ++i)
            {
                ArchiveItemComponent item = self.Items[i];
                item.UIBase.SetActive(false);
            }

            self.ReserveEmptySlots(count);
        }

        /// <summary>复用或生成指定位置的条目，前三个插在 White1 前，后三个追加到留白之后</summary>
        private static ArchiveItemComponent GetOrCreateItem(this ArchiveItemContentComponent self, int index)
        {
            if (index < self.Items.Count)
            {
                return self.Items[index];
            }

            ArchiveItemComponent item = YIUIFactory.Instantiate<ArchiveItemComponent>(self.Scene(), self, self.UIBase.OwnerRectTransform);
            if (index < ArchiveItemContentComponent.ItemsPerSide)
            {
                item.UIBase.OwnerRectTransform.SetSiblingIndex(self.UIWhite1.UIBase.OwnerRectTransform.GetSiblingIndex());
            }

            self.Items.Add(item);
            return item;
        }

        /// <summary>用两侧的布局边距补足缺少的格宽，让 White1 至 White3 在未满行中也居中</summary>
        private static void ReserveEmptySlots(this ArchiveItemContentComponent self, int count)
        {
            HorizontalLayoutGroup layout = self.UIBase.OwnerGameObject.GetComponent<HorizontalLayoutGroup>();
            float slotWidth = LayoutUtility.GetPreferredWidth(self.UIWhite1.UIBase.OwnerRectTransform) + layout.spacing;
            int left = Math.Max(0, ArchiveItemContentComponent.ItemsPerSide - count);
            int right = ArchiveItemContentComponent.Capacity - count - left;
            layout.padding = new RectOffset(Mathf.RoundToInt(left * slotWidth), Mathf.RoundToInt(right * slotWidth),
                layout.padding.top, layout.padding.bottom);
        }
    }
}
