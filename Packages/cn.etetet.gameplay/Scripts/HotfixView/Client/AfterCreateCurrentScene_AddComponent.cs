namespace ET.Client
{
    /// <summary>
    /// 子场景创建完成后挂载关卡运行所需组件，这些组件随子场景一起销毁
    /// </summary>
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddComponent: AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override ETTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            if (scene.GetComponent<ResourcesLoaderComponent>() == null)
            {
                scene.AddComponent<ResourcesLoaderComponent>();
            }

            if (scene.GetComponent<InputComponent>() == null)
            {
                scene.AddComponent<InputComponent>();
            }

            return ETTask.CompletedTask;
        }
    }
}
