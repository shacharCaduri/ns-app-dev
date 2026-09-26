using UnityEngine;

public sealed class WizardController : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 10;
    [SerializeField, Min(0f)] private float damageCooldown = 0.75f;
    private int health;
    private float nextDamageTime;
    public bool IsDefeated => health <= 0;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float jumpSpeed = 8f;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float animationFramesPerSecond = 8f;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkRightFrames;
    [SerializeField] private Sprite[] walkLeftFrames;
    [SerializeField] private Sprite[] attackRightFrames;
    [SerializeField] private Sprite[] attackLeftFrames;
    [SerializeField] private Sprite projectileRight;
    [SerializeField] private Sprite projectileLeft;

    private SpriteRenderer spriteRenderer;
    private float animationTime;
    private float bumpVelocity;
    private float bumpEffectTimer;
    private float groundY;
    private float verticalVelocity;
    private float facing = 1f;
    private float castTime;
    private float castFacing;
    private bool casting;
    private bool spellReleased;
    private const float BumpEffectDuration = 0.45f;
    private float transitionTime;
    private bool appearing = true;
    private bool vanishing;
    private bool stageCleared;
    private Vector3 transitionOrigin;
    private Vector3 transitionTarget;
    private Vector3 normalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = idleSprite;
        groundY = transform.position.y;
        maxHealth = Mathf.Max(1, maxHealth);
        health = maxHealth;
        normalScale = transform.localScale;
        spriteRenderer.color = new Color(0.4f, 0.9f, 1f, 0f);
        if (GetComponent<StagePortalGate>() == null) gameObject.AddComponent<StagePortalGate>();
    }

    private void Update()
    {
        if (appearing || vanishing)
        {
            UpdateTransition();
            return;
        }
        float horizontal = 0f;

        if (!IsDefeated && Input.GetKey(KeyCode.LeftArrow))
        {
            horizontal -= 1f;
        }

        if (!IsDefeated && Input.GetKey(KeyCode.RightArrow))
        {
            horizontal += 1f;
        }
        if (horizontal != 0f && !casting) facing = Mathf.Sign(horizontal);
        if (!IsDefeated && (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !casting)
        {
            casting = true;
            spellReleased = false;
            castTime = 0f;
            castFacing = facing;
        }

        bumpVelocity = Mathf.MoveTowards(bumpVelocity, 0f, 30f * Time.deltaTime);
        ArenaSurface support = ArenaSurface.Support(transform.position);
        bool grounded = (support != null || (ArenaSurface.Active.Count == 0 && transform.position.y <= groundY + 0.001f)) && verticalVelocity <= 0f;
        bool jumping = false;
        if (!IsDefeated && grounded && Input.GetKeyDown(KeyCode.Space))
        {
            verticalVelocity = jumpSpeed * (support != null ? support.jumpMultiplier : 1f);
            jumping = true;
        }
        verticalVelocity -= gravity * Time.deltaTime;
        float horizontalVelocity = horizontal * moveSpeed * (support != null ? support.speedMultiplier : 1f) + bumpVelocity;
        Vector3 nextPosition = transform.position + new Vector3(horizontalVelocity, verticalVelocity, 0f) * Time.deltaTime;
        if (ArenaSurface.Active.Count > 0)
            nextPosition = ArenaSurface.Move(transform.position, nextPosition, ref verticalVelocity, grounded && !jumping);
        else if (nextPosition.y <= groundY)
        {
            nextPosition.y = groundY;
            verticalVelocity = 0f;
        }
        nextPosition.x = Mathf.Clamp(nextPosition.x, -9.2f, 9.2f);
        transform.position = nextPosition;
        UpdateBumpEffect();

        if (casting)
        {
            castTime += Time.deltaTime;
            Sprite[] attack = castFacing > 0f ? attackRightFrames : attackLeftFrames;
            if (attack != null && attack.Length > 0)
                spriteRenderer.sprite = attack[Mathf.Min((int)(castTime / 0.14f), attack.Length - 1)];
            if (!spellReleased && castTime >= 0.28f)
            {
                spellReleased = true;
                ArcaneBolt.Fire(transform.position + new Vector3(castFacing * 0.75f, 0.85f, 0f), castFacing,
                    castFacing > 0f ? projectileRight : projectileLeft);
            }
            if (castTime >= 0.56f) casting = false;
            return;
        }

        if (Mathf.Approximately(horizontal, 0f))
        {
            animationTime = 0f;
            spriteRenderer.sprite = idleSprite;
            return;
        }

        Sprite[] frames = horizontal > 0f ? walkRightFrames : walkLeftFrames;
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        animationTime += Time.deltaTime;
        int frameIndex = Mathf.FloorToInt(animationTime * animationFramesPerSecond) % frames.Length;
        spriteRenderer.sprite = frames[frameIndex];
    }

    public bool TakeContactDamage(int attackPower, float sourceX)
    {
        if (appearing || vanishing || IsDefeated || attackPower <= 0 || Time.time < nextDamageTime) return false;
        health = Mathf.Max(0, health - attackPower);
        nextDamageTime = Time.time + damageCooldown;
        BumpFrom(sourceX);
        if (IsDefeated) BeginVanishing(transform.position, false);
        return true;
    }

    public void EnterPortal(Vector3 destination)
    {
        if (appearing || vanishing || IsDefeated) return;
        BeginVanishing(destination, true);
    }

    private void BeginVanishing(Vector3 destination, bool completed)
    {
        vanishing = true;
        stageCleared = completed;
        transitionTime = 0f;
        transitionOrigin = transform.position;
        transitionTarget = destination;
        casting = false;
        bumpEffectTimer = 0f;
        spriteRenderer.sprite = idleSprite;
        Collider2D body = GetComponent<Collider2D>();
        if (body != null) body.enabled = false;
    }

    private void UpdateTransition()
    {
        transitionTime += Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transitionTime / (appearing ? 0.9f : 1.4f)));
        float visibility = appearing ? t : 1f - t;
        spriteRenderer.color = new Color(Mathf.Lerp(0.35f, 1f, visibility), 0.9f + 0.1f * visibility, 1f, visibility);
        transform.localScale = Vector3.Scale(normalScale, new Vector3(Mathf.Lerp(0.15f, 1f, visibility), Mathf.Lerp(1.25f, 1f, visibility), 1f));
        if (vanishing) transform.position = Vector3.Lerp(transitionOrigin, transitionTarget, t);
        if (appearing && t >= 1f)
        {
            appearing = false;
            spriteRenderer.color = Color.white;
            transform.localScale = normalScale;
        }
    }

    private void BumpFrom(float sourceX)
    {
        float directionAwayFromSource = transform.position.x >= sourceX ? 1f : -1f;
        bumpVelocity = directionAwayFromSource * 13f;
        verticalVelocity = 5f;
        bumpEffectTimer = BumpEffectDuration;
    }

    private void UpdateBumpEffect()
    {
        if (bumpEffectTimer <= 0f)
        {
            transform.localScale = Vector3.one;
            spriteRenderer.color = Color.white;
            return;
        }

        bumpEffectTimer -= Time.deltaTime;
        float progress = 1f - bumpEffectTimer / BumpEffectDuration;
        float pulse = Mathf.Sin(progress * Mathf.PI * 4f) * (1f - progress);
        transform.localScale = new Vector3(1f + pulse * 0.18f, 1f - pulse * 0.12f, 1f);
        spriteRenderer.color = Color.Lerp(new Color(1f, 0.25f, 0.25f), Color.white, progress);
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
        GUI.Label(new Rect(20f, 65f, 220f, 30f), $"Wizard: {health} / {maxHealth} HP", hintStyle);
        Color savedColor = GUI.color;
        GUI.color = new Color(0.08f, 0.12f, 0.18f);
        GUI.DrawTexture(new Rect(20f, 98f, 220f, 14f), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.85f, 0.45f);
        if (health > 0)
            GUI.DrawTexture(new Rect(22f, 100f, 216f * health / maxHealth, 10f), Texture2D.whiteTexture);
        GUI.color = savedColor;
        if (IsDefeated)
            GUI.Label(new Rect(0f, Screen.height / 2f - 25f, Screen.width, 50f), "Wizard defeated — stop and press Play to retry", titleStyle);
        if (stageCleared && transitionTime >= 1.4f)
            GUI.Label(new Rect(0f, Screen.height / 2f - 25f, Screen.width, 50f), "Stage clear!", titleStyle);

        if (bumpEffectTimer > 0f && Camera.main != null)
        {
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.3f);
            GUIStyle bumpStyle = new GUIStyle(titleStyle)
            {
                fontSize = 28,
                normal = { textColor = new Color(1f, 0.85f, 0.2f) }
            };
            GUI.Label(new Rect(screenPosition.x - 70f, Screen.height - screenPosition.y - 25f, 140f, 50f), "BUMP!", bumpStyle);
        }
    }
}
