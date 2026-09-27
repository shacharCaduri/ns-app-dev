using System;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Player
{
    // Composes the player parts and runs them in a fixed order each frame:
    // input -> cast start -> motor -> hit feedback -> caster -> animator.
    // The parts have no Update of their own, so the order never depends on Unity.
    [RequireComponent(typeof(Health), typeof(PlayerMotor), typeof(PlayerCaster))]
    [RequireComponent(typeof(PlayerAnimator), typeof(PlayerHitFeedback), typeof(PlayerTransitions))]
    public sealed class WizardController : MonoBehaviour
    {
        private IPlayerInput input;
        private Health health;
        private PlayerMotor motor;
        private PlayerCaster caster;
        private PlayerAnimator animator;
        private PlayerHitFeedback hitFeedback;
        private PlayerTransitions transitions;
        private float facing = 1f;

        public event Action Appeared;
        public event Action Vanished;

        public bool IsDefeated => health.IsDead;
        public bool IsInPlay => transitions.IsInPlay;
        public bool IsGrounded => motor.IsGrounded;
        // 1 = right, -1 = left. Locked while a cast is running.
        public float Facing => facing;

        private void Awake()
        {
            input = GetComponent<IPlayerInput>();
            health = GetComponent<Health>();
            motor = GetComponent<PlayerMotor>();
            caster = GetComponent<PlayerCaster>();
            animator = GetComponent<PlayerAnimator>();
            hitFeedback = GetComponent<PlayerHitFeedback>();
            transitions = GetComponent<PlayerTransitions>();
        }

        private void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            transitions.Appeared += RaiseAppeared;
            transitions.Vanished += RaiseVanished;
        }

        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            transitions.Appeared -= RaiseAppeared;
            transitions.Vanished -= RaiseVanished;
        }

        // Swap the input source (tests, replays, AI).
        public void UseInput(IPlayerInput playerInput)
        {
            input = playerInput;
        }

        // Shrinks the wizard into the portal; Vanished fires when it is gone.
        public void EnterPortal(Vector3 destination)
        {
            if (!IsInPlay || IsDefeated) return;
            BeginVanishing(destination);
        }

        // Moves the wizard to a stage's spawn point, restores full health and replays the
        // appear transition. Used by StageRunner when a stage starts or restarts.
        public void Respawn(Vector3 position)
        {
            health.Revive();
            caster.Cancel();
            hitFeedback.Cancel();
            animator.ShowIdle();
            motor.ResetVelocity();
            transitions.Respawn(position);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (!IsInPlay)
            {
                transitions.Tick(deltaTime);
                return;
            }

            bool canAct = input != null && !IsDefeated;
            float horizontal = canAct ? input.Horizontal : 0f;
            bool jumpPressed = canAct && input.JumpPressed;
            bool castPressed = canAct && input.CastPressed;

            if (horizontal != 0f && !caster.IsCasting) facing = Mathf.Sign(horizontal);
            if (castPressed) caster.TryBeginCast(facing);

            motor.Tick(horizontal, jumpPressed, deltaTime);
            hitFeedback.Tick(deltaTime);
            if (caster.Tick(deltaTime)) animator.ShowCast(caster.CastFacing, caster.CastTime);
            else animator.ShowMovement(horizontal, deltaTime);
        }

        private void OnDamaged(DamageInfo damage)
        {
            motor.Knockback(damage.SourcePosition);
        }

        private void OnDied()
        {
            BeginVanishing(transform.position);
        }

        private void BeginVanishing(Vector3 destination)
        {
            caster.Cancel();
            hitFeedback.Cancel();
            animator.ShowIdle();
            transitions.VanishInto(destination);
        }

        private void RaiseAppeared() => Appeared?.Invoke();

        private void RaiseVanished() => Vanished?.Invoke();
    }
}
