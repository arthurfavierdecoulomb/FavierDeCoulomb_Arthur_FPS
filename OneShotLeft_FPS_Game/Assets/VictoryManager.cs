using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class VictoryScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject victoryScreenPanel;
    public Image darkOverlay;
    public TextMeshProUGUI mainTitleText;
    public TextMeshProUGUI motivationalText;
    public Button replayButton;
    public Button quitButton;

    [Header("Game UI to Hide")]
    public GameObject gameUIPanel;

    [Header("Animation Settings")]
    [SerializeField] private float titleShakeDuration = 0.4f;
    [SerializeField] private float titleShakeIntensity = 10f;
    [SerializeField] private float slideDuration = 0.7f;
    [SerializeField] private float slideDistance = 200f;
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("Final Positions (Y)")]
    [SerializeField] private float titleFinalYPosition = 150f;
    [SerializeField] private float motivationalFinalYPosition = 0f;
    [SerializeField] private float buttonFinalYPosition = -130f;

    [Header("Boutons - Alignement horizontal")]
    [SerializeField] private float buttonSpacing = 120f;

    private string[] victoryTitles =
    {
        "Victoire !",
        "Mission accomplie !",
        "Bravo !",
        "Succès total !",
        "Objectif atteint !"
    };

    private string[] victoryMessages =
    {
        "Les zombies ne s'en remettront pas.",
        "Franchement... c'était propre.",
        "Mission réussie chef.",
        "Ils n'ont rien compris à ce qui leur est arrivé.",
        "C'était presque trop facile.",
        "On peut dire que tu as géré."
    };

    [Header("Sons")]
    [Tooltip("Son d'impact joué quand le titre apparaît")]
    [SerializeField] private AudioClip titleImpactSound;
    [Tooltip("Bip joué à chaque lettre du typewriter")]
    [SerializeField] private AudioClip typewriterTickSound;
    [SerializeField][Range(0f, 1f)] private float titleSoundVolume = 1f;
    [SerializeField][Range(0f, 1f)] private float typewriterVolume = 0.4f;

    private AudioSource audioSource;
    private string currentMessage = "";

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        HideVictoryScreen();

        if (replayButton != null) replayButton.onClick.AddListener(OnReplayClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ShowVictoryScreen()
    {
        if (victoryScreenPanel == null) return;

        StopAllCoroutines();

        currentMessage = victoryMessages[Random.Range(0, victoryMessages.Length)];

        ResetUIElements();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        victoryScreenPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        if (mainTitleText != null)
        {
            mainTitleText.text = victoryTitles[Random.Range(0, victoryTitles.Length)];
            mainTitleText.rectTransform.anchoredPosition = Vector2.zero;
        }

        if (motivationalText != null)
        {
            motivationalText.text = "";
            motivationalText.gameObject.SetActive(false);
        }

        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);

        StartCoroutine(VictoryScreenAnimation());
    }

    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator VictoryScreenAnimation()
    {
        // PHASE 1 : Tremblement du titre
        if (mainTitleText != null)
        {
            PlaySound(titleImpactSound, titleSoundVolume); // impact au moment où le titre apparaît
            float elapsed = 0f;
            Vector3 center = Vector2.zero;
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
            Vector3 startPos = Vector2.zero;
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

            // Typewriter
            motivationalText.text = "";
            foreach (char c in currentMessage)
            {
                motivationalText.text += c;
                if (c != ' ') // pas de bip sur les espaces
                    PlaySound(typewriterTickSound, typewriterVolume);
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // PHASE 4 : Boutons slide + fade
        if (replayButton != null) replayButton.gameObject.SetActive(true);
        if (quitButton != null) quitButton.gameObject.SetActive(true);

        Vector3 rStart = new Vector2(-buttonSpacing, buttonFinalYPosition - slideDistance);
        Vector3 rEnd = new Vector2(-buttonSpacing, buttonFinalYPosition);
        Vector3 qStart = new Vector2(buttonSpacing, buttonFinalYPosition - slideDistance);
        Vector3 qEnd = new Vector2(buttonSpacing, buttonFinalYPosition);

        CanvasGroup rCG = replayButton != null ? (replayButton.GetComponent<CanvasGroup>() ?? replayButton.gameObject.AddComponent<CanvasGroup>()) : null;
        CanvasGroup qCG = quitButton != null ? (quitButton.GetComponent<CanvasGroup>() ?? quitButton.gameObject.AddComponent<CanvasGroup>()) : null;
        if (rCG != null) rCG.alpha = 0f;
        if (qCG != null) qCG.alpha = 0f;

        float btn = 0f;
        while (btn < slideDuration)
        {
            btn += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(btn / slideDuration), 3f);
            if (replayButton != null) { replayButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(rStart, rEnd, t); if (rCG != null) rCG.alpha = t; }
            if (quitButton != null) { quitButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(qStart, qEnd, t); if (qCG != null) qCG.alpha = t; }
            yield return null;
        }
        if (replayButton != null) { replayButton.GetComponent<RectTransform>().anchoredPosition = rEnd; if (rCG != null) rCG.alpha = 1f; }
        if (quitButton != null) { quitButton.GetComponent<RectTransform>().anchoredPosition = qEnd; if (qCG != null) qCG.alpha = 1f; }
    }

    // ─────────────────────────────────────────────────────────────────────
    public void HideVictoryScreen()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        StopAllCoroutines();

        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (motivationalText != null) motivationalText.gameObject.SetActive(false);
        if (mainTitleText != null) mainTitleText.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);
        if (victoryScreenPanel != null) victoryScreenPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    private void ResetUIElements()
    {
        if (mainTitleText != null)
        {
            mainTitleText.gameObject.SetActive(true);
            mainTitleText.rectTransform.anchoredPosition = Vector2.zero;
        }
        if (motivationalText != null)
        {
            motivationalText.gameObject.SetActive(false);
            motivationalText.text = "";
        }
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        // DEBUG : appuie sur V pour tester l'écran de victoire
        if (Input.GetKeyDown(KeyCode.V))
            ShowVictoryScreen();
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnReplayClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}