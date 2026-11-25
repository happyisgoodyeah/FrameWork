using System;
using PuzzleGame;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(PuzzleRevealView))]
    public static partial class PuzzleRevealViewSystem
    {
        /// <summary>为每条解谜路径绑定节点上的第一个二维触发器，并应用模型的初始可见性。</summary>
        /// <param name="self">当前解谜模型的场景表现。</param>
        /// <param name="theme">已实例化的关卡主题，命中路径相对于其根节点。</param>
        [EntitySystem]
        private static void Awake(this PuzzleRevealView self, GameObject theme)
        {
            PuzzleRevealComponent reveal = self.GetParent<PuzzleRevealComponent>();
            var paths = reveal.Rule.HitPaths;
            var areas = new Collider2D[paths.Count];
            for (int i = 0; i < paths.Count; ++i)
            {
                Transform node = theme != null ? theme.transform.Find(paths[i]) : null;
                Collider2D area = node != null && node.gameObject.activeInHierarchy
                    ? Array.Find(node.GetComponents<Collider2D>(), collider => collider.isTrigger)
                    : null;
                if (area == null)
                {
                    throw new UnityException($"拼图 {reveal.Rule.PuzzleId} 的解谜路径需要一个启用的场景节点和触发碰撞体：{paths[i]}");
                }
                if (Array.IndexOf(areas, area, 0, i) >= 0 || area.GetComponent<GameObjectEntityRef>()?.Entity != null)
                {
                    throw new UnityException($"解谜命中区域已绑定：{paths[i]}");
                }
                areas[i] = area;
            }

            foreach (Collider2D area in areas)
            {
                if (!area.TryGetComponent(out GameObjectEntityRef bridge))
                {
                    bridge = area.gameObject.AddComponent<GameObjectEntityRef>();
                }
                bridge.Entity = self;
            }
            self.HitAreas = areas;
            self.Refresh(false);
        }

        /// <summary>按解谜模型同步拼图显隐和命中区域，并在有效交互后播放道具反馈。</summary>
        /// <param name="self">当前解谜模型的场景表现。</param>
        /// <param name="playFeedback">同步已提交的交互时播放反馈，首次绑定时仅同步状态。</param>
        public static void Refresh(this PuzzleRevealView self, bool playFeedback)
        {
            PuzzleRevealComponent reveal = self.GetParent<PuzzleRevealComponent>();
            bool locked = reveal.IsLocked;
            reveal.GetParent<Puzzle>().GetComponent<PuzzleView>().GameObject.SetActive(!locked);
            foreach (Collider2D area in self.HitAreas)
            {
                area.enabled = locked;
                if (playFeedback && area.TryGetComponent(out PropPunchAnimation feedback))
                {
                    feedback.Play();
                }
            }
        }

        /// <summary>关闭此模型的命中区域，并让仍在场景中的道具停止反馈。</summary>
        [EntitySystem]
        private static void Destroy(this PuzzleRevealView self)
        {
            if (self.HitAreas == null) return;
            foreach (Collider2D area in self.HitAreas)
            {
                if (area == null) continue;
                area.enabled = false;
                if (area.TryGetComponent(out PropPunchAnimation feedback)) feedback.Stop();
            }
            self.HitAreas = null;
        }
    }

    /// <summary>把已提交的解谜进度同步到对应实例的表现。</summary>
    [Event(SceneType.Current)]
    public class PuzzleRevealProgressChanged_RefreshView : AEvent<Scene, PuzzleRevealProgressChanged>
    {
        /// <summary>每次有效点击刷新一次表现，最后一次点击显示原托盘拼图。</summary>
        protected override async ETTask Run(Scene scene, PuzzleRevealProgressChanged args)
        {
            args.Reveal.GetComponent<PuzzleRevealView>()?.Refresh(true);
            await ETTask.CompletedTask;
        }
    }
}
