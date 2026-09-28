using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Networking;

namespace PackageStructure.Editor
{
    internal static class DependencyInstallService
    {
        private static AddRequest _packageRequest;
        private static UnityWebRequest _dotweenRequest;
        private static string _dotweenDownloadPath;

        internal static bool IsBusy => _packageRequest != null || _dotweenRequest != null;

        internal static bool IsPackageInstalled(string packageName)
        {
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();

            for (var index = 0; index < packages.Length; index++)
            {
                if (packages[index].name == packageName)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsDotweenInstalled()
        {
            return File.Exists(DependencyInstallCatalog.DotweenDllPath);
        }

        internal static bool IsEventBusInstalled()
        {
            return File.Exists(Path.Combine(DependencyInstallCatalog.EventBusDirectory, "EventBus.cs"))
                && File.Exists(Path.Combine(DependencyInstallCatalog.EventBusDirectory, "IEventBus.cs"));
        }

        internal static void InstallPackage(string packageName, string packageUrl)
        {
            if (IsBusy || IsPackageInstalled(packageName))
            {
                return;
            }

            _packageRequest = Client.Add(packageUrl);
            EditorApplication.update += PollPackageRequest;
            Debug.Log($"Installing {packageName} from its official Git repository.");
        }

        internal static void InstallDotween()
        {
            if (IsBusy || IsDotweenInstalled())
            {
                return;
            }

            _dotweenDownloadPath = Path.Combine(Path.GetTempPath(), $"DOTween_{Guid.NewGuid():N}.zip");
            _dotweenRequest = UnityWebRequest.Get(DependencyInstallCatalog.DotweenDownloadUrl);
            _dotweenRequest.downloadHandler = new DownloadHandlerFile(_dotweenDownloadPath);
            var operation = _dotweenRequest.SendWebRequest();
            operation.completed += OnDotweenDownloaded;
            Debug.Log($"Downloading DOTween {DependencyInstallCatalog.DotweenVersion} from Demigiant.");
        }

        internal static void InstallEventBus()
        {
            if (IsBusy || IsEventBusInstalled()
                || !IsPackageInstalled(DependencyInstallCatalog.UniTaskName)
                || !IsPackageInstalled(DependencyInstallCatalog.VContainerName))
            {
                return;
            }

            var interfacePath = Path.Combine(DependencyInstallCatalog.EventBusDirectory, "IEventBus.cs");
            var implementationPath = Path.Combine(DependencyInstallCatalog.EventBusDirectory, "EventBus.cs");
            var assemblyPath = Path.Combine(DependencyInstallCatalog.EventBusDirectory, "EventSystem.asmdef");

            if (File.Exists(interfacePath) || File.Exists(implementationPath) || File.Exists(assemblyPath))
            {
                Debug.LogError("Event Bus target files already exist. Resolve them before installing.");
                return;
            }

            Directory.CreateDirectory(DependencyInstallCatalog.EventBusDirectory);
            CopyTemplate("IEventBus.cs.txt", "IEventBus.cs");
            CopyTemplate("EventBus.cs.txt", "EventBus.cs");
            CopyTemplate("EventSystem.asmdef.txt", "EventSystem.asmdef");
            AssetDatabase.Refresh();
            Debug.Log("Event Bus installed from the included project source.");
        }

        private static void PollPackageRequest()
        {
            if (_packageRequest == null || !_packageRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= PollPackageRequest;

            if (_packageRequest.Status == StatusCode.Success)
            {
                Debug.Log($"Installed {_packageRequest.Result.name} {_packageRequest.Result.version}.");
            }
            else
            {
                Debug.LogError($"Package installation failed: {_packageRequest.Error?.message}");
            }

            _packageRequest = null;
        }

        private static void OnDotweenDownloaded(AsyncOperation operation)
        {
            try
            {
                if (_dotweenRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"DOTween download failed: {_dotweenRequest.error}");
                    return;
                }

                using (var archive = ZipFile.OpenRead(_dotweenDownloadPath))
                {
                    var entry = archive.GetEntry(DependencyInstallCatalog.DotweenPackageName);

                    if (entry == null)
                    {
                        Debug.LogError("The official DOTween download does not contain the expected Unity package.");
                        return;
                    }

                    var packagePath = Path.Combine(Path.GetDirectoryName(_dotweenDownloadPath),
                        $"DOTween_{Guid.NewGuid():N}.unitypackage");
                    entry.ExtractToFile(packagePath);
                    AssetDatabase.ImportPackage(packagePath, false);
                }

                Debug.Log("DOTween imported. Complete 'Setup DOTween...' in Tools > Demigiant > DOTween Utility Panel.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _dotweenRequest.Dispose();
                _dotweenRequest = null;
                _dotweenDownloadPath = null;
            }
        }

        private static void CopyTemplate(string sourceName, string destinationName)
        {
            var sourcePath = Path.Combine(DependencyInstallCatalog.TemplateDirectory, sourceName);
            var destinationPath = Path.Combine(DependencyInstallCatalog.EventBusDirectory, destinationName);
            File.Copy(sourcePath, destinationPath, false);
        }
    }
}
