using System;
using UnityEngine;
using WizardArena.Player;

namespace WizardArena.Stage
{
    // The stage exit. Hidden until StageManager opens it, then grows and fades in; once fully
    // shown, the wizard stepping onto it is drawn in (WizardController.EnterPortal) and the run
    // is marked cleared once it has fully vanished.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StagePortal : MonoBehaviour
    {
        [SerializeField] private StagePortalConfig config;
        [SerializeField] private WizardController player;
        [SerializeField] private GameSession session;

        private SpriteRenderer spriteRenderer;
        private Vector3 fullScale;
        private float revealTime;
        private bool playerEntering;

        // The portal started to appear.
        public event Action Opened;
        // The player stepped in; its vanish has started.
        public event Action PlayerEntered;
        // The player's vanish finished: it has left the stage (session is now StageCleared).
        public event Action PlayerExited;

        public bool IsOpen { get; private set; }
        // Fully revealed: the player can enter.
        public bool IsReady => IsOpen && revealTime >= config.RevealSeconds;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            fullScale = transform.localScale;
            Hide();
        }

        private void OnDestroy()
        {
            if (player != null) player.Vanished -= OnPlayerVanished;
        }

        // Called by StageManager once every enemy is dead.
        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            revealTime = 0f;
            spriteRenderer.enabled = true;
            ShowReveal(0f);
            Opened?.Invoke();
        }

        // Ignored while the player is on the way through.
        public void Close()
        {
            if (!IsOpen || playerEntering) return;
            IsOpen = false;
            Hide();
        }

        private void Update()
        {
            if (!IsOpen || playerEntering) return;
            if (!IsReady)
            {
                revealTime += Time.deltaTime;
                ShowReveal(Mathf.Clamp01(revealTime / config.RevealSeconds));
                return;
            }
            if (PlayerIsInside()) BeginEntering();
        }

        private bool PlayerIsInside()
        {
            if (!player.IsInPlay || player.IsDefeated) return false;
            Vector2 offset = player.transform.position - transform.position;
            Vector2 half = config.EntryHalfSize;
            return Mathf.Abs(offset.x) < half.x && Mathf.Abs(offset.y) < half.y;
        }

        private void BeginEntering()
        {
            playerEntering = true;
            player.Vanished += OnPlayerVanished;
            PlayerEntered?.Invoke();
            player.EnterPortal(transform.position);
        }

        private void OnPlayerVanished()
        {
            player.Vanished -= OnPlayerVanished;
            playerEntering = false;
            session.End(GameState.StageCleared);
            PlayerExited?.Invoke();
        }

        private void ShowReveal(float progress)
        {
            transform.localScale = fullScale * Mathf.Lerp(config.RevealStartScale, 1f, progress);
            spriteRenderer.color = new Color(1f, 1f, 1f, progress);
        }

        private void Hide()
        {
            spriteRenderer.enabled = false;
            transform.localScale = fullScale;
        }
    }
}
