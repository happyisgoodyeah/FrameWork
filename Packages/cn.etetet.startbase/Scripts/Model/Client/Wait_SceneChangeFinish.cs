namespace ET.Client
{
    /// <summary>
    /// 子场景切换完成的等待信号，供需要阻塞到切换结束的流程使用
    /// </summary>
    public struct Wait_SceneChangeFinish: IWaitType
    {
        public int Error { get; set; }
    }
}
