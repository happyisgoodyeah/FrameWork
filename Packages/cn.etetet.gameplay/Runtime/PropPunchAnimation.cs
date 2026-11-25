using DG.Tweening;
using UnityEngine;

namespace PuzzleGame
{
    /// <summary>对场景道具播放一次局部缩放回弹，可与旋转风摆同时播放。</summary>
    [DisallowMultipleComponent]
    public sealed class PropPunchAnimation : MonoBehaviour
    {
        [SerializeField, Tooltip("相对原始缩放的横向和纵向变化量，负值表示压缩。")]
        private Vector2 strength = new Vector2(0.045f, -0.09f);
        [SerializeField, Min(0.01f), Tooltip("一次回弹的时长，单位为秒。")]
        private float duration = 0.28f;
        [SerializeField, Min(1), Tooltip("回弹期间的振动次数。")]
        private int vibrato = 4;
        [SerializeField, Range(0f, 1f), Tooltip("回弹越过原始缩放的程度。")]
        private float elasticity = 0.4f;

        private Tween tween;
        private Vector3 restScale;

        /// <summary>从原始缩放播放回弹，连续触发时先复位，避免形变累积。</summary>
        public void Play()
        {
            if (!isActiveAndEnabled) return;
            Stop();
            restScale = transform.localScale;
            Vector3 punch = new Vector3(restScale.x * strength.x, restScale.y * strength.y, 0f);
            tween = transform.DOPunchScale(punch, duration, vibrato, elasticity)
                .SetTarget(this).SetAutoKill(true).OnKill(RestoreScale);
        }

        /// <summary>中断当前回弹并立即恢复道具缩放。</summary>
        public void Stop() => tween?.Kill();

        /// <summary>回弹自然结束或被中断时释放动画引用并复位。</summary>
        private void RestoreScale()
        {
            tween = null;
            transform.localScale = restScale;
        }

        /// <summary>道具或组件停用时结束当前动画。</summary>
        private void OnDisable() => Stop();
    }
}
