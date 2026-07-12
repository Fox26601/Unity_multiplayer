using UnityEngine;
using UnityEngine.SceneManagement;

namespace FusionMultiplayer.Core
{
    /// <summary>
    /// Opening scene bootstrap: persists token prefs and loads the main menu.
    /// ConnectionManager lives in 00_MainMenu (with PlayerData prefab wired).
    /// </summary>
    public sealed class BootLoader : MonoBehaviour
    {
        private static bool _booted;

        private void Awake()
        {
            if (_booted)
            {
                Destroy(gameObject);
                return;
            }

            _booted = true;
            DontDestroyOnLoad(gameObject);
            SessionData.EnsureReconnectToken();
            Application.runInBackground = true;

            // Always enter MainMenu — it owns ConnectionManager + PlayerData prefab.
            // Dedicated (-dedicated) starts the server from ConnectionManager.Start there.
            if (SceneManager.GetActiveScene().buildIndex != SceneIndices.MainMenu)
                SceneManager.LoadScene(SceneIndices.MainMenu);
        }
    }
}
