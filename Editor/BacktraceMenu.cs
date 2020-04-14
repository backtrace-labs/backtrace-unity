using System;
using Backtrace.Unity.Model;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Backtrace.Unity.Editor
{
    public class BacktraceMenu
    {
        public const string DEFAULT_CONFIGURATION_NAME = "Backtrace Configuration";
        public const string DEFAULT_EXTENSION_NAME = ".asset";
        public const string DEFAULT_CLIENT_CONFIGURATION_NAME = DEFAULT_CONFIGURATION_NAME + DEFAULT_EXTENSION_NAME;

        [MenuItem("Assets/Create/Backtrace/Configuration")]
        public static void CreateClientConfigurationFile()
        {
            CreateAsset<BacktraceConfiguration>(DEFAULT_CLIENT_CONFIGURATION_NAME);
        }

        private static void CreateAsset<T>(string fileName) where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            var currentProjectPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(currentProjectPath))
            {
                currentProjectPath = "Assets";
            }
            else if (File.Exists(currentProjectPath))
            {
                currentProjectPath = Path.GetDirectoryName(currentProjectPath);
            }
            var destinationPath = Path.Combine(currentProjectPath, fileName);
            if (File.Exists(destinationPath))
            {
                var files = Directory.GetFiles(currentProjectPath);
                var lastFileIndex = files
                    .Where(n =>
                        Path.GetFileNameWithoutExtension(n).StartsWith(DEFAULT_CONFIGURATION_NAME) &&
                        Path.GetExtension(n) == DEFAULT_EXTENSION_NAME)
                        .Select(n =>
                        {
                            int startIndex = n.IndexOf('(') + 1;
                            int endIndex = n.IndexOf(')');
                            int result;
                            if (startIndex != 0 && endIndex != -1 && int.TryParse(n.Substring(startIndex, endIndex - startIndex), out result))
                            {
                                return result;
                            }
                            return 0;
                        })
                        .DefaultIfEmpty().Max();

                lastFileIndex++;
                destinationPath = Path.Combine(currentProjectPath,
                    string.Format("{0}({1}){2}", DEFAULT_CONFIGURATION_NAME, lastFileIndex, DEFAULT_EXTENSION_NAME));
            }
            Debug.Log(string.Format("Generating new Backtrace configuration file available in path: {0}",
                destinationPath));
            AssetDatabase.CreateAsset(asset, destinationPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }
    }
}