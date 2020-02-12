using Backtrace.Newtonsoft;
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

        private const string ENVIRONMENT_VARIABLE_KEY = "Environment Variables";
        private JToken _serializedAnnotations;

        private Dictionary<string, string> _environmentVariables = new Dictionary<string, string>();
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
        /// Get built-in complex attributes
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> ComplexAttributes = new Dictionary<string, object>();

        public Annotations()
        {

        }
        /// <summary>
        /// Create new instance of Annotations class
        /// </summary>
        /// <param name="complexAttributes">Built-in complex attributes</param>
        public Annotations(Dictionary<string, object> complexAttributes)
        {
            var environment = new EnvironmentVariables();
            ComplexAttributes = complexAttributes;
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
                envVariables[envVariable.Key] = envVariable.Value?.ToString() ?? string.Empty;
            }
            annotations[ENVIRONMENT_VARIABLE_KEY] = envVariables;

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene != null)
            {
                var gameObjects = new JArray();

                var rootObjects = new List<GameObject>();
                activeScene.GetRootGameObjects(rootObjects);
                foreach (var objects in rootObjects)
                {
                    gameObjects.Add(ConvertGameObject(objects));
                    if (gameObjects.Count > 30)
                    {
                        break;
                    }
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

        private BacktraceJObject ConvertGameObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return new BacktraceJObject();
            }
            var jGameObject = GetJObject(gameObject);
            var innerObjects = new JArray();

            foreach (var childObject in gameObject.transform)
            {
                var transformChildObject = childObject as RectTransform;
                if(transformChildObject == null)
                {
                    continue;
                }
                innerObjects.Add(ConvertGameObject(transformChildObject, gameObject.name));
            }
            jGameObject["childrens"] = innerObjects;
            return jGameObject;
        }

        private BacktraceJObject ConvertGameObject(RectTransform gameObject, string parentName)
        {
            var result = GetJObject(gameObject, parentName);
            var innerObjects = new JArray();

            foreach (var childObject in gameObject.transform)
            {
                var transformChildObject = childObject as RectTransform;
                if (transformChildObject == null)
                {
                    continue;
                }
                innerObjects.Add(ConvertGameObject(transformChildObject, gameObject.name));
            }
            result["childrens"] = innerObjects;
            return result;
        }

        private BacktraceJObject GetJObject(GameObject gameObject, string parentName = "")
        {
            return new BacktraceJObject()
            {
                ["name"] = gameObject.name,
                ["isStatic"] = gameObject.isStatic,
                ["layer"] = gameObject.layer,
                ["tag"] = gameObject.tag,
                ["transform.position"] = gameObject.transform?.position.ToString() ?? "",
                ["transform.rotation"] = gameObject.transform?.rotation.ToString() ?? "",
                ["tag"] = gameObject.tag,
                ["activeInHierarchy"] = gameObject.activeInHierarchy,
                ["activeSelf"] = gameObject.activeSelf,
                ["hideFlags"] = (int)gameObject.hideFlags,
                ["instanceId"] = gameObject.GetInstanceID(),
                ["parnetName"] = string.IsNullOrEmpty(parentName) ? "root object" : parentName
            };
        }

        private BacktraceJObject GetJObject(RectTransform gameObject, string parentName = "")
        {
            return new BacktraceJObject()
            {
                ["name"] = gameObject.name,
                ["tag"] = gameObject.tag,
                ["transform.position"] = gameObject.transform?.position.ToString() ?? "",
                ["transform.rotation"] = gameObject.transform?.rotation.ToString() ?? "",
                ["tag"] = gameObject.tag,
                ["hideFlags"] = (int)gameObject.hideFlags,
                ["instanceId"] = gameObject.GetInstanceID(),
                ["parnetName"] = string.IsNullOrEmpty(parentName) ? "root object" : parentName
            };
        }



    }
}
