using UnityEngine;

public sealed class BatEnemyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField, Min(1)] private int attackPower = 2;
    [SerializeField] private float animationFramesPerSecond = 7f;
    [SerializeField] private Sprite[] flyRightFrames;
    [SerializeField] private Sprite[] flyLeftFrames;
    [SerializeField] private Sprite deadLeft;
    [SerializeField] private Sprite deadRight;
    [SerializeField] private Sprite hurtLeft;
    [SerializeField] private Sprite hurtRight;
    [SerializeField] private GameObject exitPortal;

    private readonly Vector2 minimumPosition = new Vector2(-7f, -1.8f);
    private readonly Vector2 maximumPosition = new Vector2(7f, 3.5f);
    private SpriteRenderer spriteRenderer;
    private Vector2 direction;
    private float animationTime;
    private float directionTimer;
    private float contactCooldown;
    private CircleCollider2D batCollider;
    private CircleCollider2D wizardCollider;
    private WizardController wizard;
    private const int MaxHealth = 3;
    private int health = MaxHealth;
    private const float HurtDuration = 0.3f;
    private float hurtTimer;
    private float hitDirection;
    private HealthBarStyle healthStyle;
    private Color healthBackground;
    private Color healthFill;
    private float deathTime;
    private float fallSpeed;
    private bool landed;
    private const float FloorY = -2.4f;

    [System.Serializable]
    private class HealthBarStyle
    {
        public float width = 64;
        public float height = 9;
        public float border = 2;
        public string background = "#162238";
        public string fill = "#EF5564";
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        TextAsset style = Resources.Load<TextAsset>("UI/BatHealthBar");
        healthStyle = style != null ? JsonUtility.FromJson<HealthBarStyle>(style.text) : new HealthBarStyle();
        ColorUtility.TryParseHtmlString(healthStyle.background, out healthBackground);
        ColorUtility.TryParseHtmlString(healthStyle.fill, out healthFill);
        batCollider = GetComponent<CircleCollider2D>();
        wizard = FindFirstObjectByType<WizardController>();
        if (wizard != null)
        {
            wizardCollider = wizard.GetComponent<CircleCollider2D>();
        }
        ChooseNewDirection();
    }

    private void Update()
    {
        if (hurtTimer > 0f)
        {
            hurtTimer = Mathf.Max(0f, hurtTimer - Time.deltaTime);
            float remaining = hurtTimer / HurtDuration;
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(position.x + hitDirection * 3f * remaining * Time.deltaTime,
                minimumPosition.x, maximumPosition.x);
            transform.position = position;
            transform.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Sin((1f - remaining) * Mathf.PI * 2f) * 12f * remaining);
            // Keep the hurt pose visible, including on the lethal hit, before
            // returning to flight or beginning the existing death sequence.
            return;
        }
        if (health <= 0)
        {
            if (!landed)
            {
                fallSpeed += 9f * Time.deltaTime;
                transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
                if (spriteRenderer.bounds.min.y <= FloorY)
                {
                    landed = true;
                    Sprite deathSprite = direction.x >= 0f ? deadRight : deadLeft;
                    if (deathSprite != null) spriteRenderer.sprite = deathSprite;
                    transform.rotation = Quaternion.identity;
                    transform.position += Vector3.up * (FloorY - spriteRenderer.bounds.min.y);
                    deathTime = 0f;
                }
                return;
            }
            deathTime += Time.deltaTime;
            // Hold for two seconds AFTER landing, then fade for three seconds.
            spriteRenderer.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((deathTime - 2f) / 3f));
            if (deathTime >= 5f)
            {
                Destroy(gameObject);
            }
            return;
        }
        directionTimer -= Time.deltaTime;
        contactCooldown -= Time.deltaTime;

        if (directionTimer <= 0f)
        {
            ChooseNewDirection();
        }

        Vector2 nextPosition = (Vector2)transform.position + direction * (moveSpeed * Time.deltaTime);

        if (nextPosition.x <= minimumPosition.x || nextPosition.x >= maximumPosition.x)
        {
            direction.x *= -1f;
            nextPosition.x = Mathf.Clamp(nextPosition.x, minimumPosition.x, maximumPosition.x);
        }

        if (nextPosition.y <= minimumPosition.y || nextPosition.y >= maximumPosition.y)
        {
            direction.y *= -1f;
            nextPosition.y = Mathf.Clamp(nextPosition.y, minimumPosition.y, maximumPosition.y);
        }

        transform.position = nextPosition;
        AnimateFlight();
    }

    // Both characters move through their transforms. Check their current circles
    // every frame instead of relying on a sleeping kinematic trigger's enter event.
    private void LateUpdate()
    {
        if (health <= 0 || hurtTimer > 0f || wizard == null || wizard.IsDefeated || wizardCollider == null || batCollider == null || contactCooldown > 0f)
        {
            return;
        }

        Vector2 batCenter = transform.TransformPoint(batCollider.offset);
        Vector2 wizardCenter = wizard.transform.TransformPoint(wizardCollider.offset);
        Vector3 batScale = transform.lossyScale;
        Vector3 wizardScale = wizard.transform.lossyScale;
        float combinedRadius = batCollider.radius * Mathf.Max(Mathf.Abs(batScale.x), Mathf.Abs(batScale.y))
            + wizardCollider.radius * Mathf.Max(Mathf.Abs(wizardScale.x), Mathf.Abs(wizardScale.y));
        if ((batCenter - wizardCenter).sqrMagnitude > combinedRadius * combinedRadius)
        {
            return;
        }

        wizard.TakeContactDamage(attackPower, transform.position.x);
        Vector2 awayFromWizard = (batCenter - wizardCenter).normalized;
        direction = awayFromWizard.sqrMagnitude > 0f ? awayFromWizard : -direction;
        transform.position += (Vector3)(direction * 0.45f);
        directionTimer = Random.Range(1.2f, 2.5f);
        contactCooldown = 0.5f;
    }

    private void ChooseNewDirection()
    {
        direction = Random.insideUnitCircle.normalized;
        if (Mathf.Abs(direction.x) < 0.35f)
        {
            direction.x = Mathf.Sign(direction.x == 0f ? Random.Range(-1f, 1f) : direction.x) * 0.35f;
            direction.Normalize();
        }

        directionTimer = Random.Range(1.2f, 3f);
    }

    public bool TryHit(Vector2 start, Vector2 end)
    {
        if (health <= 0) return false;
        Vector2 center = transform.TransformPoint(batCollider.offset);
        Vector2 segment = end - start;
        float t = segment.sqrMagnitude > 0f ? Mathf.Clamp01(Vector2.Dot(center - start, segment) / segment.sqrMagnitude) : 0f;
        float radius = batCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)) + 0.12f;
        if ((center - (start + segment * t)).sqrMagnitude > radius * radius) return false;
        Sprite hurtSprite = direction.x >= 0f ? hurtRight : hurtLeft;
        if (hurtSprite != null) spriteRenderer.sprite = hurtSprite;
        hurtTimer = HurtDuration;
        hitDirection = end.x >= start.x ? 1f : -1f;
        health--;
        direction = new Vector2(hitDirection, 0.5f).normalized;
        directionTimer = 1f;
        if (health <= 0)
        {
            batCollider.enabled = false;
        }
        return true;
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;
        if (camera == null || (health <= 0 && landed)) return;
        Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 0.9f);
        Rect bar = new Rect(screen.x - healthStyle.width / 2f, Screen.height - screen.y, healthStyle.width, healthStyle.height);
        Color previousColor = GUI.color;
        GUI.color = healthBackground;
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = healthFill;
        float inset = healthStyle.border;
        if (health > 0)
            GUI.DrawTexture(new Rect(bar.x + inset, bar.y + inset, (bar.width - 2f * inset) * health / MaxHealth, bar.height - 2f * inset), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        Rect label = new Rect(bar.x - 12f, bar.y - 22f, bar.width + 24f, 22f);
        labelStyle.normal.textColor = Color.black;
        GUI.Label(new Rect(label.x + 1f, label.y + 1f, label.width, label.height), $"{health} / {MaxHealth} HP", labelStyle);
        labelStyle.normal.textColor = Color.white;
        GUI.Label(label, $"{health} / {MaxHealth} HP", labelStyle);
        GUI.color = previousColor;
    }

    private void AnimateFlight()
    {
        Sprite[] frames = direction.x >= 0f ? flyRightFrames : flyLeftFrames;
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        animationTime += Time.deltaTime;
        int frameIndex = Mathf.FloorToInt(animationTime * animationFramesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }
}
