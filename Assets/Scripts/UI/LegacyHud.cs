using UnityEngine;
using UnityEngine.SceneManagement;
using WizardArena.Combat;
using WizardArena.Player;

namespace WizardArena.UI
{
    // TEMPORARY: the prototype's OnGUI HUD and end screen, moved out of WizardController
    // unchanged. Ticket [09] replaces it with a real UI and deletes this class.
    public sealed class LegacyHud : MonoBehaviour
    {
        [SerializeField] private WizardController player;

        private Health playerHealth;
        private PlayerHitFeedback hitFeedback;
        private bool stageCleared;

        private void Awake()
        {
            playerHealth = player.GetComponent<Health>();
            hitFeedback = player.GetComponent<PlayerHitFeedback>();
        }

        private void OnEnable()
        {
            player.Vanished += OnPlayerVanished;
        }

        private void OnDisable()
        {
            player.Vanished -= OnPlayerVanished;
        }

        // A vanish that wasn't a death means the wizard went through the portal.
        private void OnPlayerVanished()
        {
            if (!playerHealth.IsDead) stageCleared = true;
        }

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            GUIStyle hintStyle = new GUIStyle(titleStyle) { fontSize = 18, fontStyle = FontStyle.Normal };
            GUI.Label(new Rect(0f, 20f, Screen.width, 40f), "Wizard Movement Prototype", titleStyle);
            GUI.Label(new Rect(0f, Screen.height - 55f, Screen.width, 30f), "← / → Walk    Space Jump    Z / Enter Cast", hintStyle);
            GUI.Label(new Rect(20f, 65f, 220f, 30f), $"Wizard: {playerHealth.Current} / {playerHealth.Max} HP", hintStyle);
            Color savedColor = GUI.color;
            GUI.color = new Color(0.08f, 0.12f, 0.18f);
            GUI.DrawTexture(new Rect(20f, 98f, 220f, 14f), Texture2D.whiteTexture);
            GUI.color = new Color(0.25f, 0.85f, 0.45f);
            if (playerHealth.Current > 0)
                GUI.DrawTexture(new Rect(22f, 100f, 216f * playerHealth.Current / playerHealth.Max, 10f), Texture2D.whiteTexture);
            GUI.color = savedColor;

            if (hitFeedback.IsPlaying && Camera.main != null)
            {
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.up * 2.3f);
                GUIStyle bumpStyle = new GUIStyle(titleStyle)
                {
                    fontSize = 28,
                    normal = { textColor = new Color(1f, 0.85f, 0.2f) }
                };
                GUI.Label(new Rect(screenPosition.x - 70f, Screen.height - screenPosition.y - 25f, 140f, 50f), "BUMP!", bumpStyle);
            }
            if (playerHealth.IsDead || stageCleared)
                DrawEndScreen();
        }

        private void DrawEndScreen()
        {
            bool defeated = playerHealth.IsDead;
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.color = new Color(0.015f, 0.025f, 0.06f, 0.88f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Keep the panel readable at different Game view resolutions.
            float scale = Mathf.Min(Screen.width / 900f, Screen.height / 600f);
            GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f),
                Quaternion.identity, Vector3.one * scale);
            GUIStyle heading = new GUIStyle(GUI.skin.label)
            {
                fontSize = 64,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = defeated ? new Color(1f, 0.3f, 0.35f) : new Color(0.4f, 1f, 0.75f) }
            };
            GUI.Label(new Rect(-430f, -140f, 860f, 100f), defeated ? "GAME OVER" : "STAGE CLEARED", heading);
            GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 28, fontStyle = FontStyle.Bold };
            if (GUI.Button(new Rect(-240f, 20f, 220f, 70f), "Retry", button))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(gameObject.scene.path, LoadSceneMode.Single);
            }
#if UNITY_EDITOR
            if (GUI.Button(new Rect(20f, 20f, 220f, 70f), "Quit", button))
                UnityEditor.EditorApplication.isPlaying = false;
#else
            // Quit is specifically an Editor Play Mode control, not an app-exit action.
            bool oldEnabled = GUI.enabled;
            GUI.enabled = false;
            GUI.Button(new Rect(20f, 20f, 220f, 70f), new GUIContent("Quit", "Available in Unity Play Mode"), button);
            GUI.enabled = oldEnabled;
#endif
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }
    }
}
