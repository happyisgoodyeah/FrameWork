namespace ET
{
    /// <summary>
    /// 当前子场景的持有者，挂在根 Scene 上，整个客户端只有一个。
    /// 作用有两条：
    /// 一是作为子场景的父节点，Dispose 时连同子场景的资源加载器、输入、关卡数据一起级联销毁；
    /// 二是给实体树外的代码（例如 UI 面板）提供定位入口，通过 root.CurrentScene() 拿到当前子场景。
    /// 目前同一时刻只持有一个子场景，切换时先销毁旧的再创建新的。
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class CurrentScenesComponent: Entity, IAwake
    {
        private EntityRef<Scene> scene;

        public Scene Scene
        {
            get
            {
                return this.scene;
            }
            set
            {
                this.scene = value;
            }
        }
    }
}
