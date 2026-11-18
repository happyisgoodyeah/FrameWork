namespace ET.Client
{
    [Event(SceneType.Game)]
    public class AppStartInitFinish_EnterPuzzle: AEvent<Scene, AppStartInitFinish>
    {
        /// <summary>
        /// 框架启动完成后加载玩法存档、准备关卡场景并打开主界面
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="args">应用启动初始化完成事件</param>
        protected override async ETTask Run(Scene root, AppStartInitFinish args)
        {
            await GameplaySaveHelper.LoadGameplaySaveAsync(root.GetComponent<SaveManagerComponent>());
            await PuzzleLevelRuntimeHelper.PreparePuzzleSceneAsync(root);
            await root.YIUIRoot().OpenPanelAsync<GameMainPanelPanelComponent>();
        }
    }
}
