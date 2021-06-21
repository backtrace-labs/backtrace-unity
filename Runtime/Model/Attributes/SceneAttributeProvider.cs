using System.Collections.Generic;
using System.Globalization;
using UnityEngine.SceneManagement;

namespace Backtrace.Unity.Model.Attributes
{
    internal sealed class SceneAttributeProvider : IDynamicAttributeProvider
    {
        public void GetAttributes(IDictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                return;
            }
            //The number of Scenes which have been added to the Build Settings. The Editor will contain Scenes that were opened before entering playmode.
            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                attributes["scene.count.build"] = SceneManager.sceneCountInBuildSettings.ToString(CultureInfo.InvariantCulture);
            }
            attributes["scene.count"] = SceneManager.sceneCount.ToString(CultureInfo.InvariantCulture);
            var activeScene = SceneManager.GetActiveScene();
            attributes["scene.active"] = activeScene.name;
            attributes["scene.buildIndex"] = activeScene.buildIndex.ToString(CultureInfo.InvariantCulture);
#if UNITY_2018_4_OR_NEWER
            attributes["scene.handle"] = activeScene.handle.ToString(CultureInfo.InvariantCulture);
#endif
            attributes["scene.isDirty"] = activeScene.isDirty.ToString(CultureInfo.InvariantCulture);
            attributes["scene.isLoaded"] = activeScene.isLoaded.ToString(CultureInfo.InvariantCulture);
            attributes["scene.name"] = activeScene.name;
            attributes["scene.path"] = activeScene.path;
        }
    }
}
