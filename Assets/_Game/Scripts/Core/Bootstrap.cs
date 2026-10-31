using NuclearProtection.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NuclearProtection.Core
{
    /// <summary>
    /// Scène Boot : contient le NetworkManager (persistant) puis charge le menu.
    /// On ne revient jamais dans Boot, ce qui évite les NetworkManager en double.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private void Start() => SceneManager.LoadScene(config.network.menuSceneName);
    }
}
