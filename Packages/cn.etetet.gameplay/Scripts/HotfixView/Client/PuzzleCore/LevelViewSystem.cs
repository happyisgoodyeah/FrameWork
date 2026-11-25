using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 按关卡配表加载主题资源并管理关卡表现生命周期
    /// </summary>
    [EntitySystemOf(typeof(LevelView))]
    public static partial class LevelViewSystem
    {
        /// <summary>初始化当前关卡的表现引用</summary>
        [EntitySystem]
        private static void Awake(this LevelView self)
        {
            self.IsInitialized = false;
            self.SceneObject = null;
            self.UIPrefab = null;
            self.UIObject = null;
            self.PlacementEffectPrefab = null;
            self.LastPlacementView = default;
        }

        /// <summary>按配置创建场景主题并预加载 UI 主题和落位特效。</summary>
        /// <param name="self">当前关卡的表现组件</param>
        /// <param name="config">当前关卡配置</param>
        public static async ETTask LoadAsync(this LevelView self, GridConfig config)
        {
            EntityRef<LevelView> selfRef = self;
            Scene scene = self.Scene();
            ResourcesLoaderComponent loader = scene.GetComponent<ResourcesLoaderComponent>();
            PuzzleSceneRoot sceneRoot = scene.GetComponent<PuzzleSceneRoot>();
            if (!string.IsNullOrEmpty(config.ThemeScenePrefabPath))
            {
                GameObject prefab = await loader.LoadAssetAsync<GameObject>(ResourceLocationHelper.ToAssetLocation(config.ThemeScenePrefabPath));
                self = selfRef;
                if (self == null) return;
                self.SceneObject = UnityEngine.Object.Instantiate(prefab, sceneRoot.SceneRoot, false);
                self.SceneObject.name = "ThemeScene";
                if (self.SceneObject.TryGetComponent(out PuzzleGame.EnvironmentBackdrop backdrop))
                {
                    backdrop.Configure(sceneRoot.GameCamera);
                }
            }

            if (!string.IsNullOrEmpty(config.ThemeUIPrefabPath))
            {
                GameObject prefab = await loader.LoadAssetAsync<GameObject>(ResourceLocationHelper.ToAssetLocation(config.ThemeUIPrefabPath));
                self = selfRef;
                if (self == null) return;
                if (prefab.GetComponent<RectTransform>() == null)
                {
                    throw new UnityException($"主题 UI 预制体根节点必须是 RectTransform: {config.ThemeUIPrefabPath}");
                }
                self.UIPrefab = prefab;
            }

            if (!string.IsNullOrEmpty(config.PlacementEffectPrefabPath))
            {
                GameObject prefab = await loader.LoadAssetAsync<GameObject>(ResourceLocationHelper.ToAssetLocation(config.PlacementEffectPrefabPath));
                self = selfRef;
                if (self == null) return;
                if (!prefab.TryGetComponent(out PuzzleGame.PlacementEffectPlayer player))
                {
                    throw new UnityException($"落位特效缺少播放器: {config.PlacementEffectPrefabPath}");
                }
                player.Validate();
                self.PlacementEffectPrefab = prefab;
            }
            self.IsInitialized = true;
        }

        /// <summary>在关卡 UI 打开时挂载主题装饰，复用本关已有的实例</summary>
        /// <param name="self">当前关卡的表现组件</param>
        /// <param name="parent">GameView 的主题 UI 挂点</param>
        public static void ShowUI(this LevelView self, RectTransform parent)
        {
            if (!self.IsInitialized) throw new UnityException("关卡主题资源尚未加载完成");
            if (self.UIPrefab == null) return;
            if (self.UIObject == null)
            {
                self.UIObject = UnityEngine.Object.Instantiate(self.UIPrefab, parent, false);
                self.UIObject.name = "ThemeUI";
            }
            else
            {
                self.UIObject.transform.SetParent(parent, false);
            }
        }

        /// <summary>关卡 UI 视图释放时清除主题实例，保留资源供再次打开使用</summary>
        /// <param name="self">当前关卡的表现组件</param>
        public static void ReleaseUI(this LevelView self)
        {
            if (self.UIObject == null) return;
            self.UIObject.SetActive(false);
            UnityEngine.Object.Destroy(self.UIObject);
            self.UIObject = null;
        }

        /// <summary>关卡释放时停止并销毁场景和 UI 主题实例</summary>
        [EntitySystem]
        private static void Destroy(this LevelView self)
        {
            self.IsInitialized = false;
            if (self.SceneObject != null)
            {
                self.SceneObject.SetActive(false);
                UnityEngine.Object.Destroy(self.SceneObject);
            }
            self.ReleaseUI();
            self.SceneObject = null;
            self.UIObject = null;
            self.UIPrefab = null;
            self.PlacementEffectPrefab = null;
            self.LastPlacementView = default;
        }
    }
}
