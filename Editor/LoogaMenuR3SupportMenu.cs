using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Menu.Editor
{
    internal static class LoogaMenuR3SupportMenu
    {
        private const string MenuPath = "LoogaSoft/Menu Framework/Enable R3 Support";
        private const string DefineSymbol = "LOOGA_MENU_R3_SUPPORT";

        private static readonly string[] RequiredAssemblies =
        {
            "R3",
            "R3.Unity",
            "ObservableCollections",
            "ObservableCollections.R3"
        };

        [MenuItem(MenuPath, priority = 201)]
        private static void ToggleR3Support()
        {
            if (IsEnabled())
            {
                Disable();
                return;
            }

            if (!LoogaMenuOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out string missingAssemblies))
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
            return LoogaMenuOptionalSupportUtility.DefineIsEnabled(DefineSymbol);
        }

        private static void Enable()
        {
            LoogaMenuOptionalSupportUtility.AddDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga Menu Framework R3 support enabled.");
        }

        private static void Disable()
        {
            LoogaMenuOptionalSupportUtility.RemoveDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga Menu Framework R3 support disabled.");
        }
    }
}