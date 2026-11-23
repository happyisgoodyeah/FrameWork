namespace ET.Client
{
    /// <summary>
    /// 关卡完成后保存进度，成功后回到关卡选择
    /// </summary>
    [Event(SceneType.Current)]
    public class PuzzleLevelCompleted_SaveAndReturnSelect : AEvent<Scene, PuzzleLevelCompleted>
    {
        /// <summary>
        /// 保存失败时记录错误并保留当前关卡，成功后等待落位反馈再返回选关
        /// </summary>
        /// <param name="scene">当前关卡逻辑场景</param>
        /// <param name="args">关卡完成事件</param>
        protected override async ETTask Run(Scene scene, PuzzleLevelCompleted args)
        {
            EntityRef<Grid> completedGrid = args.Grid;
            EntityRef<Scene> sceneRef = scene;
            Scene root = scene.Root();
            EntityRef<PuzzleView> landingViewRef = args.Grid.GetComponent<LevelView>().LastPlacementView;
            SaveResult saveResult;
            try
            {
                saveResult = await GameplaySaveHelper.CompleteLevelAsync(root.GetComponent<SaveManagerComponent>(), args.GridConfigId);
            }
            catch (System.Exception exception)
            {
                Log.Error($"保存关卡完成与图鉴解锁异常: gridConfigId={args.GridConfigId} {exception}");
                return;
            }

            if (saveResult != SaveResult.Success)
            {
                Log.Error($"保存关卡完成与图鉴解锁失败: gridConfigId={args.GridConfigId} result={saveResult}");
                return;
            }

            scene = sceneRef;
            Grid grid = completedGrid;
            if (scene == null || grid == null)
            {
                return;
            }

            PuzzleView landingView = landingViewRef;
            if (landingView != null)
            {
                await landingView.WaitForPlacementEffectAsync();
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
