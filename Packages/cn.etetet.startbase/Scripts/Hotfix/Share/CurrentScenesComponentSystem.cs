namespace ET
{
    public static partial class CurrentScenesComponentSystem
    {
        /// <summary>
        /// 获取当前子场景，未创建时返回 null
        /// </summary>
        /// <param name="root">根场景</param>
        public static Scene CurrentScene(this Scene root)
        {
            return root.GetComponent<CurrentScenesComponent>()?.Scene;
        }
    }
}
