namespace ET.Client
{
    /// <summary>
    /// 项目资源定位地址转换辅助方法
    /// </summary>
    public static class ResourceLocationHelper
    {
        private const string GameplayResourceRoot = "Packages/cn.etetet.gameplay/Resources/";
        private const string GameplayPrefabRoot = GameplayResourceRoot + "Prefab/";
        private const string PrefabExtension = ".prefab";

        /// <summary>
        /// 将相对预制体路径转换为 YooAssets 地址并保留完整资源路径
        /// </summary>
        /// <param name="resourcePath">相对于预制体目录或完整的游戏资源路径</param>
        /// <returns>带预制体扩展名的资源地址</returns>
        public static string ToAssetLocation(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                throw new System.ArgumentException("resource path is empty", nameof(resourcePath));
            }

            string normalizedPath = resourcePath.Replace('\\', '/').TrimStart('/');
            if (!normalizedPath.StartsWith(GameplayResourceRoot, System.StringComparison.Ordinal))
            {
                normalizedPath = GameplayPrefabRoot + normalizedPath;
            }

            return normalizedPath.EndsWith(PrefabExtension, System.StringComparison.Ordinal)
                    ? normalizedPath
                    : normalizedPath + PrefabExtension;
        }
    }
}
