using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 图鉴拼图条目的展示与点击
    /// </summary>
    [FriendOf(typeof(ArchiveItemComponent))]
    public static partial class ArchiveItemComponentSystem
    {
        /// <summary>让条目和留白都以未选中状态初始化</summary>
        [EntitySystem]
        private static void YIUIInitialize(this ArchiveItemComponent self)
        {
            self.SetSelected(false);
        }

        /// <summary>释放条目持有的拼图配置</summary>
        [EntitySystem]
        private static void Destroy(this ArchiveItemComponent self)
        {
            self.PuzzleConfig = null;
        }

        /// <summary>将已解锁拼图的配置和主图绑定到条目</summary>
        /// <param name="self">图鉴条目</param>
        /// <param name="config">拼图配置</param>
        /// <param name="sprite">拼图主图</param>
        public static void BindPuzzle(this ArchiveItemComponent self, PuzzleConfig config, Sprite sprite)
        {
            self.PuzzleConfig = config;
            self.u_ComU_NameText.SetText(config.Name);
            self.u_ComU_PuzzleImage.sprite = sprite;
            self.u_ComU_PuzzleImage.preserveAspect = true;
        }

        /// <summary>切换条目的选中背景</summary>
        public static void SetSelected(this ArchiveItemComponent self, bool selected)
        {
            self.u_ComU_Select.gameObject.SetActive(selected);
        }

        /// <summary>通知图鉴视图选择当前条目</summary>
        [YIUIInvoke(ArchiveItemComponent.OnEventU_ButtonClickInvoke)]
        private static ETTask OnEventU_ButtonClickInvoke(this ArchiveItemComponent self)
        {
            if (self.PuzzleConfig == null)
            {
                return ETTask.CompletedTask;
            }

            return self.DynamicEvent(self.Root(), new ArchiveItemSelected { Item = self });
        }
    }
}
