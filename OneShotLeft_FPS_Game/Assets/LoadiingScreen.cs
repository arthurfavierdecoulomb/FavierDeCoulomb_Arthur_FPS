using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Gère l'écran de chargement : séquence d'intro animée avec décompte,
// barre de progression lissée et retour audio 2D.
public class LoadingScreen : MonoBehaviour
{
    // Singleton accessible globalement pour piloter l'écran depuis n'importe quel script.
    public static LoadingScreen Instance;


    // ─── Panels ───────────────────────────────────────────────────────────

    [Header("Panels")]

    // Panneau racine qui contient toute l'interface de chargement.
    [SerializeField] private GameObject loadingPanel;

    // Panneau affiché pendant la séquence d'intro animée.
    [SerializeField] private GameObject introPanel;

    // Panneau affiché pendant le chargement effectif (barre de progression).
    [SerializeField] private GameObject progressPanel;


    // ─── Menu ─────────────────────────────────────────────────────────────

    [Header("Menu")]

    // Panneau menu affiché après la fin de l'intro, avec le bouton Jouer.
    [SerializeField] private GameObject menuPanel;

    [Header("Bouton")]
    [SerializeField] private Button playButton;

    // Durée du fade-in du menu en secondes.
    [SerializeField] private float menuFadeInDuration = 0.5f;

    // CanvasGroup du menuPanel, utilisé pour le fade (créé automatiquement si absent).
    private CanvasGroup menuCanvasGroup;


    // ─── Intro – Cercle ───────────────────────────────────────────────────

    [Header("Intro – Cercle")]

    // Transform du cercle d'arrière-plan, animé en zoom-out au démarrage.
    [SerializeField] private RectTransform circleBG;


    // ─── Intro – Logo ─────────────────────────────────────────────────────

    [Header("Intro – Logo (parent LOGO)")]

    // Parent regroupant tous les éléments du logo (ONE SHOT LEFT + countdown).
    [SerializeField] private RectTransform logoGroup;


    // ─── Intro – Textes ───────────────────────────────────────────────────

    [Header("Intro – Textes (enfants de LOGO)")]

    // Texte affiché pendant le décompte (TEN → TWO).
    [SerializeField] private TextMeshProUGUI txtCountdown;

    // Texte "ONE" révélé après la fin du décompte.
    [SerializeField] private TextMeshProUGUI txtOne;

    // Texte "SHOT" révélé après ONE, avec effet de shake.
    [SerializeField] private TextMeshProUGUI txtShot;

    // Texte "LEFT" révélé après SHOT, avec effet de shake.
    [SerializeField] private TextMeshProUGUI txtLeft;


    // ─── Progress ─────────────────────────────────────────────────────────

    [Header("Progress")]

    // Image en mode Fill utilisée comme barre de progression.
    [SerializeField] private Image barreFill;

    // Texte affichant l'étape de chargement en cours.
    [SerializeField] private TextMeshProUGUI txtEtape;


    // ─── Tailles ──────────────────────────────────────────────────────────

    [Header("Tailles")]

    // Échelle initiale du cercle d'arrière-plan avant le zoom-out.
    [SerializeField] private float circleStartScale = 3f;

    // Échelle initiale du groupe logo (x, y) avant le zoom-out.
    [SerializeField] private Vector2 logoStartScale = Vector2.one;


    // ─── Timings ──────────────────────────────────────────────────────────

    [Header("Timings")]

    // Durée d'affichage du premier chiffre du décompte (TEN), en secondes.
    [SerializeField] private float firstNumberDuration = 0.55f;

    // Facteur multiplicatif appliqué à la durée à chaque chiffre (< 1 = accélération).
    [SerializeField] private float speedUpFactor = 0.82f;

    // Amplitude du tremblement en pixels lors du shake des textes SHOT et LEFT.
    [SerializeField] private float shakeIntensity = 8f;

    // Durée totale en secondes de chaque effet de shake.
    [SerializeField] private float shakeDuration = 0.2f;

    // Position Y de départ du groupe logo sur l'axe ancré.
    [SerializeField] private float logoStartY = 0f;

    // Durée de l'animation de zoom-out du cercle et du logo, en secondes.
    [SerializeField] private float circleZoomOutDur = 0.7f;

    // Vitesse de lissage (Lerp) de la barre de progression (unités par seconde).
    [SerializeField] private float smoothSpeed = 5f;


    // ─── Sons ─────────────────────────────────────────────────────────────

    [Header("Sons")]

    // Bip joué à chaque chiffre du décompte (TEN → TWO).
    [Tooltip("Bip joué à chaque chiffre du countdown (TEN → TWO)")]
    [SerializeField] private AudioClip tickSound;

    // Son d'impact joué lorsque "ONE" apparaît à l'écran.
    [Tooltip("Son d'impact quand ONE apparaît")]
    [SerializeField] private AudioClip oneSound;

    // Whoosh ou impact joué lorsque "SHOT" apparaît à l'écran.
    [Tooltip("Whoosh/impact quand SHOT apparaît")]
    [SerializeField] private AudioClip shotSound;

