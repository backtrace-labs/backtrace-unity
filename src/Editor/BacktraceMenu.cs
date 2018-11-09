using System.IO;
using UnityEditor;
using UnityEngine;
using Backtrace.Unity.Model;

namespace Backtrace.Unity.Port.Editor
{
    public class BacktraceMenu : MonoBehaviour
    {
        public const string DEFAULT_CLIENT_CONFIGURATION_NAME = "Backtrace Configuration.asset";

        [MenuItem("Assets/Backtrace/Configuration", false, 1)]
        public static void CreateClientConfigurationFile()
        {
            CreateAsset<BacktraceConfiguration>(DEFAULT_CLIENT_CONFIGURATION_NAME);
        }

        private static void CreateAsset<T>(string fileName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            string currentProjectPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(currentProjectPath))
            {
                currentProjectPath = "Assets";
            }
            else if (File.Exists(currentProjectPath))
            {
                currentProjectPath = Path.GetDirectoryName(currentProjectPath);
            }
            AssetDatabase.CreateAsset(asset, "Assets/" + fileName);
            AssetDatabase.SaveAssets();

            var destinationPath = Path.Combine(currentProjectPath, fileName);
            AssetDatabase.MoveAsset("Assets/" + fileName, destinationPath);
            Selection.activeObject = asset;
        }
    }
}