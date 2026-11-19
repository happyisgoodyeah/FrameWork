using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// 播放拼图的待机和拎起帧，仅更新画面，不修改变换或碰撞区域
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PuzzleIdleAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField, Min(0.1f)] private float framesPerSecond = 5f;
        [SerializeField] private Sprite[] pickedUpFrames = new Sprite[0];
        [SerializeField, Min(0.1f)] private float pickedUpFramesPerSecond = 8f;
        [SerializeField, Min(0)] private int heldLoopStart = 2;

        private SpriteRenderer spriteRenderer;
        private double elapsed;
        private int currentFrame = -1;
        private bool isPickedUp;

        public int FrameCount => frames.Length;
        public float FramesPerSecond => framesPerSecond;
        public int PickedUpFrameCount => pickedUpFrames.Length;
        public bool IsPickedUp => isPickedUp;

        /// <summary>启用拼图时从待机首帧开始播放</summary>
        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            isPickedUp = false;
            elapsed = 0;
            currentFrame = -1;
            ShowFrame(0);
        }

        /// <summary>使用不受玩法暂停影响的时间推进动画</summary>
        private void Update()
        {
            Advance(Time.unscaledDeltaTime);
        }

        /// <summary>切换待机与拎起状态，重复设置相同状态不重播开场</summary>
        /// <param name="value">是否播放拎起动画</param>
        public void SetPickedUp(bool value)
        {
            value = value && pickedUpFrames.Length > 0;
            if (isPickedUp == value) return;
            isPickedUp = value;
            elapsed = 0;
            currentFrame = -1;
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            ShowFrame(0);
        }

        /// <summary>停用拼图时恢复待机表现</summary>
        private void OnDisable() => SetPickedUp(false);

        /// <summary>应用失去焦点时取消拎起表现</summary>
        /// <param name="focused">应用是否获得焦点</param>
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) SetPickedUp(false);
        }

        /// <summary>待机完整循环，拎起开场只播一次后循环悬停片段</summary>
        /// <param name="deltaTime">本次推进的秒数</param>
        public void Advance(float deltaTime)
        {
            Sprite[] activeFrames = isPickedUp ? pickedUpFrames : frames;
            if (activeFrames.Length == 0)
            {
                return;
            }

            // 使用独立时间，暂停玩法或拖拽时也继续播放待机动画。
            double fps = Mathf.Max(0.1f, isPickedUp ? pickedUpFramesPerSecond : framesPerSecond);
            int loopStart = isPickedUp ? Mathf.Clamp(heldLoopStart, 0, activeFrames.Length - 1) : 0;
            double duration = activeFrames.Length / fps;
            elapsed += Mathf.Max(0, deltaTime);
            if (elapsed >= duration)
                elapsed = loopStart / fps + (elapsed - duration) % ((activeFrames.Length - loopStart) / fps);
            int index = Mathf.Min((int)(elapsed * fps), activeFrames.Length - 1);
            ShowFrame(index);
        }

        /// <summary>显示当前状态的指定帧并跳过重复帧</summary>
        /// <param name="index">当前状态内的帧索引</param>
        private void ShowFrame(int index)
        {
            Sprite[] activeFrames = isPickedUp ? pickedUpFrames : frames;
            if (activeFrames.Length == 0 || index == currentFrame || activeFrames[index] == null)
            {
                return;
            }

            currentFrame = index;
            spriteRenderer.sprite = activeFrames[index];
        }
    }
}
