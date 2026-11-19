using UnityEngine;

namespace PuzzleGame
{
    /// <summary>管理首关篮筐三次点击的显隐和揭晓动画，Chick 数据由 ET 管理</summary>
    [DisallowMultipleComponent]
    public sealed class LevelOneBasketReveal : MonoBehaviour
    {
        private SpriteRenderer basket;
        private GameObject chick;
        private Collider2D[] chickColliders;
        private bool[] colliderStates;
        private Vector3 basketRest;
        private Vector3 basketScale;
        private Quaternion basketRotation;
        private Vector3 chickRest;
        private Vector3 chickScale;
        private Vector3 emergence;
        private float elapsed;
        private float shakeTime = 1;
        private bool smokePlayed;
        private bool chickShown;

        public int ClickCount { get; private set; }
        public bool IsRevealed { get; private set; }
        public bool IsRevealing => ClickCount == 3 && !IsRevealed;

        /// <summary>记录篮筐的初始变换</summary>
        /// <param name="basketRenderer">首关篮筐的表现节点</param>
        public void Configure(SpriteRenderer basketRenderer)
        {
            if (basketRenderer == null)
            {
                throw new UnityException("篮筐揭晓缺少篮筐表现节点");
            }

            basket = basketRenderer;
            basketRest = basket.transform.localPosition;
            basketScale = basket.transform.localScale;
            basketRotation = basket.transform.localRotation;
        }

        /// <summary>记录小鸡的待放位置并在揭晓前隐藏</summary>
        /// <param name="puzzle">当前关卡内唯一参与揭晓的小鸡表现</param>
        public void RegisterChick(GameObject puzzle)
        {
            if (basket == null || puzzle == null || chick != null)
            {
                throw new UnityException("篮筐揭晓必须在配置篮筐后注册唯一的小鸡表现");
            }

            chick = puzzle;
            chickRest = puzzle.transform.position;
            chickScale = puzzle.transform.localScale;
            chick.SetActive(false);
        }

        /// <summary>处理通用点击事件已确认的起点和终点</summary>
        /// <param name="pressWorldPosition">按下时的世界坐标</param>
        /// <param name="releaseWorldPosition">松开时的世界坐标</param>
        /// <returns>本次点击是否计入揭晓次数</returns>
        public bool TryClick(Vector3 pressWorldPosition, Vector3 releaseWorldPosition)
        {
            if (!isActiveAndEnabled || Time.timeScale == 0 || ClickCount >= 3 || chick == null ||
                !HitBasket(pressWorldPosition) || !HitBasket(releaseWorldPosition))
            {
                return false;
            }

            ++ClickCount;
            shakeTime = 0;
            if (ClickCount == 3) elapsed = 0;
            return true;
        }

        /// <summary>推进篮筐揭晓的表现动画</summary>
        private void Update()
        {
            Advance(Time.deltaTime);
        }

        /// <summary>按世界坐标检测篮筐可见区域</summary>
        /// <param name="point">待检测的世界坐标</param>
        /// <returns>坐标是否位于当前篮筐内</returns>
        private bool HitBasket(Vector3 point)
        {
            if (basket == null || !basket.enabled) return false;
            Bounds bounds = basket.bounds;
            return point.x >= bounds.min.x && point.x <= bounds.max.x &&
                point.y >= bounds.min.y && point.y <= bounds.max.y;
        }

        /// <summary>推进篮筐拉出和小鸡飞入待放区的动画，结束后恢复原有碰撞状态</summary>
        /// <param name="seconds">本次推进的秒数</param>
        public void Advance(float seconds)
        {
            if (basket == null) return;
            seconds = Mathf.Max(0, seconds);
            shakeTime += seconds;
            if (ClickCount < 3)
            {
                float shake = shakeTime < 0.28f ? Mathf.Sin(shakeTime * 65) * (1 - shakeTime / 0.28f) : 0;
                basket.transform.localRotation = basketRotation * Quaternion.Euler(0, 0, shake * 4);
                return;
            }
            if (IsRevealed) return;
            elapsed += seconds;
            float pull = Mathf.SmoothStep(0, 1, elapsed / 0.45f);
            basket.transform.localPosition = basketRest + new Vector3(-0.25f, -1.8f, 0) * pull;
            basket.transform.localScale = basketScale * Mathf.Lerp(1, 1.08f, pull);
            basket.transform.localRotation = basketRotation * Quaternion.Euler(0, 0, Mathf.Sin(pull * Mathf.PI) * -5);
            if (elapsed >= 0.45f && !smokePlayed)
            {
                smokePlayed = true;
                emergence = basket.bounds.center + Vector3.up * 0.6f;
                GetComponent<PlacementFeedback>()?.PlayReveal(emergence);
            }
            if (elapsed < 0.65f || chick == null) return;
            if (!chickShown)
            {
                chickShown = true;
                // Puzzle 的槽位视图异步生成，揭晓时再收集，包含后加载的子碰撞体。
                chickColliders = chick.GetComponentsInChildren<Collider2D>(true);
                colliderStates = new bool[chickColliders.Length];
                for (int i = 0; i < chickColliders.Length; ++i) colliderStates[i] = chickColliders[i].enabled;
                for (int i = 0; i < chickColliders.Length; ++i) chickColliders[i].enabled = false;
                chick.SetActive(true);
            }
            float travel = Mathf.SmoothStep(0, 1, (elapsed - 0.65f) / 0.55f);
            chick.transform.position = Vector3.Lerp(emergence, chickRest, travel) + Vector3.up * Mathf.Sin(travel * Mathf.PI) * 1.5f;
            chick.transform.localScale = chickScale * Mathf.Lerp(0.3f, 1, Mathf.Clamp01(travel * 2));
            if (elapsed < 1.2f) return;
            chick.transform.position = chickRest;
            chick.transform.localScale = chickScale;
            for (int i = 0; i < chickColliders.Length; ++i) chickColliders[i].enabled = colliderStates[i];
            IsRevealed = true;
        }

    }
}
