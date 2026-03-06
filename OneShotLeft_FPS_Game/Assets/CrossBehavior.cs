using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// SETUP HIÉRARCHIE UI :
/// Canvas
/// └── CrosshairHUD  ← ce script
///     ├── TickLeft       (Image, 12x2px blanc)
///     ├── TickRight      (Image, 12x2px blanc)
///     ├── DotCenter      (Image, cercle ~10px) — désactivé par défaut
///     └── ZombieHUD      (GameObject vide)
///         ├── HeartFill  (Image, Type=Filled, Method=Vertical, Origin=Bottom)
///         └── HeartOutline (Image, sprite cœur outline)
/// </summary>
public class CrosshairHUD : MonoBehaviour
{
    [Header("Caméra & Détection")]
    [SerializeField] private Camera cam;
    [SerializeField] private float detectionDistance = 20f;
    [SerializeField] private LayerMask zombieLayer;

    [Header("Tirets gauche / droite")]
    [SerializeField] private RectTransform tickLeft;
    [SerializeField] private RectTransform tickRight;

    [Tooltip("Position X des tirets — état NORMAL (pas de visée)")]
    [SerializeField] private float tickXNormal = 18f;
    [Tooltip("Position X des tirets — état VISÉE (clic droit)")]
    [SerializeField] private float tickXAiming = 10f;
    [SerializeField] private float tickAnimSpeed = 8f;

    [Header("Rond central")]
    [SerializeField] private GameObject dotCenter;

    [Header("Couleurs")]
    [SerializeField] private Color colorNormal = Color.white;
    [SerializeField] private Color colorDetect = new Color(1f, 0.2f, 0.3f);

    [Header("HUD Cœur")]
    [SerializeField] private RectTransform zombieHUD;
    [SerializeField] private Image heartFill;

    [Tooltip("Échelle du cœur quand rien n'est détecté")]
    [SerializeField] private float heartScaleHidden = 0f;
    [Tooltip("Échelle du cœur quand un zombie est détecté")]
    [SerializeField] private float heartScaleVisible = 1f;
    [SerializeField] private float heartZoomSpeed = 6f;

    // État interne
    private float currentTickX;
    private float currentHeartScale;
    private ZombieEnemy lockedZombie;

    void Start()
    {
        if (cam == null) cam = Camera.main;
        currentTickX = tickXNormal;
        currentHeartScale = heartScaleHidden;
        zombieHUD.localScale = Vector3.zero;
        dotCenter.SetActive(false);
    }

    void Update()
    {
        bool isAiming = Input.GetMouseButton(1);

        DetectZombie();
        UpdateCrosshair(isAiming);
        UpdateHeart();
    }

    // ─── Détection ────────────────────────────────────────────────────────────
    void DetectZombie()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance, zombieLayer))
            lockedZombie = hit.collider.GetComponentInParent<ZombieEnemy>();
        else
            lockedZombie = null;

        if (lockedZombie != null && lockedZombie.IsDead())
            lockedZombie = null;
    }

    // ─── Viseur ───────────────────────────────────────────────────────────────
    void UpdateCrosshair(bool isAiming)
    {
        bool detected = lockedZombie != null;

        float targetX = isAiming ? tickXAiming : tickXNormal;
        currentTickX = Mathf.Lerp(currentTickX, targetX, Time.deltaTime * tickAnimSpeed);

        Color c = detected ? colorDetect : colorNormal;

        tickLeft.anchoredPosition = new Vector2(-currentTickX, 0f);
        tickRight.anchoredPosition = new Vector2(currentTickX, 0f);
        tickLeft.GetComponent<Image>().color = c;
        tickRight.GetComponent<Image>().color = c;

        dotCenter.SetActive(detected);
    }

    // ─── Cœur zombie — zoom in / out ─────────────────────────────────────────
    void UpdateHeart()
    {
        bool detected = lockedZombie != null;

        float targetScale = detected ? heartScaleVisible : heartScaleHidden;
        currentHeartScale = Mathf.Lerp(currentHeartScale, targetScale, Time.deltaTime * heartZoomSpeed);
        zombieHUD.localScale = Vector3.one * currentHeartScale;

        if (!detected) return;

        float pct = Mathf.Clamp01((float)lockedZombie.GetHealth() / lockedZombie.GetMaxHealth());
        heartFill.fillAmount = pct;
        heartFill.color = Color.Lerp(colorDetect, new Color(1f, 0.65f, 0.65f), pct);
    }
}