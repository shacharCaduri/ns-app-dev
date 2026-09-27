using UnityEngine;

namespace WizardArena.Player
{
    // Arrows walk, Space jumps, Z or Enter casts.
    public sealed class KeyboardPlayerInput : MonoBehaviour, IPlayerInput
    {
        public float Horizontal
        {
            get
            {
                float horizontal = 0f;
                if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
                if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
                return horizontal;
            }
        }

        public bool JumpPressed => Input.GetKeyDown(KeyCode.Space);

        public bool CastPressed =>
            Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
    }
}
