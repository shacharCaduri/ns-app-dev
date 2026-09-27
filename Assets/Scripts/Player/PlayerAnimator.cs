using UnityEngine;

namespace WizardArena.Player
{
    // Picks the wizard's sprite. WizardController tells it what the wizard is doing.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite[] walkRightFrames;
        [SerializeField] private Sprite[] walkLeftFrames;
        [SerializeField] private Sprite[] attackRightFrames;
        [SerializeField] private Sprite[] attackLeftFrames;

        private SpriteRenderer spriteRenderer;
        private float walkTime;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ShowIdle();
        }

        internal void ShowIdle()
        {
            spriteRenderer.sprite = idleSprite;
        }

        internal void ShowMovement(float horizontal, float deltaTime)
        {
            if (Mathf.Approximately(horizontal, 0f))
            {
                walkTime = 0f;
                ShowIdle();
                return;
            }

            Sprite[] frames = horizontal > 0f ? walkRightFrames : walkLeftFrames;
            if (frames == null || frames.Length == 0) return;
            walkTime += deltaTime;
            int frame = Mathf.FloorToInt(walkTime * config.WalkFramesPerSecond) % frames.Length;
            spriteRenderer.sprite = frames[frame];
        }

        internal void ShowCast(float facing, float castTime)
        {
            Sprite[] frames = facing > 0f ? attackRightFrames : attackLeftFrames;
            if (frames == null || frames.Length == 0) return;
            spriteRenderer.sprite = frames[Mathf.Min((int)(castTime / config.CastFrameSeconds), frames.Length - 1)];
        }
    }
}
