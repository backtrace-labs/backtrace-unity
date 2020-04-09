using UnityEngine;
using System.Collections;
using Backtrace.Unity;
using Backtrace.Unity.Model;
using NUnit.Framework;

public class BacktraceBaseTest : MonoBehaviour
{
    protected GameObject GameObject;
    protected BacktraceClient BacktraceClient;
    protected void BeforeSetup()
    {
        Debug.unityLogger.logEnabled = false;
        GameObject = new GameObject();
        GameObject.SetActive(false);
        BacktraceClient = GameObject.AddComponent<BacktraceClient>();
    }

    protected void AfterSetup(bool refresh = true)
    {
        if (refresh)
        {
            BacktraceClient.Refresh();
        }
        GameObject.SetActive(true);
    }

    protected BacktraceConfiguration GetBasicConfiguration()
    {
        var configuration = ScriptableObject.CreateInstance<BacktraceConfiguration>();
        configuration.ServerUrl = "https://submit.backtrace.io/test/token/json";
        configuration.DestroyOnLoad = true;
        return configuration;
    }

    [TearDown]
    public void Cleanup()
    {
        DestroyImmediate(GameObject);
    }
}
