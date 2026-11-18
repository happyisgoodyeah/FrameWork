using System;
using UnityEngine.SceneManagement;

namespace ET.Client
{
    [Event(SceneType.Game)]
    public class SceneChangeStart_LoadPuzzleScene: AEvent<Scene, SceneChangeStart>
    {
        /// <summary>
        /// 场景切换开始时加载对应的玩法 Unity 场景资源
        /// </summary>
        /// <param name="root">客户端根场景</param>
        /// <param name="args">场景切换开始事件</param>
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            try
            {
                Scene currentScene = root.CurrentScene();

                ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();

                await resourcesLoaderComponent.LoadSceneAsync(
                    $"Packages/cn.etetet.gameplay/Resources/Scene/{currentScene.Name}.unity", LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
