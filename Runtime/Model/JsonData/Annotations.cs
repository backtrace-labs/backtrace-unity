using Backtrace.Unity.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// Get report annotations - environment variables
    /// </summary>
    public class Annotations
    {
        public static Dictionary<string, string> EnvironmentVariablesCache { get; set; } = SetEnvironmentVariables();
        public Dictionary<string, string> EnvironmentVariables { get; set; } = EnvironmentVariablesCache;
        /// <summary>
        /// Set maximum number of game objects in Backtrace report
        /// </summary>
        private readonly int _gameObjectDepth;
        /// <summary>
        /// Exception object
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Create new instance of Annotations class
        /// </summary>
        /// <param name="exception">Current exception</param>
        /// <param name="gameObjectDepth">Game object depth</param>
        public Annotations(Exception exception, int gameObjectDepth)
        {
            _gameObjectDepth = gameObjectDepth;
            Exception = exception;
        }
        
        private static Dictionary<string, string> SetEnvironmentVariables()
        {
            var result = new Dictionary<string, string>();
            var environmentVariables = Environment.GetEnvironmentVariables();
            if (environmentVariables == null)
            {
                return result;
            }


            foreach (DictionaryEntry variable in environmentVariables)
            {
                var key = variable.Key as string;
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                var rawValue = variable.Value as string;
                result.Add(key, string.IsNullOrEmpty(rawValue) ? "NULL" : rawValue);
            }
            return result;
        }
        public BacktraceJObject ToJson()
        {
            var annotations = new BacktraceJObject();
            annotations.Add("Environment Variables", new BacktraceJObject(EnvironmentVariables));

            if (Exception != null)
            {
                annotations.Add("Exception properties", new BacktraceJObject(new Dictionary<string, string>()
                {
                    {"message",  Exception.Message },
                    {"stackTrace",Exception.StackTrace},
                    {"type", Exception.GetType().FullName },
                    { "source",Exception.Source },
                }));
            }
            if (_gameObjectDepth > -1)
            {
                var activeScene = SceneManager.GetActiveScene();
                var gameObjects = new List<BacktraceJObject>();
                var rootObjects = new List<GameObject>();
                activeScene.GetRootGameObjects(rootObjects);
                foreach (var gameObject in rootObjects)
                {
                    gameObjects.Add(ConvertGameObject(gameObject));
                }
                annotations.Add("Game objects", gameObjects);
            }
            return annotations;
        }
        private BacktraceJObject ConvertGameObject(GameObject gameObject, int depth = 0)
        {
            if (gameObject == null)
            {
                return new BacktraceJObject();
            }
            var jGameObject = GetJObject(gameObject);
            var innerObjects = new List<BacktraceJObject>();
            foreach (var childObject in gameObject.transform)
            {
                var transformChildObject = childObject as Component;
                if (transformChildObject == null)
                {
                    continue;
                }
                innerObjects.Add(ConvertGameObject(transformChildObject, gameObject.name, depth + 1));
            }
            jGameObject.Add("children", innerObjects);
            return jGameObject;
        }
        private BacktraceJObject ConvertGameObject(Component gameObject, string parentName, int depth)
        {
            if (_gameObjectDepth > 0 && depth > _gameObjectDepth)
            {
                return new BacktraceJObject();
            }
            var result = GetJObject(gameObject, parentName);
            if (_gameObjectDepth > 0 && depth + 1 >= _gameObjectDepth)
            {
                return result;
            }
            var innerObjects = new List<BacktraceJObject>();
            foreach (var childObject in gameObject.transform)
            {
                var transformChildObject = childObject as Component;
                if (transformChildObject == null)
                {
                    continue;
                }
                innerObjects.Add(ConvertGameObject(transformChildObject, gameObject.name, depth + 1));
            }
            result.Add("children", innerObjects);
            return result;
        }
        private BacktraceJObject GetJObject(GameObject gameObject, string parentName = "")
        {
            return new BacktraceJObject(new Dictionary<string, string>()
            {
                { "name",  gameObject.name},
                {"isStatic", gameObject.isStatic.ToString(CultureInfo.InvariantCulture).ToLower() },
                {"layer",  gameObject.layer.ToString(CultureInfo.InvariantCulture) },
                {"transform.position", gameObject.transform.position.ToString()},
                {"transform.rotation", gameObject.transform.rotation.ToString()},
                {"tag",gameObject.tag},
                {"activeInHierarchy", gameObject.activeInHierarchy.ToString(CultureInfo.InvariantCulture).ToLower()},
                {"activeSelf",  gameObject.activeSelf.ToString(CultureInfo.InvariantCulture).ToLower() },
                {"instanceId", gameObject.GetInstanceID().ToString(CultureInfo.InvariantCulture) },
                { "parnetName", string.IsNullOrEmpty(parentName) ? "root object" : parentName }
            });
        }
        private BacktraceJObject GetJObject(Component gameObject, string parentName = "")
        {
            return new BacktraceJObject(new Dictionary<string, string>()
            {
                { "name",  gameObject.name},
                {"transform.position", gameObject.transform.position.ToString()},
                {"transform.rotation", gameObject.transform.rotation.ToString()},
                {"tag",gameObject.tag},
                {"instanceId", gameObject.GetInstanceID().ToString(CultureInfo.InvariantCulture) },
                { "parnetName", string.IsNullOrEmpty(parentName) ? "root object" : parentName }
            });
        }
    }
}