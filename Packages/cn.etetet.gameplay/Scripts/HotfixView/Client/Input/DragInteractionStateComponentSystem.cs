namespace ET.Client
{
    /// <summary>
    /// DragInteractionStateComponent 的状态维护与快捷访问逻辑
    /// </summary>
    [EntitySystemOf(typeof(DragInteractionStateComponent))]
    public static partial class DragInteractionStateComponentSystem
    {
        /// <summary>
        /// 初始化当前场景的拖拽交互状态
        /// </summary>
        [EntitySystem]
        private static void Awake(this DragInteractionStateComponent self)
        {
            self.DraggingTarget = default;
        }

        /// <summary>
        /// 清理当前场景的拖拽交互状态
        /// </summary>
        [EntitySystem]
        private static void Destroy(this DragInteractionStateComponent self)
        {
            self.DraggingTarget = default;
        }

        /// <summary>
        /// 获取或挂载当前场景的拖拽交互状态组件
        /// </summary>
        /// <param name="self">当前场景</param>
        /// <returns>当前场景对应的拖拽交互状态组件</returns>
        public static DragInteractionStateComponent GetOrAddDragInteractionState(this Scene self)
        {
            DragInteractionStateComponent interactionStateComponent = self.GetComponent<DragInteractionStateComponent>();
            if (interactionStateComponent == null)
            {
                interactionStateComponent = self.AddComponent<DragInteractionStateComponent>();
            }

            return interactionStateComponent;
        }

        /// <summary>
        /// 获取当前拖拽手势锁定的业务目标
        /// </summary>
        /// <param name="self">当前场景的拖拽交互状态组件</param>
        /// <returns>当前拖拽目标；若不存在则返回空</returns>
        public static Entity GetDraggingTarget(this DragInteractionStateComponent self)
        {
            return self.DraggingTarget;
        }

        /// <summary>
        /// 记录当前拖拽手势锁定的业务目标
        /// </summary>
        /// <param name="self">当前场景的拖拽交互状态组件</param>
        /// <param name="target">要记录为正在拖拽的业务目标</param>
        public static void SetDraggingTarget(this DragInteractionStateComponent self, Entity target)
        {
            self.DraggingTarget = target;
        }

        /// <summary>
        /// 清空当前拖拽手势锁定的业务目标
        /// </summary>
        /// <param name="self">当前场景的拖拽交互状态组件</param>
        public static void ClearDraggingTarget(this DragInteractionStateComponent self)
        {
            self.DraggingTarget = default;
        }
    }

    /// <summary>
    /// 取消手势时回退拼图拖拽并清理拎起表现
    /// </summary>
    [Event(SceneType.Current)]
    public class PointerCancel_ResetPuzzleInteraction : AEvent<Scene, PointerCancel>
    {
        /// <summary>
        /// 取消拖拽时清除占格并回初始托盘和默认朝向，再释放交互目标
        /// </summary>
        /// <param name="scene">当前关卡场景</param>
        /// <param name="args">取消输入上下文</param>
        protected override async ETTask Run(Scene scene, PointerCancel args)
        {
            DragInteractionStateComponent interaction = scene.GetComponent<DragInteractionStateComponent>();
            Puzzle draggingPuzzle = interaction?.GetDraggingTarget() as Puzzle;
            draggingPuzzle?.GetComponent<PuzzleDragComponent>()?.RestoreInitialPosition();
            interaction?.ClearDraggingTarget();
            PuzzleInputResolver.ResolvePress(args.Context, false).PuzzleView?.SetPickedUp(false);
            await ETTask.CompletedTask;
        }
    }
}
