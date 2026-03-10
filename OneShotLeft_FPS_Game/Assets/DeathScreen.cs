using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject deathScreenPanel;
    public Image darkOverlay;
    public TextMeshProUGUI mainTitleText;
    public TextMeshProUGUI motivationalText;
    public Button respawnButton;
    public Button quitButton;

    [Header("Game UI to Hide")]
    public GameObject gameUIPanel;

    [Header("Player Reference")]
    private PlayerHealth playerHealth;

    [Header("Map")]
    [Tooltip("Glisse ici le GameObject qui contient le MapGenerator")]
    [SerializeField] private MapGenerator mapGenerator;

    [Header("Animation Settings")]
    [SerializeField] private float titleShakeDuration = 0.4f;
    [SerializeField] private float titleShakeIntensity = 15f;
    [SerializeField] private float slideDuration = 0.7f;
    [SerializeField] private float slideDistance = 200f;
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("Final Positions (Y)")]
    [SerializeField] private float titleFinalYPosition = 150f;
    [SerializeField] private float motivationalFinalYPosition = 0f;
    [SerializeField] private float buttonFinalYPosition = -130f;

    [Header("Boutons - Alignement horizontal")]
    [SerializeField] private float buttonSpacing = 120f;

    [Header("Sons")]
    [Tooltip("Son d'impact joué quand le titre apparaît")]
    [SerializeField] private AudioClip titleImpactSound;
    [Tooltip("Bip joué à chaque lettre du typewriter")]
    [SerializeField] private AudioClip typewriterTickSound;
    [SerializeField][Range(0f, 1f)] private float titleSoundVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float typewriterVolume = 0.4f;

    private AudioSource audioSource;

    [Header("Messages aléatoires")]
    private string[] deathTitles = new string[]
    {
        "Dommage...", "Tu es mort !", "Mort subite !", "Oof...",
        "Adieu.", "Echec.", "Au-revoir..."
    };

    private string[] motivationalMessages = new string[]
    {
        "Chef, il est mort comme une merde",
        "Tu aurais pu faire mieux...",
        "Je m'y attendais pas",
        "Pathétique, non je rigole...",
        "L'intention y est, c'est déjà ça !",
        "Nooon, pas toi, pas aujourd'hui ! Pas après tout ce que tu as fait",
        "Je mettrais une étoile pour l'effort... les quatre autres ? Bah...",
        "Je mettrais pas ça sur ton rapport de performance.",
        "Je dirais que tu as fait de ton mieux, mais je mentirais.",
        "Bruh",
        "Même un tutoriel n'aurait pas pu te sauver là...",
        "J'ai vu des plantes en pot avec plus de réflexes que toi.",
        "Ta grand-mère jouerait mieux... et elle ne sait même pas ce qu'est un ordinateur.",
        "Les zombies vont raconter cette blague pendant des années !",
        "Tu t'es fait éliminer par un zombie qui n'avait même pas de cerveau... ironique, non ?",
        "Félicitations ! Tu as débloqué l'achievement : 'Comment mourir en 5 secondes'",
        "Même les PNJ se moquent de toi en coulisses.",
        "Les zombies t'ont remercié pour ce repas gratuit.",
        "Waouh... juste... waouh. Aucun mot.",
        "Ta mort était si rapide que j'ai même pas eu le temps de préparer du pop-corn.",
        "Tu as réussi à transformer une victoire facile en défaite catastrophique.",
        "Bravo, tu as réussi à perdre dans un tutoriel."
    };

    private string currentMessage = "";

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        // Active le panel d'abord pour pouvoir accéder aux enfants
        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);

        // Cache tous les enfants proprement
        if (respawnButton != null) respawnButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (motivationalText != null) motivationalText.gameObject.SetActive(false);
        if (mainTitleText != null) mainTitleText.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);

        // Cache le panel entier APRÈS avoir géré les enfants
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);

        if (gameUIPanel != null) gameUIPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (respawnButton != null) respawnButton.onClick.AddListener(OnRespawnClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
            Debug.LogWarning("PlayerHealth non trouvé dans la scène !");

        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<MapGenerator>();
        if (mapGenerator == null)
            Debug.LogWarning("MapGenerator non trouvé dans la scène !");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ShowDeathScreen()
    {
        if (deathScreenPanel == null) return;

        StopAllCoroutines();

        // Active le panel EN PREMIER pour que les enfants soient accessibles
        deathScreenPanel.SetActive(true);

        // Force la réactivation de tous les enfants — corrige le bug où
        // SetActive(false) sur le panel parent bloque les enfants
        foreach (Transform child in deathScreenPanel.transform)
            child.gameObject.SetActive(true);

        string chosenTitle = deathTitles[Random.Range(0, deathTitles.Length)];
        currentMessage = motivationalMessages[Random.Range(0, motivationalMessages.Length)];

        // Puis reset les enfants
        ResetUIElements();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        if (mainTitleText != null)
        {
            mainTitleText.text = chosenTitle;
            mainTitleText.rectTransform.anchoredPosition = new Vector2(0, 0);
        }

        if (motivationalText != null)
        {
            motivationalText.text = "";
            motivationalText.gameObject.SetActive(false);
        }

        if (respawnButton != null) respawnButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);

        StartCoroutine(DeathScreenAnimation());
    }

    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator DeathScreenAnimation()
    {
        // PHASE 1 : Tremblement du titre
        if (mainTitleText != null)
        {
            PlaySound(titleImpactSound, titleSoundVolume);
            float elapsed = 0f;
            Vector3 center = new Vector2(0, 0);
            while (elapsed < titleShakeDuration)
            {
                float ox = Random.Range(-titleShakeIntensity, titleShakeIntensity);
                float oy = Random.Range(-titleShakeIntensity, titleShakeIntensity);
                mainTitleText.rectTransform.anchoredPosition = center + new Vector3(ox, oy, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            mainTitleText.rectTransform.anchoredPosition = center;
        }

        yield return new WaitForSeconds(0.15f);

        // PHASE 2 : Titre glisse vers le haut
        if (mainTitleText != null)
        {
            float elapsed = 0f;
            Vector3 startPos = new Vector2(0, 0);
            Vector3 endPos = new Vector2(0, titleFinalYPosition);
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideDuration), 3f);
                mainTitleText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            mainTitleText.rectTransform.anchoredPosition = endPos;
        }

        yield return new WaitForSeconds(0.1f);

        // PHASE 3 : Texte motivationnel slide + fade + typewriter
        if (motivationalText != null)
        {
            motivationalText.gameObject.SetActive(true);
            float elapsed = 0f;
            Vector3 startPos = new Vector2(0, motivationalFinalYPosition - slideDistance);
            Vector3 endPos = new Vector2(0, motivationalFinalYPosition);
            Color col = motivationalText.color;
            col.a = 0f;
            motivationalText.color = col;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideDuration), 3f);
                motivationalText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                col.a = t;
                motivationalText.color = col;
                yield return null;
            }
            motivationalText.rectTransform.anchoredPosition = endPos;
            col.a = 1f;
            motivationalText.color = col;

            motivationalText.text = "";
            foreach (char c in currentMessage)
            {
                motivationalText.text += c;
                if (c != ' ')
                    PlaySound(typewriterTickSound, typewriterVolume);
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // PHASE 4 : Boutons
        if (respawnButton != null) respawnButton.gameObject.SetActive(true);
        if (quitButton != null) quitButton.gameObject.SetActive(true);

        Vector3 rStart = new Vector2(-buttonSpacing, buttonFinalYPosition - slideDistance);
        Vector3 rEnd = new Vector2(-buttonSpacing, buttonFinalYPosition);
        Vector3 qStart = new Vector2(buttonSpacing, buttonFinalYPosition - slideDistance);
        Vector3 qEnd = new Vector2(buttonSpacing, buttonFinalYPosition);

        CanvasGroup rCG = respawnButton?.GetComponent<CanvasGroup>() ?? respawnButton?.gameObject.AddComponent<CanvasGroup>();
        CanvasGroup qCG = quitButton?.GetComponent<CanvasGroup>() ?? quitButton?.gameObject.AddComponent<CanvasGroup>();
        if (rCG != null) rCG.alpha = 0f;
        if (qCG != null) qCG.alpha = 0f;

        float btn = 0f;
        while (btn < slideDuration)
        {
            btn += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(btn / slideDuration), 3f);
            respawnButton?.GetComponent<RectTransform>().SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 0, 0);
            if (respawnButton != null) { respawnButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(rStart, rEnd, t); if (rCG != null) rCG.alpha = t; }
            if (quitButton != null) { quitButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(qStart, qEnd, t); if (qCG != null) qCG.alpha = t; }
            yield return null;
        }
        if (respawnButton != null) { respawnButton.GetComponent<RectTransform>().anchoredPosition = rEnd; if (rCG != null) rCG.alpha = 1f; }
        if (quitButton != null) { quitButton.GetComponent<RectTransform>().anchoredPosition = qEnd; if (qCG != null) qCG.alpha = 1f; }
    }

    // ─────────────────────────────────────────────────────────────────────
    void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    // ─────────────────────────────────────────────────────────────────────
    public void HideDeathScreen()
    {
        StopAllCoroutines();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Active le panel pour accéder aux enfants
        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);

        if (respawnButton != null) respawnButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (motivationalText != null) motivationalText.gameObject.SetActive(false);
        if (mainTitleText != null) mainTitleText.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);

        // Cache le panel entier après
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);

        if (gameUIPanel != null) gameUIPanel.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    private void ResetUIElements()
    {
        if (mainTitleText != null) { mainTitleText.gameObject.SetActive(true); mainTitleText.rectTransform.anchoredPosition = Vector2.zero; }
        if (motivationalText != null) { motivationalText.gameObject.SetActive(false); motivationalText.text = ""; }
        if (respawnButton != null) respawnButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnRespawnClicked() => RegenerateMapAndRespawn();

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RegenerateMapAndRespawn()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}