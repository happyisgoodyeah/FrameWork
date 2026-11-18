namespace ET.Client
{
    public static class LocalSceneHelper
    {
        /// <summary>
        /// 切换到指定的本地子场景，销毁旧子场景后重建，并驱动完整的切换事件流程
        /// </summary>
        /// <param name="root">根场景</param>
        /// <param name="sceneName">目标子场景名字，业务包据此加载对应的 Unity 场景</param>
        public static async ETTask<Scene> ChangeSceneAsync(Scene root, string sceneName)
        {
            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();

            //销毁旧子场景，其下的资源加载器、输入和关卡数据一并级联释放
            currentScenesComponent.Scene?.Dispose();

            long sceneInstanceId = IdGenerater.Instance.GenerateInstanceId();
            Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);

            //先发切换开始，业务包在此加载 Unity 场景资源
            await EventSystem.Instance.PublishAsync(root, new SceneChangeStart());

            EventSystem.Instance.Publish(currentScene, new SceneChangeFinish());

            //通知等待切换完成的协程
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());

            EventSystem.Instance.Publish(root, new EnterMapFinish());

            return currentScene;
        }
    }
}
