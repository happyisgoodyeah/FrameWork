using System.Collections.Generic;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>成功放置时播放六帧云尘，枕头回弹只影响外观，不移动槽位或碰撞体</summary>
    [DisallowMultipleComponent]
    public sealed class PlacementFeedback : MonoBehaviour
    {
        [SerializeField] private Sprite[] cloudFrames;
        private readonly Dictionary<Transform, Vector3> pillowSurfaces = new Dictionary<Transform, Vector3>();
        private readonly List<Puff> puffs = new List<Puff>();
        private Transform board;
        private Vector3 boardScale;
        private float pillowTime = 1;
        private int nextPuff;
        private bool pillow;
        private static readonly float[] Width = { 1.025f, 1.055f, 1.025f, 0.98f, 1.01f, 1 };
        private static readonly float[] Height = { 0.97f, 0.91f, 0.965f, 1.035f, 0.99f, 1 };

        public const int LandingDurationMilliseconds = 420;
        public const float LandingDuration = LandingDurationMilliseconds / 1000f;
        public bool IsPlaying => pillowTime < LandingDuration || puffs.Exists(value => value.Renderer.enabled);

        /// <summary>记录棋盘表现的原始缩放并启用对应主题的回弹</summary>
        /// <param name="boardMat">独立于槽位和碰撞体的棋盘底垫</param>
        /// <param name="theme">当前主题索引</param>
        public void Configure(Transform boardMat, int theme)
        {
            if (boardMat == null || cloudFrames == null || cloudFrames.Length != 6)
            {
                throw new UnityException("落位反馈需要棋盘底垫和完整的六帧云尘");
            }

            foreach (Sprite frame in cloudFrames)
            {
                if (frame == null || frame.bounds.size.x <= 0)
                {
                    throw new UnityException("落位反馈的云尘帧无效");
                }
            }

            board = boardMat;
            boardScale = board.localScale;
            pillow = theme == 0;
        }

        /// <summary>记录参与回弹的槽位外观及其原始缩放</summary>
        /// <param name="surface">独立于槽位碰撞体的外观节点</param>
        public void RegisterSurface(Transform surface)
        {
            if (!pillowSurfaces.ContainsKey(surface)) pillowSurfaces.Add(surface, surface.localScale);
        }

        /// <summary>在拼图落位处播放云尘并触发当前主题的回弹</summary>
        /// <param name="position">反馈出现的世界坐标</param>
        public void PlayLanding(Vector3 position)
        {
            if (pillow) pillowTime = 0;
            PlayCloud(position, 4.5f, LandingDuration);
            Advance(0);
        }

        /// <summary>在篮筐揭晓处播放遮挡烟尘</summary>
        /// <param name="position">烟尘出现的世界坐标</param>
        public void PlayReveal(Vector3 position)
        {
            PlayCloud(position, 6f, 0.6f);
        }

        /// <summary>从有固定上限的缓存中取出云尘并重置本次播放状态</summary>
        /// <param name="position">云尘出现的世界坐标</param>
        /// <param name="size">云尘的表现宽度</param>
        /// <param name="duration">本次播放时长</param>
        private void PlayCloud(Vector3 position, float size, float duration)
        {
            // 固定上限，连续快速放置时复用最早的一团云，不无限生成对象。
            Puff puff;
            if (puffs.Count < 8)
            {
                GameObject child = new GameObject("CartoonCloud");
                child.transform.SetParent(transform, false);
                SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
                renderer.sortingLayerName = "Puzzle";
                renderer.sortingOrder = 100;
                puff = new Puff { Renderer = renderer };
                puffs.Add(puff);
            }
            else
            {
                puff = puffs[nextPuff];
                nextPuff = (nextPuff + 1) % puffs.Count;
            }
            puff.Age = 0;
            puff.Duration = duration;
            puff.Renderer.transform.position = position;
            puff.Renderer.transform.localScale = Vector3.one * size / cloudFrames[0].bounds.size.x;
            puff.Renderer.sprite = cloudFrames[0];
            puff.Renderer.enabled = true;
        }

        /// <summary>按玩法时间推进落位反馈</summary>
        private void Update() => Advance(Time.deltaTime);

        /// <summary>推进六帧云尘并按原始缩放采样回弹</summary>
        /// <param name="seconds">本次推进的秒数</param>
        public void Advance(float seconds)
        {
            pillowTime += Mathf.Max(0, seconds);
            Vector3 scale = Vector3.one;
            if (pillow && pillowTime < LandingDuration)
            {
                int frame = Mathf.Min(5, (int)(pillowTime / LandingDuration * 6));
                scale = new Vector3(Width[frame], Height[frame], 1);
            }
            if (board != null) board.localScale = Vector3.Scale(boardScale, scale);
            foreach (KeyValuePair<Transform, Vector3> surface in pillowSurfaces)
                if (surface.Key != null) surface.Key.localScale = Vector3.Scale(surface.Value, scale);
            foreach (Puff puff in puffs)
            {
                if (!puff.Renderer.enabled) continue;
                puff.Age += Mathf.Max(0, seconds);
                if (puff.Age >= puff.Duration)
                {
                    puff.Renderer.enabled = false;
                    continue;
                }
                int frame = Mathf.Min(cloudFrames.Length - 1, (int)(puff.Age / puff.Duration * cloudFrames.Length));
                puff.Renderer.sprite = cloudFrames[frame];
            }
        }

        /// <summary>停用反馈时恢复原始缩放并隐藏所有云尘</summary>
        private void OnDisable()
        {
            pillowTime = LandingDuration;
            if (board != null) board.localScale = boardScale;
            foreach (KeyValuePair<Transform, Vector3> surface in pillowSurfaces)
                if (surface.Key != null) surface.Key.localScale = surface.Value;
            foreach (Puff puff in puffs)
                if (puff.Renderer != null) puff.Renderer.enabled = false;
        }

        private sealed class Puff
        {
            public SpriteRenderer Renderer;
            public float Age;
            public float Duration;
        }
    }
}
