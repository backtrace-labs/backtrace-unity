﻿using Backtrace.Newtonsoft;
using Backtrace.Newtonsoft.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Backtrace.Unity.Model.JsonData
{
    /// <summary>
    /// Get report annotations - environment variables
    /// </summary>
    public class Annotations
    {
        /// <summary>
        /// Set maximum number of game objects in Backtrace report
        /// </summary>
        public static int GameObjectDepth { get; set; }

        private const string ENVIRONMENT_VARIABLE_KEY = "Environment Variables";
        private JToken _serializedAnnotations;

        private readonly Dictionary<string, string> _environmentVariables = new Dictionary<string, string>();
        /// <summary>
        /// Get system environment variables
        /// </summary>
        [JsonProperty(PropertyName = ENVIRONMENT_VARIABLE_KEY)]
        public Dictionary<string, string> EnvironmentVariables
        {
            get
            {
                if (_serializedAnnotations != null && _environmentVariables.Count == 0)
                {
                    foreach (BacktraceJProperty keys in _serializedAnnotations[ENVIRONMENT_VARIABLE_KEY])
                    {
                        _environmentVariables.Add(keys.Name, keys.Value.Value<string>());
                    }
                    return _environmentVariables;
                }
                else
                {
                    return _environmentVariables;
                }
            }
        }


        /// <summary>
        /// Create new instance of Annotations class
        /// </summary>
        public Annotations()
        {
            var environment = new EnvironmentVariables();
            _environmentVariables = environment.Variables;
        }

        public void FromJson(JToken jtoken)
        {
            _serializedAnnotations = jtoken;
        }

        public BacktraceJObject ToJson()
        {
            if (_serializedAnnotations != null)
            {
                return _serializedAnnotations as BacktraceJObject;
            }
            var annotations = new BacktraceJObject();
            var envVariables = new BacktraceJObject();

            foreach (var envVariable in EnvironmentVariables)
            {
                envVariables[envVariable.Key] = envVariable.Value ?? string.Empty;
            }
            annotations[ENVIRONMENT_VARIABLE_KEY] = envVariables;

            if (GameObjectDepth > -1)
            {
                var activeScene = SceneManager.GetActiveScene();

                var gameObjects = new JArray();

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

        public static Annotations Deserialize(JToken token)
        {
            var annotations = new Annotations();
            annotations.FromJson(token);
            return annotations;
        }

        private BacktraceJObject ConvertGameObject(GameObject gameObject, int depth = 0)
        {
            if (gameObject == null)
            {
                return new BacktraceJObject();
            }
            var jGameObject = GetJObject(gameObject);
            var innerObjects = new JArray();

            foreach (var childObject in gameObject.transform)
            {
                var transformChildObject = childObject as Component;
                if (transformChildObject == null)
                {
                    continue;
                }
                innerObjects.Add(ConvertGameObject(transformChildObject, gameObject.name, depth + 1));
            }
            jGameObject["childrens"] = innerObjects;
            return jGameObject;
        }

        private BacktraceJObject ConvertGameObject(Component gameObject, string parentName, int depth)
        {
            if (GameObjectDepth > 0 && depth > GameObjectDepth)
            {
                return new BacktraceJObject();
            }
            var result = GetJObject(gameObject, parentName);
            if (GameObjectDepth > 0 && depth + 1 >= GameObjectDepth)
            {
                return result;
            }
            var innerObjects = new JArray();


            foreach (var childObject in gameObject.transform)
            {
                var transformChildObject = childObject as Component;
                if (transformChildObject == null)
                {
                    continue;
                }
                innerObjects.Add(ConvertGameObject(transformChildObject, gameObject.name, depth + 1));
            }
            result["childrens"] = innerObjects;
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
