using UnityEngine;

namespace PuzzleGame
{
    /// <summary>播放当前拼图的六帧落位云尘并在结束后释放特效实例</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlacementEffectPlayer : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.01f)] private float duration = 0.42f;
        [SerializeField, Min(0.01f)] private float width = 4.5f;
        private SpriteRenderer spriteRenderer;
        private float elapsed;
        public bool IsPlaying { get; private set; }

        /// <summary>校验特效帧和播放参数，在关卡加载时暴露无效资源</summary>
        public void Validate()
        {
            if (frames == null || frames.Length != 6 || duration <= 0 || width <= 0)
            {
                throw new UnityException($"落位特效需要六帧图片和有效播放参数: {name}");
            }
            foreach (Sprite frame in frames)
            {
                if (frame == null || frame.bounds.size.x <= 0)
                {
                    throw new UnityException($"落位特效帧无效: {name}");
                }
            }
        }

        /// <summary>从第一帧开始播放当前特效实例</summary>
        private void OnEnable()
        {
            Validate();
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = frames[0];
            transform.localScale = Vector3.one * (width / frames[0].bounds.size.x);
            elapsed = 0;
            IsPlaying = true;
        }

        /// <summary>按玩法时间切换云尘图片，播放结束后销毁实例</summary>
        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                IsPlaying = false;
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            spriteRenderer.sprite = frames[Mathf.Min(frames.Length - 1, (int)(elapsed / duration * frames.Length))];
        }

        /// <summary>停用时结束当前特效播放状态</summary>
        private void OnDisable() => IsPlaying = false;
    }
}
