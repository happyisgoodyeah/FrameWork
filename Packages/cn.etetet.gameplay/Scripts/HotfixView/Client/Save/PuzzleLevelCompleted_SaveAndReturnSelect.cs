namespace ET.Client
{
    /// <summary>
    /// 关卡完成后保存进度并回到关卡选择
    /// </summary>
    [Event(SceneType.Current)]
    public class PuzzleLevelCompleted_SaveAndReturnSelect : AEvent<Scene, PuzzleLevelCompleted>
    {
        /// <summary>
        /// 保存通关状态并等待落位反馈，取消或切场景后不再跳转界面
        /// </summary>
        /// <param name="scene">当前关卡逻辑场景</param>
        /// <param name="args">关卡完成事件</param>
        protected override async ETTask Run(Scene scene, PuzzleLevelCompleted args)
        {
            EntityRef<Grid> completedGrid = args.Grid;
            EntityRef<Scene> sceneRef = scene;
            Scene root = scene.Root();
            bool hasLandingFeedback = args.Grid.GetComponent<GridView>().GameObject.GetComponent<PuzzleGame.PlacementFeedback>() != null;
            long completedAt = TimeInfo.Instance.ClientFrameTime();
            SaveResult saveResult = await GameplaySaveHelper.CompleteLevelAsync(root.GetComponent<SaveManagerComponent>(), args.GridConfigId);
            if (saveResult != SaveResult.Success)
            {
                Log.Error($"保存关卡完成与图鉴解锁失败: {saveResult}");
            }

            scene = sceneRef;
            Grid grid = completedGrid;
            if (scene == null || grid == null)
            {
                return;
            }

            long remaining = PuzzleGame.PlacementFeedback.LandingDurationMilliseconds - (TimeInfo.Instance.ClientFrameTime() - completedAt);
            if (hasLandingFeedback && remaining > 0)
            {
                await scene.Root().GetComponent<TimerComponent>().WaitAsync(remaining);
            }

            scene = sceneRef;
            grid = completedGrid;
            if (scene == null || grid == null)
            {
                return;
            }

            grid.Dispose();
            await scene.YIUIRoot().OpenPanelAsync<GameMainPanelPanelComponent, EGameMainPanelPanelViewEnum>(EGameMainPanelPanelViewEnum.SelectLevelView);
        }
    }
}
