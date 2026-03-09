using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public static LoadingScreen Instance;

    [Header("Panels")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject introPanel;
    [SerializeField] private GameObject progressPanel;

    [Header("Intro – Cercle")]
    [SerializeField] private RectTransform circleBG;

    [Header("Intro – Logo (parent LOGO)")]
    [SerializeField] private RectTransform logoGroup;

    [Header("Intro – Textes (enfants de LOGO)")]
    [SerializeField] private TextMeshProUGUI txtCountdown;
    [SerializeField] private TextMeshProUGUI txtOne;
    [SerializeField] private TextMeshProUGUI txtShot;
    [SerializeField] private TextMeshProUGUI txtLeft;

    [Header("Progress")]
    [SerializeField] private Image barreFill;
    [SerializeField] private TextMeshProUGUI txtEtape;

    [Header("Tailles")]
    [SerializeField] private float circleStartScale = 3f;
    [SerializeField] private Vector2 logoStartScale = Vector2.one;

    [Header("Timings")]
    [SerializeField] private float firstNumberDuration = 0.55f;
    [SerializeField] private float speedUpFactor = 0.82f;
    [SerializeField] private float shakeIntensity = 8f;
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float logoStartY = 0f;
    [SerializeField] private float circleZoomOutDur = 0.7f;
    [SerializeField] private float smoothSpeed = 5f;

    private float targetProgress = 0f;
    private float currentProgress = 0f;

    void Awake()
    {
        Instance = this;
        loadingPanel.SetActive(false);
    }

    void Update()
    {
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * smoothSpeed);
        if (barreFill != null)
            barreFill.fillAmount = currentProgress;
    }

    public void Show()
    {
        currentProgress = 0f;
        targetProgress = 0f;
        loadingPanel.SetActive(true);
        progressPanel.SetActive(false);
        introPanel.SetActive(true);
    }

    public IEnumerator WaitUntilReady()
    {
        yield return StartCoroutine(PlayIntro());
    }

    public void Hide()
    {
        loadingPanel.SetActive(false);
    }

    public void SetProgress(float pct, string etape = "")
    {
        targetProgress = Mathf.Clamp01(pct);
        if (txtEtape != null && etape != "")
            txtEtape.text = etape;
    }

    // ─── Séquence intro complète ──────────────────────────────────────────
    IEnumerator PlayIntro()
    {
        // Init
        circleBG.localScale = Vector3.one * circleStartScale;
        logoGroup.localScale = new Vector3(logoStartScale.x, logoStartScale.y, 1f);
        logoGroup.anchoredPosition = new Vector2(logoGroup.anchoredPosition.x, logoStartY);

        txtOne.gameObject.SetActive(false);
        txtShot.gameObject.SetActive(false);
        txtLeft.gameObject.SetActive(false);
        txtCountdown.gameObject.SetActive(true);
        txtCountdown.text = "";

        string[] numbers = { "TEN", "NINE", "EIGHT", "SEVEN", "SIX", "FIVE", "FOUR", "THREE", "TWO", "ONE" };

        // ── PHASE 1 : Décompte TEN → ONE ──────────────────────────────────
        float duration = firstNumberDuration;
        for (int i = 0; i < numbers.Length; i++)
        {
            txtCountdown.text = numbers[i];
            yield return new WaitForSeconds(duration);
            duration *= speedUpFactor;
        }

        // ── PHASE 2 : ONE, SHOT, LEFT apparaissent ────────────────────────
        txtCountdown.gameObject.SetActive(false);
        txtOne.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.15f);

        txtShot.gameObject.SetActive(true);
        yield return StartCoroutine(ShakeText(txtShot, shakeDuration, shakeIntensity));
        yield return new WaitForSeconds(0.1f);

        txtLeft.gameObject.SetActive(true);
        yield return StartCoroutine(ShakeText(txtLeft, shakeDuration, shakeIntensity));
        yield return new WaitForSeconds(0.4f);

       

        // ── PHASE 4 : Barre de progression ────────────────────────────────
        StopAllCoroutines();
        introPanel.SetActive(false);
        progressPanel.SetActive(true);
    }

    // ─── Zoom out cercle + logo ensemble ─────────────────────────────────
    IEnumerator ZoomOutCircleWithLogo()
    {
        float elapsed = 0f;

        while (elapsed < circleZoomOutDur)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / circleZoomOutDur);
            t = t * t;

            float sc = Mathf.Lerp(circleStartScale, 0f, t);
            float sx = Mathf.Lerp(logoStartScale.x, 0f, t);
            float sy = Mathf.Lerp(logoStartScale.y, 0f, t);

            circleBG.localScale = Vector3.one * sc;
            logoGroup.localScale = new Vector3(sx, sy, 1f);
            yield return null;
        }

        circleBG.localScale = Vector3.zero;
        logoGroup.localScale = Vector3.zero;
    }

    

    // ─── Tremblement ponctuel ─────────────────────────────────────────────
    IEnumerator ShakeText(TextMeshProUGUI txt, float dur, float intensity)
    {
        Vector3 origin = txt.rectTransform.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            txt.rectTransform.anchoredPosition = origin + new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity), 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        txt.rectTransform.anchoredPosition = origin;
    }
}