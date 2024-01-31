using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Backtrace.Unity;

public class BacktraceGameController : MonoBehaviour
{

    private const string LastAction = "action.last";
    private BacktraceClient _client;

    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 10;
        _client = BacktraceClient.Instance;
    }

    public void Crash()
    {
        _client[LastAction] = "Crash";
#if UNITY_EDITOR
        Debug.LogError("Crashing the game will crash your Editor. Preventing the crash in the editor mode.");
        return;
#endif

        Debug.LogWarning("Crashing the game");
        UnityEngine.Diagnostics.Utils.ForceCrash(UnityEngine.Diagnostics.ForcedCrashCategory.Abort);
    }


    public void HandledException()
    {
        try
        {
            Debug.LogError("Handled exception action");
            ReadFile();
        }
        catch (Exception e)
        {
            _client.Send(e);
        }
    }

    public void UnhandledException()
    {
        Debug.LogWarning("Unhandled exception action");
        ReadFile();
    }

    public void Oom()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Debug.LogWarning("Starting OOM");
        StartCoroutine(StartOom());
#else
        Debug.LogError("Action not supported.");
#endif
    }

    private bool _doOom = true;
    private List<Texture2D> _textures = new List<Texture2D>();
    private IEnumerator StartOom()
    {
        while (_doOom)
        {
            var texture = new Texture2D(512, 512, TextureFormat.ARGB32, true);
            texture.Apply();
            _textures.Add(texture);
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    private void FreezeMainThread()
    {
        const int anrTime = 11000;
        System.Threading.Thread.Sleep(anrTime);
    }

    public void StartAnr()
    {
        Debug.LogWarning("Starting ANR in the managed Unity thread.");
        FreezeMainThread();
    }

    /// <summary>
    /// Use this function to extend stack trace information
    /// </summary>
    private void ReadFile()
    {
        ReadNotExistingFileFromTheStorage();
    }

    private void ReadNotExistingFileFromTheStorage()
    {
        throw new Exception("ReadNotExistingFileFromTheStorage");
    }

}
