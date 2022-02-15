using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Backtrace.Unity.Model.Attributes
{
    internal sealed class RuntimeAttributeProvider : IScopeAttributeProvider
    {
        public void GetAttributes(IDictionary<string, string> attributes)
        {
            if (attributes == null)
            {
                return;
            }

            attributes["backtrace.version"] = BacktraceClient.VERSION;
            attributes["api.compatibility"] = GetApiCompatibility();
            attributes["scripting.backend"] = GetScriptingBackend();
            attributes["application"] = Application.productName;
            attributes["application.version"] = Application.version;
            attributes["application.url"] = Application.absoluteURL;
            attributes["application.company.name"] = Application.companyName;
            attributes["application.data_path"] = Application.dataPath;
            attributes["application.id"] = Application.identifier;
            attributes["application.installer.name"] = Application.installerName;
            attributes["application.editor"] = Application.isEditor.ToString(CultureInfo.InvariantCulture);            
            attributes["application.mobile"] = Application.isMobilePlatform.ToString(CultureInfo.InvariantCulture);            
            attributes["application.background"] = Application.runInBackground.ToString(CultureInfo.InvariantCulture);
            attributes["application.sandboxType"] = Application.sandboxType.ToString();
            attributes["application.system.language"] = Application.systemLanguage.ToString();
            attributes["application.unity.version"] = Application.unityVersion;
            attributes["application.debug"] = Debug.isDebugBuild.ToString(CultureInfo.InvariantCulture);
#if !UNITY_SWITCH
            attributes["application.temporary_cache"] = Application.temporaryCachePath;
#endif
        }


        private string GetScriptingBackend()
        {
#if ENABLE_IL2CPP
            return "IL2CPP";
#else
            return "Mono";
#endif
        }

        private string GetApiCompatibility()
        {

#if NET_STANDARD_2_0
            return ".NET Standard 2.0";
#elif NET_4_6
            return ".NET Framework 4.5";
#else 
            return ".NET Framework 3.5 equivalent";
#endif
        }
    }
}
