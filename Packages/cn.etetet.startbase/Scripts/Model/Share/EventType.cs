namespace ET.Client
{
    /// <summary>
    /// 子场景切换开始，发布对象是根 Scene。业务包在此加载对应的 Unity 场景资源
    /// </summary>
    public struct SceneChangeStart
    {
    }

    /// <summary>
    /// 子场景切换完成，发布对象是新建的子场景
    /// </summary>
    public struct SceneChangeFinish
    {
    }

    /// <summary>
    /// 子场景实体创建完成，发布对象是新建的子场景。业务包在此挂载关卡所需组件
    /// </summary>
    public struct AfterCreateCurrentScene
    {
    }

    /// <summary>
    /// 框架初始化完成，发布对象是根 Scene。业务包在此接管启动流程
    /// </summary>
    public struct AppStartInitFinish
    {
    }

    /// <summary>
    /// 进入子场景的完整流程结束，发布对象是根 Scene
    /// </summary>
    public struct EnterMapFinish
    {
    }
}
