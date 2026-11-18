using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace ET.Client
{
    /// <summary>
    /// PuzzleSceneRoot 的初始化和根节点查找逻辑
    /// </summary>
    [EntitySystemOf(typeof(PuzzleSceneRoot))]
    public static partial class PuzzleSceneRootSystem
    {
        private const string SceneRootName = "SceneRoot";
        private const string GridRootName = "GridRoot";

        /// <summary>
        /// 初始化缓存状态
        /// </summary>
        [EntitySystem]
        private static void Awake(this PuzzleSceneRoot self)
        {
            self.SceneRoot = null;
            self.GridRoot = null;
            self.GameCamera = null;
            self.IsInitialized = false;
        }

        /// <summary>
        /// 释放缓存引用
        /// </summary>
        [EntitySystem]
        private static void Destroy(this PuzzleSceneRoot self)
        {
            self.SceneRoot = null;
            self.GridRoot = null;
            self.GameCamera = null;
            self.IsInitialized = false;
        }

        /// <summary>
        /// 按当前屏幕比例拟合相机，保证内容范围在任意比例下都不被裁切。
        /// 朝向已锁定横屏，运行期屏幕尺寸不会变化，因此只在生成关卡时调用一次。
        /// </summary>
        public static void FitGameCamera(this PuzzleSceneRoot self)
        {
            if (!self.TryGetGameCamera(out Camera gameCamera) || !gameCamera.orthographic)
            {
                return;
            }

            float aspect = gameCamera.aspect;
            if (aspect <= 0f)
            {
                return;
            }

            // orthographicSize 即可见半高，可见半宽为 orthographicSize * aspect
            // 两个方向都取满足条件的下限，再取较大者，等价于 contain 语义
            float sizeByHeight = PuzzleLayoutConst.RequiredHalfHeight;
            float sizeByWidth = PuzzleLayoutConst.RequiredHalfWidth / aspect;
            gameCamera.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);
        }

        /// <summary>
        /// 获取当前用于渲染玩法内容的相机，缓存失效时重新解析
        /// </summary>
        /// <param name="self">PuzzleSceneRoot 自身</param>
        /// <param name="gameCamera">返回找到的相机</param>
        /// <returns>是否成功找到可用相机</returns>
        private static bool TryGetGameCamera(this PuzzleSceneRoot self, out Camera gameCamera)
        {
            gameCamera = self.GameCamera;
            if (gameCamera != null && gameCamera.isActiveAndEnabled)
            {
                return true;
            }

            // 玩法相机由 Init 场景的 MainCamera 通过 DontDestroyOnLoad 跨场景保留
            gameCamera = Camera.main;
            self.GameCamera = gameCamera;
            return gameCamera != null;
        }

        /// <summary>
        /// 优先按 ET Scene 名称解析对应的 Unity 场景，找不到时再回退到当前激活场景
        /// </summary>
        public static bool TryGetUnityScene(this Scene etScene, out UnityScene unityScene)
        {
            unityScene = default;

            UnityScene namedUnityScene = UnitySceneManager.GetSceneByName(etScene.Name);
            if (namedUnityScene.IsValid() && namedUnityScene.isLoaded)
            {
                unityScene = namedUnityScene;
                return true;
            }

            UnityScene activeUnityScene = UnitySceneManager.GetActiveScene();
            if (activeUnityScene.IsValid() && activeUnityScene.isLoaded && activeUnityScene.name == etScene.Name)
            {
                unityScene = activeUnityScene;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 判断当前 ET 场景是否已经具备创建 PuzzleCore View 的场景与资源加载条件
        /// </summary>
        /// <param name="etScene">当前 ET 逻辑场景</param>
        /// <returns>是否可以安全创建 GridView、PuzzleView 和 SlotView</returns>
        public static bool IsPuzzleViewReady(this Scene etScene)
        {
            return etScene != null && !etScene.IsDisposed && etScene.TryGetUnityScene(out _) && etScene.GetComponent<ResourcesLoaderComponent>() != null;
        }

        /// <summary>
        /// 初始化当前 ET Scene 对应的 Unity 场景根节点缓存
        /// </summary>
        public static PuzzleSceneRoot InitializePuzzleSceneRoot(this Scene etScene)
        {
            PuzzleSceneRoot puzzleSceneRoot = etScene.GetComponent<PuzzleSceneRoot>();
            if (puzzleSceneRoot == null)
            {
                puzzleSceneRoot = etScene.AddComponent<PuzzleSceneRoot>();
            }

            if (puzzleSceneRoot.IsInitialized)
            {
                return puzzleSceneRoot;
            }

            if (!etScene.TryGetUnityScene(out UnityScene unityScene))
            {
                UnityScene activeUnityScene = UnitySceneManager.GetActiveScene();
                throw new UnityException($"can not resolve unity scene for et scene: etScene={etScene.Name} activeUnityScene={activeUnityScene.name}");
            }

            Transform sceneRoot = FindSceneRootTransform(unityScene);
            if (sceneRoot == null)
            {
                throw new UnityException($"can not find {SceneRootName} in unity scene: {unityScene.name}");
            }

            Transform gridRoot = sceneRoot.Find(GridRootName);
            if (gridRoot == null)
            {
                throw new UnityException($"can not find {GridRootName} under {SceneRootName} in unity scene: {unityScene.name}");
            }

            puzzleSceneRoot.SceneRoot = sceneRoot;
            puzzleSceneRoot.GridRoot = gridRoot;
            puzzleSceneRoot.IsInitialized = true;

            // 生成时拟合一次，朝向锁定横屏后运行期不再变化
            puzzleSceneRoot.FitGameCamera();
            return puzzleSceneRoot;
        }

        /// <summary>
        /// 在目标 Unity 场景的根节点中查找 PuzzleCore 使用的 SceneRoot
        /// </summary>
        private static Transform FindSceneRootTransform(UnityScene unityScene)
        {
            foreach (GameObject rootGameObject in unityScene.GetRootGameObjects())
            {
                if (rootGameObject.name == SceneRootName)
                {
                    return rootGameObject.transform;
                }
            }

            return null;
        }
    }
}
