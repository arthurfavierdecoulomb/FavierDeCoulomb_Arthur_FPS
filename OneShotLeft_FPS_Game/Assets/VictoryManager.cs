using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// Gère l'écran de victoire : animation séquencée en 4 phases (shake, slide titre,
// typewriter message, apparition boutons), sons d'impact et messages aléatoires.
public class VictoryScreen : MonoBehaviour
{
    // ─── Références UI ────────────────────────────────────────────────────

    [Header("UI References")]

    // Panneau racine de l'écran de victoire, activé/désactivé selon l'état du jeu.
    public GameObject victoryScreenPanel;

    // Overlay sombre affiché derrière les éléments pour assombrir le fond de jeu.
    public Image darkOverlay;

    // Texte principal affiché au centre : titre de victoire choisi aléatoirement.
    public TextMeshProUGUI mainTitleText;

    // Texte secondaire affiché en typewriter : message humoristique choisi aléatoirement.
    public TextMeshProUGUI motivationalText;

    // Bouton permettant de relancer la scène courante.
    public Button replayButton;

    // Bouton permettant de quitter le jeu (ou stopper le play mode en Editor).
    public Button quitButton;


    // ─── UI de jeu à masquer ──────────────────────────────────────────────

    [Header("Game UI to Hide")]

    // Panneau d'UI de jeu (HUD, stamina, etc.) masqué pendant l'écran de victoire.
    public GameObject gameUIPanel;


    // ─── Animation Settings ───────────────────────────────────────────────

    [Header("Animation Settings")]

    // Durée en secondes du tremblement initial du titre.
    [SerializeField] private float titleShakeDuration = 0.4f;

    // Amplitude en pixels du tremblement du titre.
    [SerializeField] private float titleShakeIntensity = 10f;

    // Durée en secondes des animations de slide (titre, message, boutons).
    [SerializeField] private float slideDuration = 0.7f;

    // Distance en pixels parcourue lors du slide d'entrée (bas → position finale).
    [SerializeField] private float slideDistance = 200f;

    // Délai en secondes entre chaque caractère du typewriter.
    [SerializeField] private float typewriterSpeed = 0.03f;


    // ─── Positions finales (Y) ────────────────────────────────────────────

    [Header("Final Positions (Y)")]

    // Position Y ancré finale du titre après son slide vers le haut.
    [SerializeField] private float titleFinalYPosition = 150f;

    // Position Y ancré finale du texte motivationnel.
    [SerializeField] private float motivationalFinalYPosition = 0f;

    // Position Y ancré finale des boutons.
    [SerializeField] private float buttonFinalYPosition = -130f;


    // ─── Alignement des boutons ───────────────────────────────────────────

    [Header("Boutons - Alignement horizontal")]

    // Décalage horizontal en pixels entre le centre et chaque bouton (symétrique).
    [SerializeField] private float buttonSpacing = 120f;


    // ─── Messages aléatoires ──────────────────────────────────────────────

    // Titres de victoire tirés aléatoirement à chaque affichage.
    private string[] victoryTitles =
    {
        "Victoire !",
        "Mission accomplie !",
        "Bravo chef !",
        "Succès total !",
        "Objectif atteint !",
        "C'est un saint homme !",
        "GG !",
        "J'aurai pas fait mieux."
    };

    // Messages humoristiques affichés en typewriter, tirés aléatoirement.
    private string[] victoryMessages =
    {
        "Les zombies ne s'en remettront pas.",
        "Franchement... c'était propre.",
        "Mission réussie chef.",
        "Ils n'ont rien compris à ce qui leur est arrivé.",
        "C'était presque trop facile.",
        "On peut dire que tu as géré.",
        "On parle deja de toi dans les bars du coin.",
        "J'aurai pas parié sur toi, mais tu m'as prouvé le contraire.",
        "T'es chaud quand même, pas vrai ?",
        "Oh grand maitre, appelle moi ton disciple.",
        "Je m'y attendais pas, mais je suis pas surpris.",
        "Que dire face à une telle performance ?...",
        "J'ai surment parlé trop vite, navré que veux tu...",
        "J'ai tout dit au developpeur, il va devoir te nerf maintenant.",
        "Les zombies ont du mal à se remettre de cette défaite, tu sais ?",
        "Pouah, c'est eux qui ont glissé !",
        "Ouais... pas mal, pas mal...",
        "Easy les 5 vagues, hein ?",
        "Dit moi, les 5 vagues, pour toi... C'est des vagues ou des petites clapotis ?",
    };


    // ─── Sons ─────────────────────────────────────────────────────────────

    [Header("Sons")]

    // Son d'impact joué au moment exact où le titre apparaît à l'écran.
    [Tooltip("Son d'impact joué quand le titre apparaît")]
    [SerializeField] private AudioClip titleImpactSound;

    // Bip joué à chaque lettre affichée par le typewriter (sauf les espaces).
    [Tooltip("Bip joué à chaque lettre du typewriter")]
    [SerializeField] private AudioClip typewriterTickSound;

