namespace ET
{
    public static partial class SceneType
    {
        //客户端根 Scene，需要与 GlobalConfig.SceneName 的字符串一致
        public const int Game = PackageType.StartBase * 1000 + 20;

        //子场景类型，关卡或地图这一层，随切换整体销毁重建
        public const int Current = PackageType.StartBase * 1000 + 21;
    }
}
