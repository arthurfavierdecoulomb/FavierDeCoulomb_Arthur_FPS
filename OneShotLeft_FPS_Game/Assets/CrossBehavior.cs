using UnityEngine;
using UnityEngine.UI;

public class ZombieHeartHUD : MonoBehaviour
{
    [Header("Caméra")]
    [SerializeField] private Camera cam;

    [Header("Détection")]
    [SerializeField] private float detectionDistance = 20f;
    [SerializeField] private LayerMask zombieLayer;

    [Header("UI")]
    [SerializeField] private Image heartFill;

    [Header("Animation")]
    [SerializeField] private float fillSpeed = 5f;

    private float currentFill = 0f;
    private float targetFill = 0f;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        heartFill.fillAmount = 0f;
    }

    void Update()
    {
        if (heartFill == null) return;

        // ── Détection ──────────────────────────────────────────────
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        ZombieEnemy zombie = null;

        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance, zombieLayer))
            zombie = hit.collider.GetComponentInParent<ZombieEnemy>();

        bool detected = zombie != null && !zombie.IsDead();

        // ── Target fill ────────────────────────────────────────────
        targetFill = detected
            ? Mathf.Clamp01((float)zombie.GetHealth() / zombie.GetMaxHealth())
            : 0f;

        // ── Transition smooth ──────────────────────────────────────
        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * fillSpeed);
        heartFill.fillAmount = currentFill;
    }
}