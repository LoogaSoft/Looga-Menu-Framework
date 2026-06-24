using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuR3SupportMenu
    {
        private const string MenuPath = "LoogaSoft/Menu Framework/Enable R3 Support";
        private const string GeneratedFolder = "Assets/LoogaSoft/Generated/Menu Framework R3";
        private const string GeneratedAsmdefPath = GeneratedFolder + "/LoogaSoft.Menu.R3.asmdef";
        private const string GeneratedSourcePath = GeneratedFolder + "/LoogaMenuR3Extensions.cs";

        private const string GeneratedAsmdef = @"{
    ""name"": ""LoogaSoft.Menu.R3"",
    ""rootNamespace"": ""LoogaSoft.Menu"",
    ""references"": [
        ""LoogaSoft.Menu.Runtime"",
        ""R3.Unity""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}";

        private const string GeneratedSource = @"using System;
using System.Threading;
using R3;

namespace LoogaSoft.Menu
{
    public static class LoogaMenuR3Extensions
    {
        public static Observable<LoogaMenuState> StateChangedAsObservable(
            this LoogaMenuRoot root,
            CancellationToken cancellationToken = default)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (root.MenuManager == null)
                throw new InvalidOperationException(""The menu root has not initialized its menu manager yet."");

            return root.MenuManager.StateChangedAsObservable(cancellationToken);
        }

        public static Observable<LoogaMenuState> StateChangedAsObservable(
            this LoogaMenuManager manager,
            CancellationToken cancellationToken = default)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            return Observable.FromEvent<Action<LoogaMenuState>, LoogaMenuState>(
                handler => new Action<LoogaMenuState>(handler),
                handler => manager.StateChanged += handler,
                handler => manager.StateChanged -= handler,
                cancellationToken);
        }
    }
}";

        [MenuItem(MenuPath, priority = 201)]
        private static void ToggleR3Support()
        {
            if (IsEnabled())
            {
                Disable();
                return;
            }

            if (!R3IsAvailable())
            {
                EditorUtility.DisplayDialog(
                    "R3 Not Found",
                    "Install R3 before enabling Looga Menu Framework R3 support. The menu framework core package does not depend on R3, so this integration is generated only for projects that have R3 installed.",
                    "OK");
                return;
            }

            Enable();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggle()
        {
            UnityEditor.Menu.SetChecked(MenuPath, IsEnabled());
            return true;
        }

        private static bool IsEnabled()
        {
            return File.Exists(GeneratedAsmdefPath) && File.Exists(GeneratedSourcePath);
        }

        private static bool R3IsAvailable()
        {
            if (CompilationPipeline.GetAssemblies().Any(assembly => assembly.name == "R3.Unity" || assembly.name == "R3"))
                return true;

            return AssetDatabase.FindAssets("R3.Unity t:AssemblyDefinitionAsset").Length > 0
                || AssetDatabase.FindAssets("R3 t:AssemblyDefinitionAsset").Length > 0;
        }

        private static void Enable()
        {
            Directory.CreateDirectory(GeneratedFolder);
            File.WriteAllText(GeneratedAsmdefPath, GeneratedAsmdef);
            File.WriteAllText(GeneratedSourcePath, GeneratedSource);
            AssetDatabase.Refresh();
            Debug.Log("Looga Menu Framework R3 support enabled.");
        }

        private static void Disable()
        {
            DeleteAssetAndMeta(GeneratedSourcePath);
            DeleteAssetAndMeta(GeneratedAsmdefPath);

            if (Directory.Exists(GeneratedFolder) && Directory.GetFiles(GeneratedFolder).Length == 0)
            {
                Directory.Delete(GeneratedFolder);
                DeleteAssetAndMeta(GeneratedFolder + ".meta");
            }

            AssetDatabase.Refresh();
            Debug.Log("Looga Menu Framework R3 support disabled.");
        }

        private static void DeleteAssetAndMeta(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            string metaPath = path.EndsWith(".meta") ? path : path + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }
    }
}