    // Whoosh ou impact joué lorsque "LEFT" apparaît à l'écran.
    [Tooltip("Whoosh/impact quand LEFT apparaît")]
    [SerializeField] private AudioClip leftSound;

    // Volume global appliqué à tous les sons de l'intro (0 = muet, 1 = plein volume).
    [SerializeField][Range(0f, 1f)] private float introVolume = 1f;


    // ─── Références privées ───────────────────────────────────────────────

    // Source audio 2D utilisée pour tous les effets sonores de l'intro.
    private AudioSource audioSource;

    // Valeur cible de la progression (mise à jour par SetProgress).
    private float targetProgress = 0f;

    // Valeur affichée de la progression, lissée vers targetProgress via Lerp.
    private float currentProgress = 0f;


    



    // ─── Initialisation ───────────────────────────────────────────────────

    void Awake()
    {
        // Enregistre ce composant comme instance singleton accessible globalement.
        Instance = this;

        // Masque le panneau de chargement au démarrage, il sera affiché via Show().
        loadingPanel.SetActive(false);

        // Récupère ou crée la source audio 2D (spatialBlend = 0 = son d'interface).
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        // Récupère ou crée le CanvasGroup pour le fade-in du menu.
        if (menuPanel != null)
        {
            menuCanvasGroup = menuPanel.GetComponent<CanvasGroup>();
            if (menuCanvasGroup == null)
                menuCanvasGroup = menuPanel.AddComponent<CanvasGroup>();

            // Le menu est caché au démarrage, il apparaîtra après l'intro.
            menuPanel.SetActive(false);
        }

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        else
            Debug.LogError("playButton non assigné !");
    }

    void Start()
    {
        StartCoroutine(Boot());
    }

    IEnumerator Boot()
    {
        Show();
        yield return StartCoroutine(WaitUntilReady());
        // WaitUntilReady() finit après le fade-in du menu.
        // Le bouton "Jouer" prend le relais via OnPlayButtonClicked().
    }

    void Update()
    {
        // Lisse la progression affichée vers la valeur cible à chaque frame.
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * smoothSpeed);

