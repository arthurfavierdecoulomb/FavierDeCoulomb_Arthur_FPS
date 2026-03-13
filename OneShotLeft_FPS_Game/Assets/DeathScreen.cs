using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathScreen : MonoBehaviour                                            // Gère l'écran de mort animé : shake titre, slide, typewriter, boutons
{
    [Header("UI References")]
    public GameObject deathScreenPanel;                                             // Panel racine de l'écran de mort — activé/désactivé selon l'état
    public Image darkOverlay;                                                       // Fond sombre derrière les éléments UI
    public TextMeshProUGUI mainTitleText;                                           // Titre principal (ex: "Tu es mort !")
    public TextMeshProUGUI motivationalText;                                        // Message humoristique affiché en typewriter
    public Button respawnButton;                                                    // Bouton pour relancer la partie
    public Button quitButton;                                                       // Bouton pour quitter le jeu

    [Header("Game UI to Hide")]
    public GameObject gameUIPanel;                                                  // HUD de jeu à cacher pendant l'écran de mort

    [Header("Player Reference")]
    private PlayerHealth playerHealth;                                              // Référence au joueur — trouvée automatiquement dans la scène

    [Header("Map")]
    [Tooltip("Glisse ici le GameObject qui contient le MapGenerator")]
    [SerializeField] private MapGenerator mapGenerator;                            // Référence au générateur de map pour le respawn

    [Header("Animation Settings")]
    [Tooltip("Durée du tremblement du titre à l'apparition")]
    [SerializeField] private float titleShakeDuration = 0.4f;                   // Durée du shake initial du titre en secondes
    [Tooltip("Intensité du tremblement du titre")]
    [SerializeField] private float titleShakeIntensity = 15f;                    // Amplitude du shake en pixels
    [Tooltip("Durée du glissement des éléments UI")]
    [SerializeField] private float slideDuration = 0.7f;                   // Durée de chaque animation de slide
    [Tooltip("Distance de départ du slide en pixels")]
    [SerializeField] private float slideDistance = 200f;                   // Les éléments partent de cette distance en dessous de leur position finale
    [Tooltip("Vitesse du typewriter — délai entre chaque lettre")]
    [SerializeField] private float typewriterSpeed = 0.03f;                  // Plus faible = plus rapide

    [Header("Final Positions (Y)")]
    [Tooltip("Position Y finale du titre")]
    [SerializeField] private float titleFinalYPosition = 150f;             // Le titre remonte à cette position après le shake
    [Tooltip("Position Y finale du texte motivationnel")]
    [SerializeField] private float motivationalFinalYPosition = 0f;               // Position centrale du texte motivationnel
    [Tooltip("Position Y finale des boutons")]
    [SerializeField] private float buttonFinalYPosition = -130f;            // Les boutons apparaissent en bas de l'écran

    [Header("Boutons - Alignement horizontal")]
    [Tooltip("Espacement horizontal entre les deux boutons")]
    [SerializeField] private float buttonSpacing = 120f;                          // Distance entre le centre de chaque bouton

    [Header("Sons")]
    [Tooltip("Son d'impact joué quand le titre apparaît — idéalement un son percutant et court")]
    [SerializeField] private AudioClip titleImpactSound;                          // Son joué au début du shake du titre
    [Tooltip("Bip joué à chaque lettre du typewriter — idéalement un tick sec et discret")]
    [SerializeField] private AudioClip typewriterTickSound;                       // Son joué pour chaque lettre (ignoré sur les espaces)
    [SerializeField][Range(0f, 1f)] private float titleSoundVolume = 1f;        // Volume du son d'impact du titre
    [SerializeField][Range(0f, 1f)] private float typewriterVolume = 0.4f;      // Volume des ticks typewriter — plus faible pour rester discret

    private AudioSource audioSource;                                               // AudioSource pour jouer les sons de l'écran de mort

    [Header("Messages aléatoires")]
    private string[] deathTitles = new string[]                                   // Liste de titres tirés aléatoirement à chaque mort
    {
        "Dommage...", "Tu es mort !", "Mort subite !", "Oof...",
        "Adieu.", "Echec.", "Au-revoir..."
    };

    private string[] motivationalMessages = new string[]                          // Liste de messages humoristiques affichés en typewriter
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

    private string currentMessage = "";                                            // Message motivationnel sélectionné pour cette mort

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);            // Active le panel pour pouvoir accéder aux enfants

        if (respawnButton != null) respawnButton.gameObject.SetActive(false);   // Cache les boutons au démarrage
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (motivationalText != null) motivationalText.gameObject.SetActive(false); // Cache le texte motivationnel
        if (mainTitleText != null) mainTitleText.gameObject.SetActive(false);   // Cache le titre
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);     // Cache le fond sombre

        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);           // Cache le panel entier APRÈS avoir géré les enfants

        if (gameUIPanel != null) gameUIPanel.SetActive(true);                      // S'assure que le HUD de jeu est visible au démarrage
        Cursor.lockState = CursorLockMode.Locked;                                  // Verrouille le curseur en mode jeu
        Cursor.visible = false;

        if (respawnButton != null) respawnButton.onClick.AddListener(OnRespawnClicked); // Abonne le bouton respawn à sa méthode
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);       // Abonne le bouton quitter à sa méthode

        playerHealth = FindFirstObjectByType<PlayerHealth>();                      // Trouve le PlayerHealth automatiquement dans la scène
        if (playerHealth == null)
            Debug.LogWarning("PlayerHealth non trouvé dans la scène !");

        if (mapGenerator == null)
            mapGenerator = FindFirstObjectByType<MapGenerator>();                  // Trouve le MapGenerator automatiquement si non assigné
        if (mapGenerator == null)
            Debug.LogWarning("MapGenerator non trouvé dans la scène !");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();                  // Crée l'AudioSource dynamiquement si absente
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;                                             // Son 2D — entendu partout sans atténuation spatiale
    }

    // ─────────────────────────────────────────────────────────────────────
    public void ShowDeathScreen()
    {
        if (deathScreenPanel == null) return;                                      // Sécurité : ne fait rien si le panel n'est pas assigné

        StopAllCoroutines();                                                       // Stoppe toute animation en cours avant d'en lancer une nouvelle

        deathScreenPanel.SetActive(true);                                          // Active le panel EN PREMIER pour que les enfants soient accessibles

        foreach (Transform child in deathScreenPanel.transform)
            child.gameObject.SetActive(true);                                      // Force la réactivation des enfants — corrige le bug du panel parent désactivé

        string chosenTitle = deathTitles[Random.Range(0, deathTitles.Length)];    // Titre aléatoire pour cette mort
        currentMessage = motivationalMessages[Random.Range(0, motivationalMessages.Length)]; // Message humoristique aléatoire

        ResetUIElements();                                                         // Remet tous les éléments à leur état initial avant l'animation

        Cursor.lockState = CursorLockMode.None;                                   // Déverrouille le curseur pour les boutons
        Cursor.visible = true;

        if (gameUIPanel != null) gameUIPanel.SetActive(false);                    // Cache le HUD de jeu pendant l'écran de mort

        if (mainTitleText != null)
        {
            mainTitleText.text = chosenTitle;                                      // Assigne le titre choisi
            mainTitleText.rectTransform.anchoredPosition = new Vector2(0, 0);     // Remet le titre au centre pour le shake
        }

        if (motivationalText != null)
        {
            motivationalText.text = "";                                            // Vide le texte avant le typewriter
            motivationalText.gameObject.SetActive(false);                         // Caché jusqu'à la phase 3
        }

        if (respawnButton != null) respawnButton.gameObject.SetActive(false);     // Cachés jusqu'à la phase 4
        if (quitButton != null) quitButton.gameObject.SetActive(false);

        StartCoroutine(DeathScreenAnimation());                                    // Lance la séquence d'animation complète
    }

    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator DeathScreenAnimation()
    {
        // PHASE 1 : Tremblement du titre
        if (mainTitleText != null)
        {
            PlaySound(titleImpactSound, titleSoundVolume);                         // Son d'impact au début du shake
            float elapsed = 0f;
            Vector3 center = new Vector2(0, 0);
            while (elapsed < titleShakeDuration)
            {
                float ox = Random.Range(-titleShakeIntensity, titleShakeIntensity); // Offset horizontal aléatoire
                float oy = Random.Range(-titleShakeIntensity, titleShakeIntensity); // Offset vertical aléatoire
                mainTitleText.rectTransform.anchoredPosition = center + new Vector3(ox, oy, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            mainTitleText.rectTransform.anchoredPosition = center;                // Recentre le titre après le shake
        }

        yield return new WaitForSeconds(0.15f);                                   // Pause entre le shake et le slide

        // PHASE 2 : Titre glisse vers le haut
        if (mainTitleText != null)
        {
            float elapsed = 0f;
            Vector3 startPos = new Vector2(0, 0);
            Vector3 endPos = new Vector2(0, titleFinalYPosition);
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideDuration), 3f); // Easing cubic out — décélère à l'arrivée
                mainTitleText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            mainTitleText.rectTransform.anchoredPosition = endPos;                // Force la position finale
        }

        yield return new WaitForSeconds(0.1f);

        // PHASE 3 : Texte motivationnel slide + fade + typewriter
        if (motivationalText != null)
        {
            motivationalText.gameObject.SetActive(true);                          // Affiche le texte pour l'animer
            float elapsed = 0f;
            Vector3 startPos = new Vector2(0, motivationalFinalYPosition - slideDistance); // Démarre en bas
            Vector3 endPos = new Vector2(0, motivationalFinalYPosition);
            Color col = motivationalText.color;
            col.a = 0f;
            motivationalText.color = col;                                          // Commence transparent

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideDuration), 3f); // Easing cubic out
                motivationalText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                col.a = t;
                motivationalText.color = col;                                      // Fade in synchronisé avec le slide
                yield return null;
            }
            motivationalText.rectTransform.anchoredPosition = endPos;
            col.a = 1f;
            motivationalText.color = col;

            motivationalText.text = "";
            foreach (char c in currentMessage)
            {
                motivationalText.text += c;                                        // Ajoute une lettre à la fois
                if (c != ' ')
                    PlaySound(typewriterTickSound, typewriterVolume);              // Bip à chaque lettre, ignoré sur les espaces
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // PHASE 4 : Boutons slide + fade in
        if (respawnButton != null) respawnButton.gameObject.SetActive(true);
        if (quitButton != null) quitButton.gameObject.SetActive(true);

        Vector3 rStart = new Vector2(-buttonSpacing, buttonFinalYPosition - slideDistance); // Respawn part à gauche et en bas
        Vector3 rEnd = new Vector2(-buttonSpacing, buttonFinalYPosition);
        Vector3 qStart = new Vector2(buttonSpacing, buttonFinalYPosition - slideDistance); // Quit part à droite et en bas
        Vector3 qEnd = new Vector2(buttonSpacing, buttonFinalYPosition);

        CanvasGroup rCG = respawnButton?.GetComponent<CanvasGroup>() ?? respawnButton?.gameObject.AddComponent<CanvasGroup>(); // CanvasGroup pour le fade
        CanvasGroup qCG = quitButton?.GetComponent<CanvasGroup>() ?? quitButton?.gameObject.AddComponent<CanvasGroup>();
        if (rCG != null) rCG.alpha = 0f;                                          // Commence transparent
        if (qCG != null) qCG.alpha = 0f;

        float btn = 0f;
        while (btn < slideDuration)
        {
            btn += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(btn / slideDuration), 3f); 
            if (respawnButton != null) { respawnButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(rStart, rEnd, t); if (rCG != null) rCG.alpha = t; }
            if (quitButton != null) { quitButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(qStart, qEnd, t); if (qCG != null) qCG.alpha = t; }
            yield return null;
        }
        if (respawnButton != null) { respawnButton.GetComponent<RectTransform>().anchoredPosition = rEnd; if (rCG != null) rCG.alpha = 1f; } // Force la position finale
        if (quitButton != null) { quitButton.GetComponent<RectTransform>().anchoredPosition = qEnd; if (qCG != null) qCG.alpha = 1f; }
    }

    // ─────────────────────────────────────────────────────────────────────
    void PlaySound(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;                          // Sécurité : ne fait rien si la source ou le clip manque
        audioSource.PlayOneShot(clip, volume);                                    // PlayOneShot — ne coupe pas les sons précédents
    }

    // ─────────────────────────────────────────────────────────────────────
    public void HideDeathScreen()
    {
        StopAllCoroutines();                                                       // Stoppe toute animation en cours
        Cursor.lockState = CursorLockMode.Locked;                                 // Reverrouille le curseur en mode jeu
        Cursor.visible = false;

        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);           // Active le panel pour accéder aux enfants

        if (respawnButton != null) respawnButton.gameObject.SetActive(false);  // Cache tous les éléments
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (motivationalText != null) motivationalText.gameObject.SetActive(false);
        if (mainTitleText != null) mainTitleText.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);

        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);          // Cache le panel entier après les enfants

        if (gameUIPanel != null) gameUIPanel.SetActive(true);                     // Réaffiche le HUD de jeu
    }

    // ─────────────────────────────────────────────────────────────────────
    private void ResetUIElements()
    {
        if (mainTitleText != null) { mainTitleText.gameObject.SetActive(true); mainTitleText.rectTransform.anchoredPosition = Vector2.zero; } // Titre visible au centre
        if (motivationalText != null) { motivationalText.gameObject.SetActive(false); motivationalText.text = ""; }  // Texte caché et vidé
        if (respawnButton != null) respawnButton.gameObject.SetActive(false);  // Boutons cachés avant l'animation
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(true);     // Fond sombre toujours visible
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnRespawnClicked() => RegenerateMapAndRespawn();                          // Délègue à RegenerateMapAndRespawn pour recharger la scène

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;                          // Stoppe le Play Mode dans l'éditeur Unity
#else
        Application.Quit();                                                        // Quitte l'application en build
#endif
    }

    void RegenerateMapAndRespawn()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name        // Recharge la scène active — regénère la map et réinitialise tout
        );
    }
}