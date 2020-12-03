using Backtrace.Unity.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        public Annotations()
        {
        }
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
            var environmentVariables = new Dictionary<string, string>();
            foreach (DictionaryEntry variable in Environment.GetEnvironmentVariables())
            {
                if (variable.Key == null)
                {
                    continue;
                }
                var value = variable.Value == null ? "NULL" : variable.Value.ToString();
                environmentVariables.Add(variable.Key.ToString(), value);
                environmentVariables.Add(variable.Key.ToString(), value);
            }
            return environmentVariables;
        }
        public BacktraceJObject ToJson()
        {
            var annotations = new BacktraceJObject();
            var envVariables = new BacktraceJObject();
            if (EnvironmentVariables != null)
            {
                foreach (var envVariable in EnvironmentVariables)
                {
                    envVariables[envVariable.Key] = envVariable.Value;
                }
                annotations["Environment Variables"] = envVariables;
            }
            if (Exception != null)
            {
                annotations["Exception properties"] = new BacktraceJObject()
                {
                    ["message"] = Exception.Message,
                    ["stackTrace"] = Exception.StackTrace,
                    ["type"] = Exception.GetType().FullName,
                    ["source"] = Exception.Source
                };
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
                annotations["Game objects"] = gameObjects;
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
            jGameObject["children"] = innerObjects;
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
            result["children"] = innerObjects;
            return result;
        }
        private BacktraceJObject GetJObject(GameObject gameObject, string parentName = "")
        {
            var o = new BacktraceJObject();
            o["name"] = gameObject.name;
            o["isStatic"] = gameObject.isStatic;
            o["layer"] = gameObject.layer;
            o["transform.position"] = gameObject.transform.position.ToString() ?? "";
            o["transform.rotation"] = gameObject.transform.rotation.ToString() ?? "";
            o["tag"] = gameObject.tag;
            o["activeInHierarchy"] = gameObject.activeInHierarchy;
            o["activeSelf"] = gameObject.activeSelf;
            o["hideFlags"] = (int)gameObject.hideFlags;
            o["instanceId"] = gameObject.GetInstanceID();
            o["parnetName"] = string.IsNullOrEmpty(parentName) ? "root object" : parentName;
            return o;
        }
        private BacktraceJObject GetJObject(Component gameObject, string parentName = "")
        {
            var o = new BacktraceJObject();
            o["name"] = gameObject.name;
            o["transform.position"] = gameObject.transform.position.ToString() ?? "";
            o["transform.rotation"] = gameObject.transform.rotation.ToString() ?? "";
            o["tag"] = gameObject.tag;
            o["hideFlags"] = (int)gameObject.hideFlags;
            o["instanceId"] = gameObject.GetInstanceID();
            o["parnetName"] = string.IsNullOrEmpty(parentName) ? "root object" : parentName;
            return o;
        }
    }
}