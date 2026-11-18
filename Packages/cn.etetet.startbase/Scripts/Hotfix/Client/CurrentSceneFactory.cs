namespace ET.Client
{
    public static class CurrentSceneFactory
    {
        /// <summary>
        /// 创建子场景并挂到持有者下，创建完成后发布 AfterCreateCurrentScene 供业务包挂载组件
        /// </summary>
        /// <param name="id">子场景 Id</param>
        /// <param name="name">子场景名字，业务包据此决定加载哪个 Unity 场景</param>
        /// <param name="currentScenesComponent">子场景持有者</param>
        public static Scene Create(long id, string name, CurrentScenesComponent currentScenesComponent)
        {
            Scene currentScene = EntitySceneFactory.CreateScene(currentScenesComponent, id, IdGenerater.Instance.GenerateInstanceId(), SceneType.Current, name);
            currentScenesComponent.Scene = currentScene;

            EventSystem.Instance.Publish(currentScene, new AfterCreateCurrentScene());
            return currentScene;
        }
    }
}
