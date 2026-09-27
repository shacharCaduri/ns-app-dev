using UnityEngine;

namespace WizardArena.World
{
    // The world-space area that free-moving things (flying enemies) keep their centre in.
    // One per scene, placed by the scene builder. Code reads it through Active, so enemies
    // spawned from prefabs need no scene reference.
    public sealed class ArenaBounds : MonoBehaviour
    {
        [SerializeField] private Rect area = Rect.MinMaxRect(-7f, -1.8f, 7f, 3.5f);

        public static ArenaBounds Active { get; private set; }

        public Rect Area => area;

        // For the scene builder and tests.
        public void Configure(Rect worldArea)
        {
            area = worldArea;
        }

        public bool Contains(Vector2 point)
        {
            return point.x >= area.xMin && point.x <= area.xMax && point.y >= area.yMin && point.y <= area.yMax;
        }

        private void OnEnable() { Active = this; }

        private void OnDisable() { if (Active == this) Active = null; }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(area.center, area.size);
        }
    }
}
