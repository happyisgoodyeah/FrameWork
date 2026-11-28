using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;

namespace ET.Client
{
    /// <summary>
    /// 图鉴存档读取、列表刷新与拼图详情展示
    /// </summary>
    [FriendOf(typeof(ArchiveViewComponent))]
    [FriendOf(typeof(ArchiveItemContentComponent))]
    [FriendOf(typeof(ArchiveItemComponent))]
    public static partial class ArchiveViewComponentSystem
    {
        /// <summary>初始化图鉴列表并将滚动限制在竖直方向</summary>
        [EntitySystem]
        private static void YIUIInitialize(this ArchiveViewComponent self)
        {
            self.Contents = new List<EntityRef<ArchiveItemContentComponent>>();
            self.ScrollRect = self.UIBase.OwnerGameObject.GetComponentInChildren<ScrollRect>(true);
            self.ScrollRect.horizontal = false;
            self.SelectItem(null);
            self.u_ComU_UnlockCount.SetText(string.Empty);
        }

        /// <summary>释放列表、滚动容器及选中条目的引用</summary>
        [EntitySystem]
        private static void Destroy(this ArchiveViewComponent self)
        {
            self.Contents.Clear();
            self.ScrollRect = null;
            self.SelectedItem = default;
        }

        /// <summary>打开图鉴时读取当前存档并按解锁顺序加载条目数据</summary>
        /// <returns>存档与拼图数据就绪时返回 true</returns>
        [EntitySystem]
        private static async ETTask<bool> YIUIOpen(this ArchiveViewComponent self)
        {
            EntityRef<ArchiveViewComponent> selfRef = self;
            SaveDataRoot saveData = await GameplaySaveHelper.LoadGameplaySaveAsync(self.Root().GetComponent<SaveManagerComponent>());
            self = selfRef;
            if (self == null || saveData == null)
            {
                return false;
            }

            int[] unlockIds = saveData.GetComponent<PuzzleArchiveSaveDataComponent>().UnlockedPuzzleIds.ToArray();
            ResourcesLoaderComponent loader = self.Root().GetComponent<ResourcesLoaderComponent>();
            var puzzles = new (PuzzleConfig Config, Sprite Sprite)[unlockIds.Length];
            for (int i = 0; i < unlockIds.Length; ++i)
            {
                PuzzleConfig config = PuzzleCoreHelper.GetPuzzleConfig(unlockIds[i]);
                Sprite sprite = await LoadPuzzleSpriteAsync(loader, config);
                self = selfRef;
                if (self == null)
                {
                    return false;
                }

                puzzles[i] = (config, sprite);
            }

            self.RefreshArchive(puzzles);
            return true;
        }

        /// <summary>读取玩法预制体的主图供图鉴列表与详情共用</summary>
        /// <param name="loader">根场景的资源加载组件</param>
        /// <param name="config">图鉴条目对应的拼图配置</param>
        /// <returns>拼图预制体中的主图</returns>
        private static async ETTask<Sprite> LoadPuzzleSpriteAsync(ResourcesLoaderComponent loader, PuzzleConfig config)
        {
            string location = ResourceLocationHelper.ToAssetLocation(config.PrefabPath);
            GameObject prefab = await loader.LoadAssetAsync<GameObject>(location);
            if (prefab == null)
            {
                throw new InvalidOperationException($"图鉴拼图预制体加载失败：{location}");
            }

            Sprite sprite = PuzzleViewSystem.ResolveSpriteRenderer(prefab.transform, prefab.transform.Find("VisualRoot"))?.sprite;
            if (sprite == null)
            {
                throw new InvalidOperationException($"图鉴拼图预制体缺少主图：{location}");
            }

            return sprite;
        }

        /// <summary>先创建需要的图鉴行，再填充拼图并更新选中详情</summary>
        private static void RefreshArchive(this ArchiveViewComponent self, IReadOnlyList<(PuzzleConfig Config, Sprite Sprite)> puzzles)
        {
            const int capacity = ArchiveItemContentComponent.Capacity;
            int rowCount = (puzzles.Count + capacity - 1) / capacity;
            while (self.Contents.Count < rowCount)
            {
                ArchiveItemContentComponent content = YIUIFactory.Instantiate<ArchiveItemContentComponent>(self.Scene(), self, self.ScrollRect.content);
                self.Contents.Add(content);
            }

            for (int i = 0; i < self.Contents.Count; ++i)
            {
                ArchiveItemContentComponent content = self.Contents[i];
                content.UIBase.SetActive(i < rowCount);
                if (i < rowCount)
                {
                    content.Fill(puzzles, i * capacity);
                }
            }

            self.u_ComU_UnlockCount.SetText($"已经解锁{puzzles.Count}个");
            ArchiveItemContentComponent firstContent = rowCount > 0 ? self.Contents[0].Entity : null;
            self.SelectItem(firstContent?.Items[0].Entity);
            self.ScrollRect.StopMovement();
            LayoutRebuilder.ForceRebuildLayoutImmediate(self.ScrollRect.content);
            self.ScrollRect.verticalNormalizedPosition = 1f;
        }

        /// <summary>接收属于当前图鉴视图的条目点击事件</summary>
        [EntitySystem]
        private static async ETTask DynamicEvent(this ArchiveViewComponent self, ArchiveItemSelected args)
        {
            ArchiveItemComponent item = args.Item;
            if (item != null && item.UIBase.GetParent<ArchiveItemContentComponent>().UIBase.GetParent<ArchiveViewComponent>() == self)
            {
                self.SelectItem(item);
            }

            await ETTask.CompletedTask;
        }

        /// <summary>切换选中条目并同步主图、名称与占用格数，空列表时隐藏详情</summary>
        private static void SelectItem(this ArchiveViewComponent self, ArchiveItemComponent item)
        {
            self.SelectedItem.Entity?.SetSelected(false);
            self.SelectedItem = EntityRefHelper.GetEntityRefSafety(item);
            self.u_ComU_SelectPuzzleImage.transform.parent.gameObject.SetActive(item != null);
            if (item == null)
            {
                return;
            }

            item.SetSelected(true);
            PuzzleConfig config = item.PuzzleConfig;
            self.u_ComU_SelectPuzzleImage.sprite = item.u_ComU_PuzzleImage.sprite;
            self.u_ComU_SelectPuzzleImage.preserveAspect = true;
            self.u_ComU_SelectPuzzleNameText.SetText(config.Name);
            self.u_ComU_SelectPuzzleDescText.SetText($"占用 {config.SlotOffset.Count} 格");
        }

        /// <summary>在当前主面板内从图鉴返回主菜单</summary>
        [YIUIInvoke(ArchiveViewComponent.OnEventU_BackButtonClickInvoke)]
        private static async ETTask OnEventU_BackButtonClickInvoke(this ArchiveViewComponent self)
        {
            GameMainPanelPanelComponent panel = self.UIBase.GetParent<GameMainPanelPanelComponent>();
            await panel.UIPanel.OpenViewAsync<Main1ViewComponent>();
        }
    }
}
