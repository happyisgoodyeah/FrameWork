using YIUIFramework;

namespace ET.Client
{
    [Event(SceneType.Game)]
    public class EntryEvent3_InitStartBaseClient: AEvent<Scene, EntryEvent3>
    {
        /// <summary>
        /// 处理客户端第三阶段初始化，装配单机框架运行时组件
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="args">入口事件参数</param>
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            //缓存 Global、GlobalUI、GlobalConfig 等全局 Unity 节点和配置
            root.AddComponent<GlobalComponent>();

            //业务期资源加载器
            root.AddComponent<ResourcesLoaderComponent>();

            //子场景容器，关卡切换时由业务包创建和销毁具体场景
            root.AddComponent<CurrentScenesComponent>();

            //存档系统管理器，路径由客户端持久化目录注入
            SaveManagerComponent saveManagerComponent = root.AddComponent<SaveManagerComponent>();
            SaveResult saveResult = await saveManagerComponent.InitializeClientAsync();
            if (saveResult != SaveResult.Success)
            {
                Log.Error($"初始化存档系统失败: {saveResult}");
                return;
            }

            //创建 YIUIMgrComponent，让 UI 框架真正启动
            bool result = await root.AddComponent<YIUIMgrComponent>().Initialize();
            if (!result)
            {
                Log.Error("初始化UI失败");
                return;
            }

            //框架就绪，由安装的业务包自行响应
            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
        }
    }
}
