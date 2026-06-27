using System;
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

        private static readonly string[] RequiredAssemblies =
        {
            "R3",
            "R3.Unity",
            "ObservableCollections",
            "ObservableCollections.R3"
        };

        private const string GeneratedAsmdef = @"{
    ""name"": ""LoogaSoft.Menu.R3"",
    ""rootNamespace"": ""LoogaSoft.Menu"",
    ""references"": [
        ""LoogaSoft.Menu.Runtime"",
        ""R3.Unity"",
        ""ObservableCollections"",
        ""ObservableCollections.R3""
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
using System.Collections.Generic;
using System.Threading;
using ObservableCollections;
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

        public static ObservableList<LoogaMenuScreenDefinition> CreateObservableOpenScreens(
            this LoogaMenuRoot root,
            CancellationToken cancellationToken = default)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            if (root.MenuManager == null)
                throw new InvalidOperationException(""The menu root has not initialized its menu manager yet."");

            return root.MenuManager.CreateObservableOpenScreens(cancellationToken);
        }

        public static ObservableList<LoogaMenuScreenDefinition> CreateObservableOpenScreens(
            this LoogaMenuManager manager,
            CancellationToken cancellationToken = default)
        {
            if (manager == null)
                throw new ArgumentNullException(nameof(manager));

            ObservableList<LoogaMenuScreenDefinition> openScreens = new();
            CopyOpenScreens(manager.OpenScreens, openScreens);

            void HandleStateChanged(LoogaMenuState state)
            {
                CopyOpenScreens(state.OpenScreens, openScreens);
            }

            manager.StateChanged += HandleStateChanged;

            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => manager.StateChanged -= HandleStateChanged);
            }

            return openScreens;
        }

        private static void CopyOpenScreens(
            IReadOnlyList<LoogaMenuScreenDefinition> source,
            ObservableList<LoogaMenuScreenDefinition> target)
        {
            if (OpenScreensMatch(source, target))
                return;

            target.Clear();

            if (source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }
        }

        private static bool OpenScreensMatch(
            IReadOnlyList<LoogaMenuScreenDefinition> source,
            ObservableList<LoogaMenuScreenDefinition> target)
        {
            int sourceCount = source?.Count ?? 0;
            if (sourceCount != target.Count)
                return false;

            for (int i = 0; i < sourceCount; i++)
            {
                if (!ReferenceEquals(source[i], target[i]))
                    return false;
            }

            return true;
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

            if (!ReactiveDependenciesAreAvailable(out string missingAssemblies))
            {
                EditorUtility.DisplayDialog(
                    "Reactive Dependencies Not Found",
                    "Install R3, R3.Unity, ObservableCollections, and ObservableCollections.R3 before enabling Looga Menu Framework R3 support.\n\nMissing: " + missingAssemblies,
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

        private static bool ReactiveDependenciesAreAvailable(out string missingAssemblies)
        {
            string[] missing = RequiredAssemblies
                .Where(assemblyName => !AssemblyIsAvailable(assemblyName))
                .ToArray();

            missingAssemblies = string.Join(", ", missing);
            return missing.Length == 0;
        }

        private static bool AssemblyIsAvailable(string assemblyName)
        {
            if (CompilationPipeline.GetAssemblies().Any(assembly => assembly.name == assemblyName))
                return true;

            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == assemblyName))
                return true;

            return AssetDatabase.FindAssets($"{assemblyName} t:AssemblyDefinitionAsset").Length > 0;
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

