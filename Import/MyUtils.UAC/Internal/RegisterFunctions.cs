using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MyUtils.UAC.Internal
{
    // ONLY PUBLIC, NOT STATIC!!
    public class RegisterFunctions
    {
        private const string _appId = "9AD9AFB1-F291-4FFB-9EA9-7A8956645853";

        [ComRegisterFunction]
        static void CustomRegister(Type t)
        {
            RegisterForElevation(Assembly.GetExecutingAssembly().Location, ElevatedComApi.ComClassIdString,_appId, 100);
        }

        [ComUnregisterFunction]
        static void CustomUnregister(Type t)
        {
            UnRegisterFromElevation(Assembly.GetExecutingAssembly().Location, ElevatedComApi.ComClassIdString, _appId);
        }

        public static bool IsUacEnabledOs
        {
            get
            {
                return Environment.OSVersion.Version.Major >= 6;
            }
        }

        public static void RegisterForElevation(string assemblyLocation, string classToElevate, string appId, int localizedStringId)
        {
            if (!IsUacEnabledOs)
            {
                return;
            }

            // [HKEY_CLASSES_ROOT\CLSID\{71E050A7-AF7F-42dd-BE00-BF955DDD13D4}]
            // "AppID"="{75AB90B0-8B9C-45c9-AC55-C53A9D718E1A}"
            // "LocalizedString"="@E:\\Daten\\Firma\\Konferenzen und Talks\\VSone 2007\\UAC\\Samples\\ConsumeMyElevatedCOM\\ManagedElevator\\bin\\Debug\\ManagedElevator.dll,-100"
            RegistryKey classKey = Registry.ClassesRoot.OpenSubKey(@"CLSID\{" + classToElevate + "}", true);
            classKey.SetValue("AppId",
                "{" + appId + "}",
                RegistryValueKind.String);

            classKey.SetValue("LocalizedString",
                "@" + assemblyLocation + ",-" + localizedStringId,
                RegistryValueKind.String);

            // [HKEY_CLASSES_ROOT\CLSID\{71E050A7-AF7F-42dd-BE00-BF955DDD13D4}\Elevation]
            // "Enabled"=dword:00000001
            RegistryKey elevationKey = classKey.CreateSubKey("Elevation");
            elevationKey.SetValue("Enabled", 1, RegistryValueKind.DWord);
            elevationKey.Close();

            classKey.Close();

            // [HKEY_CLASSES_ROOT\AppID\{75AB90B0-8B9C-45c9-AC55-C53A9D718E1A}]
            // @="ManagedElevator"
            // "DllSurrogate"=""
            RegistryKey hkcrappId = Registry.ClassesRoot.OpenSubKey("AppID", true);
            RegistryKey appIdKey = hkcrappId.CreateSubKey("{" + appId + "}");
            appIdKey.SetValue(null, Path.GetFileNameWithoutExtension(assemblyLocation));
            appIdKey.SetValue("DllSurrogate", "", RegistryValueKind.String);
            appIdKey.Close();

            // [HKEY_CLASSES_ROOT\AppID\ManagedElevator.dll]
            // "AppID"="{75AB90B0-8B9C-45c9-AC55-C53A9D718E1A}"
            RegistryKey asmKey = hkcrappId.CreateSubKey(Path.GetFileName(assemblyLocation));
            asmKey.SetValue("AppID", "{" + appId + "}", RegistryValueKind.String);
            asmKey.Close();

            hkcrappId.Close();
        }

        public static void UnRegisterFromElevation(string assemblyLocation, string classToElevate, string appId)
        {
            try
            {
                if (!IsUacEnabledOs)
                {
                    return;
                }


                RegistryKey classKey = Registry.ClassesRoot.OpenSubKey("CLSID", true);
                classKey.DeleteSubKeyTree("{" + classToElevate + "}");

                RegistryKey hkcrappId = Registry.ClassesRoot.OpenSubKey("AppID", true);
                hkcrappId.DeleteSubKeyTree("{" + appId + "}");
                hkcrappId.DeleteSubKeyTree(Path.GetFileName(assemblyLocation));
                hkcrappId.Close();
            }
            catch
            {
            }

        }
    }
}
