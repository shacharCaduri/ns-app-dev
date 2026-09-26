using UnityEngine;

public sealed class ArcaneBolt : MonoBehaviour
{
    private float direction;
    private float lifetime;
    private Vector3 previousPosition;
    private BatEnemyController[] targets;

    public static void Fire(Vector3 position, float direction, Sprite sprite)
    {
        GameObject bolt = new GameObject("Arcane Bolt");
        bolt.transform.position = position;
        ArcaneBolt projectile = bolt.AddComponent<ArcaneBolt>();
        projectile.direction = direction;
        projectile.targets = Object.FindObjectsByType<BatEnemyController>(FindObjectsSortMode.None);
        SpriteRenderer renderer = bolt.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 20;
        if (sprite != null) bolt.transform.localScale = Vector3.one * (0.7f / sprite.bounds.size.x);
    }

    private void Update()
    {
        previousPosition = transform.position;
        transform.position += Vector3.right * (direction * 11f * Time.deltaTime);
        foreach (BatEnemyController target in targets)
        {
            if (target != null && target.TryHit(previousPosition, transform.position))
            {
                Destroy(gameObject);
                return;
            }
        }
        lifetime += Time.deltaTime;
        if (lifetime >= 2f) Destroy(gameObject);
    }

}
