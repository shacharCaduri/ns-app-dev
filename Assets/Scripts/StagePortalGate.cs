using UnityEngine;

// The stage owns the exit: individual enemies cannot open it early.
public sealed class StagePortalGate : MonoBehaviour
{
    private GameObject portal;
    private WizardController wizard;
    private float revealTime;
    private Vector3 portalScale;
    private SpriteRenderer portalRenderer;

    private void Start()
    {
        wizard = GetComponent<WizardController>();
        foreach (Transform candidate in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate.gameObject.scene == gameObject.scene && candidate.name == "Stage Exit Portal")
            {
                portal = candidate.gameObject;
                portalScale = candidate.localScale;
                portalRenderer = portal.GetComponent<SpriteRenderer>();
                portal.SetActive(false);
                break;
            }
        }
    }

    private void Update()
    {
        if (portal == null) return;
        // Include inactive enemies and corpses; removal happens only after fading.
        foreach (BatEnemyController enemy in FindObjectsByType<BatEnemyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy.gameObject.scene != gameObject.scene) continue;
            portal.SetActive(false);
            revealTime = 0f;
            return;
        }
        portal.SetActive(true);
        revealTime += Time.deltaTime;
        float progress = Mathf.Clamp01(revealTime / 0.8f);
        portal.transform.localScale = portalScale * Mathf.Lerp(0.7f, 1f, progress);
        if (portalRenderer != null) portalRenderer.color = new Color(1f, 1f, 1f, progress);
        Vector3 offset = wizard.transform.position - portal.transform.position;
        if (progress >= 1f && Mathf.Abs(offset.x) < 0.65f && Mathf.Abs(offset.y) < 0.5f)
            wizard.EnterPortal(portal.transform.position);
    }
}