    // Volume du son d'impact du titre.
    [SerializeField][Range(0f, 1f)] private float titleSoundVolume = 1f;

    // Volume du bip typewriter.
    [SerializeField][Range(0f, 1f)] private float typewriterVolume = 0.4f;


    // ─── Variables privées ────────────────────────────────────────────────

    // Source audio 2D créée dynamiquement pour tous les sons de l'écran de victoire.
    private AudioSource audioSource;

    // Message motivationnel sélectionné aléatoirement et stocké avant l'animation.
    private string currentMessage = "";


    // ─── Initialisation ───────────────────────────────────────────────────

    void Start()
    {
        // Cache l'écran de victoire au démarrage — il ne doit apparaître qu'en fin de partie.
        HideVictoryScreen();

        // Enregistre les callbacks des boutons une seule fois à l'initialisation.
        if (replayButton != null) replayButton.onClick.AddListener(OnReplayClicked);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

        // Récupère ou crée la source audio 2D (interface, pas de spatialisation).
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }


    // ─── Affichage de l'écran de victoire ────────────────────────────────

    public void ShowVictoryScreen()
    {
        if (victoryScreenPanel == null) return;

        // Arrête toute animation en cours avant d'en lancer une nouvelle.
        StopAllCoroutines();

        // Choisit le message motivationnel aléatoirement et le mémorise pour le typewriter.
        currentMessage = victoryMessages[Random.Range(0, victoryMessages.Length)];

        // Remet tous les éléments UI dans leur état initial avant l'animation.
        ResetUIElements();

        // Libère le curseur pour permettre l'interaction avec les boutons.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Affiche le panneau de victoire et masque le HUD de jeu.
        victoryScreenPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);

        // Choisit un titre de victoire aléatoire et le positionne au centre.
        if (mainTitleText != null)
        {
            mainTitleText.text = victoryTitles[Random.Range(0, victoryTitles.Length)];
            mainTitleText.rectTransform.anchoredPosition = Vector2.zero;
        }

        // Cache le texte motivationnel — il sera révélé pendant l'animation.
        if (motivationalText != null)
        {
            motivationalText.text = "";
            motivationalText.gameObject.SetActive(false);
        }

