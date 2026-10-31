using UnityEditor;
using UnityEditor.SceneManagement;

namespace NuclearProtection.EditorTools
{
    /// <summary>
    /// Le bouton Play démarre toujours depuis la scène Boot (où vit le NetworkManager),
    /// quelle que soit la scène ouverte. Désactivable via le menu.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromBootScene
    {
        private const string BootPath = "Assets/_Game/Scenes/Boot.unity";
        private const string MenuPath = "Nuclear Protection/Play depuis Boot";
        private const string PrefKey = "NP_PlayFromBoot";

        static PlayFromBootScene() => EditorApplication.delayCall += Apply;

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey, true));
            Apply();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, true));
            return true;
        }

        private static void Apply()
        {
            EditorSceneManager.playModeStartScene = EditorPrefs.GetBool(PrefKey, true)
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(BootPath)
                : null;
        }
    }
}
