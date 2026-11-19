using UnityEngine;

namespace PuzzleGame
{
    /// <summary>
    /// 为导入的前三关提供环境和槽位外观，装饰随 Grid 销毁且不参与占格计算
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class LevelSceneArt : MonoBehaviour
    {
        [SerializeField] private Sprite[] backgrounds = new Sprite[3];
        [SerializeField] private Sprite[] floorTiles = new Sprite[3];
        [SerializeField] private Sprite[] obstacles = new Sprite[3];
        [SerializeField] private GameObject[] environmentLayers = new GameObject[3];
        [SerializeField] private Sprite roomBearObstacle;

        private int theme = -1;
        private SpriteRenderer backdrop;
        private Camera sceneCamera;
        private Camera cameraOverride;

        public int Theme => theme;

        /// <summary>安装导入关卡的背景和棋盘表现</summary>
        /// <param name="levelId">当前关卡配置 ID</param>
        /// <param name="previewCamera">当前玩法使用的正交相机</param>
        /// <returns>当前关卡是否使用导入主题</returns>
        public bool Configure(int levelId, Camera previewCamera)
        {
            int index = levelId - 1101;
            if (index < 0 || index >= 3)
            {
                return false;
            }

            if (theme >= 0)
            {
                throw new UnityException($"关卡主题不能重复初始化: {levelId}");
            }

            if (index >= backgrounds.Length || index >= floorTiles.Length || index >= obstacles.Length ||
                index >= environmentLayers.Length || backgrounds[index] == null || floorTiles[index] == null ||
                obstacles[index] == null || environmentLayers[index] == null)
            {
                throw new UnityException($"关卡主题资源不完整: {levelId}");
            }

            if (previewCamera == null || !previewCamera.orthographic)
            {
                throw new UnityException($"关卡主题需要玩法正交相机: {levelId}");
            }

            theme = index;
            cameraOverride = previewCamera;
            Transform icon = transform.Find("Icon");
            if (icon != null) icon.gameObject.SetActive(false);
            backdrop = CreateRenderer("Environment", transform, backgrounds[index], "BackGround", 100);
            GameObject layers = Instantiate(environmentLayers[index], backdrop.transform, false);
            layers.name = "EnvironmentLayers";
            SpriteRenderer frame = CreateRenderer("BoardMat", transform, floorTiles[index], "Grid", -10);
            // 用同材质的大底垫连接九个地格；实际碰撞范围仍为原来的 9×9。
            frame.transform.localScale = Vector3.one * (11.3f / 3f);
            GetComponent<PlacementFeedback>()?.Configure(frame.transform, index);
            if (index == 0)
            {
                Transform basketTransform = layers.transform.Find("PuzzleProps/RevealBasket");
                if (basketTransform == null || !basketTransform.TryGetComponent(out SpriteRenderer basket))
                {
                    throw new UnityException($"首关主题缺少篮筐表现节点: {levelId}");
                }

                gameObject.AddComponent<LevelOneBasketReveal>().Configure(basket);
            }
            FitCamera();
            return true;
        }

        /// <summary>替换槽位贴图并保留原有碰撞和坐标</summary>
        /// <param name="slot">棋盘槽位的表现节点</param>
        /// <param name="allowPlace">当前槽位是否允许放置拼图</param>
        public void ApplySlot(Transform slot, bool allowPlace)
        {
            if (theme < 0) return;
            Transform square = slot.Find("Square");
            if (square == null) return;
            // 保留 Square 的原始 Sprite 和尺寸供 ET 坐标、吸附逻辑使用。
            square.GetComponent<SpriteRenderer>().enabled = false;
            Transform previous = slot.Find("SceneSurface");
            SpriteRenderer surface = previous != null ? previous.GetComponent<SpriteRenderer>() :
                CreateRenderer("SceneSurface", slot, floorTiles[theme], "Slot", 0);
            surface.sprite = allowPlace ? floorTiles[theme] :
                theme == 0 && roomBearObstacle != null ? roomBearObstacle : obstacles[theme];
            GetComponent<PlacementFeedback>()?.RegisterSurface(surface.transform);
        }

        /// <summary>在指定排序层创建独立的表现节点</summary>
        /// <param name="name">节点名称</param>
        /// <param name="parent">表现挂载节点</param>
        /// <param name="sprite">表现使用的贴图</param>
        /// <param name="layer">渲染排序层</param>
        /// <param name="order">层内渲染顺序</param>
        /// <returns>创建完成的表现组件</returns>
        private static SpriteRenderer CreateRenderer(string name, Transform parent, Sprite sprite, string layer, int order)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
            return renderer;
        }

        /// <summary>在玩法相机更新后调整背景覆盖范围</summary>
        private void LateUpdate()
        {
            if (theme >= 0) FitCamera();
        }

        /// <summary>按玩法相机的视口缩放背景</summary>
        private void FitCamera()
        {
            if (sceneCamera == null)
            {
                Camera camera = cameraOverride != null ? cameraOverride : Camera.main;
                if (camera == null || !camera.orthographic) return;
                sceneCamera = camera;
            }

            float aspect = Mathf.Max(0.1f, sceneCamera.aspect);
            if (backdrop == null) return;
            Vector2 size = backdrop.sprite.bounds.size;
            float height = sceneCamera.orthographicSize * 2;
            float scale = Mathf.Max(height * aspect / size.x, height / size.y);
            backdrop.transform.localScale = Vector3.one * scale;
        }

    }
}
