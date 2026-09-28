using UnityEditor;
using UnityEngine;

namespace PackageStructure.Editor
{
    public sealed class DependencyInstallerWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Package Structure/Dependency Installer";

        [MenuItem(MenuPath)]
        private static void Open()
        {
            GetWindow<DependencyInstallerWindow>("Dependencies");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Online dependency installer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Install each dependency manually. Git packages are fetched from their official repositories; DOTween is downloaded from Demigiant. Event Bus uses this project's included source.", MessageType.Info);

            DrawPackageRow("Unity MCP", DependencyInstallCatalog.UnityMcpName, DependencyInstallCatalog.UnityMcpUrl);
            DrawPackageRow("UniTask", DependencyInstallCatalog.UniTaskName, DependencyInstallCatalog.UniTaskUrl);
            DrawPackageRow("VContainer", DependencyInstallCatalog.VContainerName, DependencyInstallCatalog.VContainerUrl);
            DrawDotweenRow();
            DrawEventBusRow();

            if (DependencyInstallService.IsBusy)
            {
                EditorGUILayout.HelpBox("Installation is running. Check the Console for the result.", MessageType.None);
            }

        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private static void DrawPackageRow(string displayName, string packageName, string packageUrl)
        {
            var installed = DependencyInstallService.IsPackageInstalled(packageName);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(displayName, installed ? "Installed" : "Not installed");

                using (new EditorGUI.DisabledScope(installed || DependencyInstallService.IsBusy))
                {
                    if (GUILayout.Button("Install", GUILayout.Width(90)))
                    {
                        DependencyInstallService.InstallPackage(packageName, packageUrl);
                    }
                }
            }
        }

        private static void DrawDotweenRow()
        {
            var installed = DependencyInstallService.IsDotweenInstalled();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("DOTween", installed ? "Installed" : "Not installed");

                using (new EditorGUI.DisabledScope(installed || DependencyInstallService.IsBusy))
                {
                    if (GUILayout.Button("Install", GUILayout.Width(90)))
                    {
                        DependencyInstallService.InstallDotween();
                    }
                }
            }

            if (installed)
            {
                if (GUILayout.Button("Open DOTween Setup"))
                {
                    EditorApplication.ExecuteMenuItem(DependencyInstallCatalog.DotweenUtilityPanelMenu);
                }

                EditorGUILayout.HelpBox("Click 'Setup DOTween...' in the Utility Panel after a new import.", MessageType.None);
            }
        }

        private static void DrawEventBusRow()
        {
            var installed = DependencyInstallService.IsEventBusInstalled();
            var dependenciesReady = DependencyInstallService.IsPackageInstalled(DependencyInstallCatalog.UniTaskName)
                && DependencyInstallService.IsPackageInstalled(DependencyInstallCatalog.VContainerName);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Event Bus", installed ? "Installed" : "Not installed");

                using (new EditorGUI.DisabledScope(installed || !dependenciesReady || DependencyInstallService.IsBusy))
                {
                    if (GUILayout.Button("Install", GUILayout.Width(90)))
                    {
                        DependencyInstallService.InstallEventBus();
                    }
                }
            }

            if (!installed && !dependenciesReady)
            {
                EditorGUILayout.HelpBox("Install UniTask and VContainer first.", MessageType.None);
            }
        }
    }
}
