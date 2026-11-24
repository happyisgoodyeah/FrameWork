using UnityEngine;

namespace PuzzleGame
{
    /// <summary>按玩法相机视口拟合场景主题的背景与装饰根节点</summary>
    [DisallowMultipleComponent]
    public sealed class EnvironmentBackdrop : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Transform contentRoot;
        private Camera sceneCamera;

        /// <summary>绑定玩法相机并调整主题覆盖范围</summary>
        /// <param name="camera">当前玩法使用的正交相机</param>
        public void Configure(Camera camera)
        {
            if (camera == null || !camera.orthographic || background == null || background.sprite == null || contentRoot == null)
            {
                throw new UnityException($"场景主题需要背景、装饰根节点和正交相机: {name}");
            }
            sceneCamera = camera;
            FitCamera();
        }

        /// <summary>在相机视口变化后同步背景覆盖范围</summary>
        private void LateUpdate()
        {
            if (sceneCamera != null) FitCamera();
        }

        /// <summary>将整组美术缩放至覆盖视口，并把底图中心对准相机的视线。</summary>
        private void FitCamera()
        {
            Vector2 size = background.sprite.bounds.size;
            float height = sceneCamera.orthographicSize * 2;
            Vector3 scale = Vector3.one * Mathf.Max(height * sceneCamera.aspect / size.x, height / size.y);
            if (contentRoot.localScale != scale) contentRoot.localScale = scale;
            Vector3 toCamera = sceneCamera.transform.position - background.bounds.center;
            Vector3 offset = Vector3.ProjectOnPlane(toCamera, sceneCamera.transform.forward);
            if (offset != Vector3.zero) contentRoot.position += offset;
        }
    }
}