        // Met à jour le fillAmount de la barre si elle est assignée.
        if (barreFill != null)
            barreFill.fillAmount = currentProgress;
    }


    // ─── API publique ─────────────────────────────────────────────────────

    // Retourne true si le panneau de chargement est actuellement visible.
    public bool IsVisible => loadingPanel != null && loadingPanel.activeSelf;

    // Affiche l'écran de chargement et démarre avec le panneau d'intro.
    public void Show()
    {
        // Réinitialise la progression à zéro avant chaque nouvelle ouverture.
        currentProgress = 0f;
        targetProgress = 0f;

        loadingPanel.SetActive(true);
        progressPanel.SetActive(false);
        introPanel.SetActive(true);

        // S'assure que le menu est masqué à chaque nouvelle ouverture.
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    // Coroutine à attendre pour bloquer jusqu'à la fin de l'intro.
    public IEnumerator WaitUntilReady()
    {
        yield return StartCoroutine(PlayIntro());
    }

    // Masque l'intégralité de l'écran de chargement.
    public void Hide()
    {
        loadingPanel.SetActive(false);
    }

    // Met à jour la progression cible (0–1) et le texte d'étape affiché.
    public void SetProgress(float pct, string etape = "")
    {
        targetProgress = Mathf.Clamp01(pct);

        // N'écrase le texte que si une étape est explicitement fournie.
        if (txtEtape != null && etape != "")
            txtEtape.text = etape;
    }

    // ─── Bouton Jouer ─────────────────────────────────────────────────────

    // Appelé par le bouton "Jouer" du menu via l'Inspector (OnClick).
    // Masque le menu, affiche la barre de progression et lance la génération de map.
    public void OnPlayButtonClicked()
    {
        Debug.Log("OnPlayButtonClicked appelé");

        if (menuPanel != null)
            menuPanel.SetActive(false);
        else
            Debug.LogError("menuPanel est NULL !");

        if (progressPanel != null)
        {
            progressPanel.SetActive(true);
            Debug.Log("progressPanel activé");
        }
        else
            Debug.LogError("progressPanel est NULL !");

        if (MapGenerator.Instance != null)
        {
            Debug.Log("GenerateMap() appelé");
            MapGenerator.Instance.GenerateMap();
        }
        else
            Debug.LogError("MapGenerator.Instance est NULL !");
    }


    // ─── Séquence intro complète ──────────────────────────────────────────

    IEnumerator PlayIntro()
    {
        // Réinitialise les transforms du cercle et du logo à leurs valeurs de départ.
        circleBG.localScale = Vector3.one * circleStartScale;
        logoGroup.localScale = new Vector3(logoStartScale.x, logoStartScale.y, 1f);
        logoGroup.anchoredPosition = new Vector2(logoGroup.anchoredPosition.x, logoStartY);

        // Cache les textes finaux et affiche uniquement le compteur au départ.
        txtOne.gameObject.SetActive(false);
        txtShot.gameObject.SetActive(false);
        txtLeft.gameObject.SetActive(false);
        txtCountdown.gameObject.SetActive(true);
        txtCountdown.text = "";

        string[] numbers = { "TEN", "NINE", "EIGHT", "SEVEN", "SIX", "FIVE", "FOUR", "THREE", "TWO", "ONE" };

        // ── PHASE 1 : Décompte TEN → TWO avec bip ─────────────────────────

        float duration = firstNumberDuration;

        // S'arrête avant ONE (géré séparément en phase 2).
        for (int i = 0; i < numbers.Length - 1; i++)
        {
            txtCountdown.text = numbers[i];

            // Joue le bip associé à chaque chiffre.
            PlaySound(tickSound);

            yield return new WaitForSeconds(duration);

            // Accélère progressivement le décompte à chaque itération.
            duration *= speedUpFactor;
        }

        // ── PHASE 2 : ONE, SHOT, LEFT apparaissent ────────────────────────

        // Cache le compteur numérique avant de révéler les mots.
        txtCountdown.gameObject.SetActive(false);

        // Révèle "ONE" avec son son d'impact.
        txtOne.gameObject.SetActive(true);
        PlaySound(oneSound);
        yield return new WaitForSeconds(0.15f);

        // Révèle "SHOT" avec whoosh/impact et un tremblement visuel.
        txtShot.gameObject.SetActive(true);
        PlaySound(shotSound);
        yield return StartCoroutine(ShakeText(txtShot, shakeDuration, shakeIntensity));
        yield return new WaitForSeconds(0.1f);

        // Révèle "LEFT" avec whoosh/impact et un tremblement visuel.
        txtLeft.gameObject.SetActive(true);
        PlaySound(leftSound);
        yield return StartCoroutine(ShakeText(txtLeft, shakeDuration, shakeIntensity));
        yield return new WaitForSeconds(0.4f);

        // Laisse le temps aux sons de se terminer avant de passer à la suite.
        yield return new WaitForSeconds(3f);

        // ── PHASE 3 : Affichage du menu ───────────────────────────────────

        // NOTE : StopAllCoroutines() supprimé intentionnellement —
        // il tuait GenerateMapRoutine() qui tourne en parallèle dans MapGenerator.
        introPanel.SetActive(false);

        // Fade-in du menu après la fin de l'intro.
        yield return StartCoroutine(ShowMenu());
    }


    // ─── Fade-in du menu ─────────────────────────────────────────────────

    IEnumerator ShowMenu()
    {
        if (menuPanel == null || menuCanvasGroup == null)
        {
            Debug.LogWarning("LoadingScreen: menuPanel ou menuCanvasGroup non assigné !");
            yield break;
        }

        menuCanvasGroup.alpha = 0f;
        menuPanel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < menuFadeInDuration)
        {
            elapsed += Time.deltaTime;
            menuCanvasGroup.alpha = Mathf.Clamp01(elapsed / menuFadeInDuration);
            yield return null;
        }

        menuCanvasGroup.alpha = 1f;
    }


    // ─── Zoom out cercle + logo ensemble ─────────────────────────────────

    IEnumerator ZoomOutCircleWithLogo()
    {
        float elapsed = 0f;

        while (elapsed < circleZoomOutDur)
        {
            elapsed += Time.deltaTime;

            // t normalisé entre 0 et 1 sur la durée de l'animation.
            float t = Mathf.Clamp01(elapsed / circleZoomOutDur);

            // Courbe ease-in quadratique : démarre lentement, accélère vers la fin.
            t = t * t;

            // Applique le zoom-out simultanément sur le cercle et le logo.
            float sc = Mathf.Lerp(circleStartScale, 0f, t);
            float sx = Mathf.Lerp(logoStartScale.x, 0f, t);
            float sy = Mathf.Lerp(logoStartScale.y, 0f, t);

            circleBG.localScale = Vector3.one * sc;
            logoGroup.localScale = new Vector3(sx, sy, 1f);
            yield return null;
        }

        // Force les échelles à zéro exact pour éviter tout résidu d'arrondi flottant.
        circleBG.localScale = Vector3.zero;
        logoGroup.localScale = Vector3.zero;
    }


    // ─── Son ─────────────────────────────────────────────────────────────

    // Joue un clip audio en one-shot avec le volume d'intro défini dans l'Inspector.
    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, introVolume);
    }


    // ─── Tremblement ponctuel ─────────────────────────────────────────────

    // Fait trembler un texte autour de sa position d'origine pendant une durée donnée.
    IEnumerator ShakeText(TextMeshProUGUI txt, float dur, float intensity)
    {
        // Mémorise la position initiale pour y revenir proprement en fin de shake.
        Vector3 origin = txt.rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < dur)
        {
            // Déplace aléatoirement le texte dans un carré de ±intensity pixels.
            txt.rectTransform.anchoredPosition = origin + new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity), 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Remet le texte exactement à sa position d'origine après le shake.
        txt.rectTransform.anchoredPosition = origin;
    }
}