        // Cache les boutons — ils apparaîtront en dernière phase de l'animation.
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);

        // Lance la séquence d'animation complète.
        StartCoroutine(VictoryScreenAnimation());
    }


    // ─── Séquence d'animation complète ───────────────────────────────────

    private IEnumerator VictoryScreenAnimation()
    {
        // ── PHASE 1 : Tremblement du titre ────────────────────────────────

        if (mainTitleText != null)
        {
            // Joue le son d'impact au moment exact où le titre apparaît.
            PlaySound(titleImpactSound, titleSoundVolume);

            float elapsed = 0f;
            Vector3 center = Vector2.zero;

            while (elapsed < titleShakeDuration)
            {
                // Déplace le titre aléatoirement autour du centre à chaque frame.
                float ox = Random.Range(-titleShakeIntensity, titleShakeIntensity);
                float oy = Random.Range(-titleShakeIntensity, titleShakeIntensity);
                mainTitleText.rectTransform.anchoredPosition = center + new Vector3(ox, oy, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Remet le titre exactement au centre après le shake.
            mainTitleText.rectTransform.anchoredPosition = center;
        }

        yield return new WaitForSeconds(0.15f);

        // ── PHASE 2 : Titre glisse vers le haut ───────────────────────────

        if (mainTitleText != null)
        {
            float elapsed = 0f;
            Vector3 startPos = Vector2.zero;
            Vector3 endPos = new Vector2(0, titleFinalYPosition);

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;

                // Courbe ease-out cubique : rapide au départ, ralentit à l'arrivée.
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideDuration), 3f);
                mainTitleText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            // Force la position finale exacte pour éviter tout résidu d'arrondi.
            mainTitleText.rectTransform.anchoredPosition = endPos;
        }

        yield return new WaitForSeconds(0.1f);

        // ── PHASE 3 : Message motivationnel — slide + fade + typewriter ───

        if (motivationalText != null)
        {
            motivationalText.gameObject.SetActive(true);

            float elapsed = 0f;

            // Le texte entre par le bas : démarre slideDistance pixels sous sa position finale.
            Vector3 startPos = new Vector2(0, motivationalFinalYPosition - slideDistance);
            Vector3 endPos = new Vector2(0, motivationalFinalYPosition);

            // Initialise l'alpha à 0 pour le fade-in simultané au slide.
            Color col = motivationalText.color;
            col.a = 0f;
            motivationalText.color = col;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;

                // Même courbe ease-out cubique que le titre pour cohérence visuelle.
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideDuration), 3f);
                motivationalText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);

                // L'alpha suit le même t : slide et fade sont parfaitement synchronisés.
                col.a = t;
                motivationalText.color = col;
                yield return null;
            }

            // Force position et opacité finales exactes.
            motivationalText.rectTransform.anchoredPosition = endPos;
            col.a = 1f;
            motivationalText.color = col;

            // Typewriter : affiche le message caractère par caractère.
            motivationalText.text = "";
            foreach (char c in currentMessage)
            {
                motivationalText.text += c;

                // N'émet pas de bip sur les espaces pour un rendu sonore plus naturel.
                if (c != ' ')
                    PlaySound(typewriterTickSound, typewriterVolume);

                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // ── PHASE 4 : Boutons — slide + fade ─────────────────────────────

        if (replayButton != null) replayButton.gameObject.SetActive(true);
        if (quitButton != null) quitButton.gameObject.SetActive(true);

        // Les deux boutons partent symétriquement de part et d'autre du centre.
        Vector3 rStart = new Vector2(-buttonSpacing, buttonFinalYPosition - slideDistance);
        Vector3 rEnd = new Vector2(-buttonSpacing, buttonFinalYPosition);
        Vector3 qStart = new Vector2(buttonSpacing, buttonFinalYPosition - slideDistance);
        Vector3 qEnd = new Vector2(buttonSpacing, buttonFinalYPosition);

        // Récupère ou crée un CanvasGroup sur chaque bouton pour piloter leur alpha.
        CanvasGroup rCG = replayButton != null ? (replayButton.GetComponent<CanvasGroup>() ?? replayButton.gameObject.AddComponent<CanvasGroup>()) : null;
        CanvasGroup qCG = quitButton != null ? (quitButton.GetComponent<CanvasGroup>() ?? quitButton.gameObject.AddComponent<CanvasGroup>()) : null;

        // Démarre les deux boutons invisibles pour le fade-in.
        if (rCG != null) rCG.alpha = 0f;
        if (qCG != null) qCG.alpha = 0f;

        float btn = 0f;
        while (btn < slideDuration)
        {
            btn += Time.deltaTime;

            // Courbe ease-out cubique identique aux phases précédentes.
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(btn / slideDuration), 3f);

            // Slide et fade des deux boutons en parallèle dans la même boucle.
            if (replayButton != null) { replayButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(rStart, rEnd, t); if (rCG != null) rCG.alpha = t; }
            if (quitButton != null) { quitButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(qStart, qEnd, t); if (qCG != null) qCG.alpha = t; }
            yield return null;
        }

        // Force positions et opacités finales exactes pour les deux boutons.
        if (replayButton != null) { replayButton.GetComponent<RectTransform>().anchoredPosition = rEnd; if (rCG != null) rCG.alpha = 1f; }
        if (quitButton != null) { quitButton.GetComponent<RectTransform>().anchoredPosition = qEnd; if (qCG != null) qCG.alpha = 1f; }
    }


    // ─── Masquage de l'écran de victoire ─────────────────────────────────

    public void HideVictoryScreen()
    {
        // Reverrouille le curseur pour reprendre le contrôle FPS.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Stoppe toute animation en cours pour éviter des états intermédiaires.
        StopAllCoroutines();

        // Masque tous les éléments UI de l'écran de victoire.
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (motivationalText != null) motivationalText.gameObject.SetActive(false);
        if (mainTitleText != null) mainTitleText.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);
        if (victoryScreenPanel != null) victoryScreenPanel.SetActive(false);

        // Réaffiche le HUD de jeu.
        if (gameUIPanel != null) gameUIPanel.SetActive(true);
    }


    // ─── Réinitialisation des éléments UI ────────────────────────────────

    // Remet tous les éléments dans leur état de départ avant de lancer l'animation.
    // Garantit un affichage propre si ShowVictoryScreen() est appelé plusieurs fois.
    private void ResetUIElements()
    {
        if (mainTitleText != null)
        {
            mainTitleText.gameObject.SetActive(true);

            // Centre le titre à l'origine avant le shake de la phase 1.
            mainTitleText.rectTransform.anchoredPosition = Vector2.zero;
        }

        if (motivationalText != null)
        {
            // Cache le texte motivationnel : il sera révélé en phase 3.
            motivationalText.gameObject.SetActive(false);
            motivationalText.text = "";
        }

        // Cache les boutons : ils seront révélés en phase 4.
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);

        // Affiche l'overlay sombre dès le début pour assombrir le fond.
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(true);
    }


    // ─── Son ─────────────────────────────────────────────────────────────

    // Joue un clip audio en one-shot avec le volume spécifié.
    // Ne fait rien si la source ou le clip est manquant.
    void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }


    // ─── Debug ────────────────────────────────────────────────────────────

    void Update() // (Optionnel) Permet de tester l'écran de victoire en appuyant
                  // sur la touche V pendant le play mode. mais enlevé pour eviter
                  // les conflits avec d'autres inputs et pour respecter la consigne
                  // de ne pas inclure de code de test ou debug dans la version finale.
    {
        
    }


    // ─── Callbacks boutons ────────────────────────────────────────────────

    // Recharge la scène active depuis le début — équivalent d'un "Rejouer".
    void OnReplayClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // Quitte le jeu en build, ou stoppe le play mode dans l'Editor Unity.
    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}