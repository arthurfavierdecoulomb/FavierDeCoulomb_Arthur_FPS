using UnityEngine;
using TMPro;
using System.Collections;

// Affiche des messages contextuels en typewriter dans l'UI du joueur :
// manque de munitions, récupération de balle, ou fatigue au sprint.
// Chaque événement pioche aléatoirement dans une liste de messages et l'affiche lettre par lettre.
public class TypewriterWarning : MonoBehaviour
{
    // ─── Références ───────────────────────────────────────────────────────

    [Header("References")]

    // Texte TMPro sur lequel les messages sont écrits en typewriter.
    [SerializeField] private TextMeshProUGUI warningText;

    // Référence au WeaponController pour lire le nombre de balles courantes.
    [SerializeField] private WeaponController weaponController;

    // Référence au StaminaSystem pour lire le pourcentage de stamina.
    [SerializeField] private StaminaSystem staminaSystem;


    // ─── Messages – Plus de balles ────────────────────────────────────────

    [Header("Messages - Plus de balles")]

    // Messages affichés en jaune lorsque le joueur n'a plus de munitions.
    [SerializeField]
    private string[] emptyMessages = new string[]
    {
        "Ah... je n'ai plus de balle",
        "Je me demande bien pourquoi je ne peux plus faire paw paw avec mon pistolet ?",
        "Mince, j'aurais dû ramasser ma balle...",
        "Bon bah... va falloir que j'aille la chercher maintenant",
        "Oups, plus de munitions !",
        "C'est pas grave, je cours vite de toute façon",
        "Ma balle doit être quelque part par là...",
        "J'ai peut-être oublié quelque chose ?",
        "Ah oui c'est vrai, je n'ai qu'une seule balle",
        "Oh non, mais... roooh !",
        "ah c'est ce bouton pour tirer ?",
    };


    // ─── Messages – Balle récupérée ───────────────────────────────────────

    [Header("Messages - Balle récupérée")]

    // Messages affichés en vert lorsque le joueur récupère sa balle (balles 0 → > 0).
    [SerializeField]
    private string[] reloadMessages = new string[]
    {
        "Ah la voilà !",
        "aller, encore moins de chance de crever !",
        "ah oui, elle était ici !",
        "je peux desormait faire paw paw !",
        "ouais, je vais pas crever aujourd'hui !",
    };


    // ─── Messages – Fatigue ───────────────────────────────────────────────

    [Header("Messages - Trop fatigué pour courir")]

    // Messages affichés en orange lorsque le joueur tente de sprinter avec trop peu de stamina.
    [SerializeField]
    private string[] fatigueMessages = new string[]
    {
        "Je ne peux faire cela...",
        "J'ai besoin d'une pause...",
        "Je ne me sens pas capable...",
        "Je suis faatigué...",
        "Ouf...ouf... mes poumons vont lâcher...",
        "Trop... épuisé...",
        "Je dois reprendre mon souffle...",
        "Mes jambes ne répondent plus...",
        "Laissez-moi respirer...",
        "Je n'en peux plus..."
    };


    // ─── Typewriter Settings ──────────────────────────────────────────────

    [Header("Typewriter Settings")]

    // Délai en secondes entre l'affichage de chaque lettre.
    [SerializeField] private float typingSpeed = 0.05f;

    // Couleur du texte pour les messages "plus de munitions".
    [SerializeField] private Color emptyColor = Color.yellow;

    // Couleur du texte pour les messages "balle récupérée".
    [SerializeField] private Color reloadColor = Color.green;

    // Couleur du texte pour les messages de fatigue (orange).
    [SerializeField] private Color fatigueColor = new Color(1f, 0.5f, 0f);

    // Durée en secondes pendant laquelle le message reste affiché avant de disparaître.
    [SerializeField] private float messageDuration = 3f;


    // ─── Fatigue Settings ─────────────────────────────────────────────────

    [Header("Fatigue Settings")]

    // Seuil de stamina en pourcentage (0–100) en dessous duquel le message de fatigue peut s'afficher.
    [SerializeField] private float staminaThreshold = 5f;

    // Délai minimum en secondes entre deux messages de fatigue consécutifs (anti-spam).
    [SerializeField] private float fatigueCooldown = 2f;


    // ─── Son ──────────────────────────────────────────────────────────────

    [Header("Son")]

    // Bip joué à chaque lettre affichée par le typewriter (sauf les espaces).
    [Tooltip("Bip joué à chaque lettre du typewriter")]
    [SerializeField] private AudioClip tickSound;

    // Volume du bip typewriter (0 = muet, 1 = plein volume).
    [SerializeField][Range(0f, 1f)] private float tickVolume = 0.4f;


    // ─── Variables privées ────────────────────────────────────────────────

    // Source audio 2D créée dynamiquement pour les sons d'interface.
    private AudioSource audioSource;

    // Empêche l'affichage répété du message "plus de munitions" tant que le joueur n'a pas rechargé.
    private bool hasShownWarning = false;

    // Mémorise le nombre de balles à la frame précédente pour détecter le passage 0 → > 0.
    private int lastBulletCount = -1;

    // Timestamp du dernier message de fatigue affiché, initialisé loin dans le passé
    // pour autoriser l'affichage dès le premier déclenchement.
    private float lastFatigueMessageTime = -999f;

    // Référence à la coroutine du typewriter en cours, pour pouvoir l'interrompre.
    private Coroutine typewriterCoroutine;

    // Référence à la coroutine de disparition du message, pour pouvoir l'interrompre.
    private Coroutine fadeCoroutine;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Start()
    {
        // Vide le texte au démarrage pour éviter tout résidu d'une session précédente.
        if (warningText != null) warningText.text = "";

        // Récupère ou crée la source audio 2D dédiée aux bips du typewriter.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }


    // ─── Détection des événements ─────────────────────────────────────────

    void Update()
    {
        // Ne fait rien si le texte UI est absent.
        if (warningText == null) return;

        // ── Surveillance des munitions ────────────────────────────────────

        if (weaponController != null)
        {
            int currentBullets = weaponController.GetCurrentBullets();

            // Déclenche le message "plus de munitions" lors du passage à 0 balle.
            // hasShownWarning évite de respammer le message si le joueur reste à 0.
            if (currentBullets == 0 && !hasShownWarning)
            {
                hasShownWarning = true;
                StopAllTypewriterCoroutines();
                string msg = emptyMessages[Random.Range(0, emptyMessages.Length)];
                typewriterCoroutine = StartCoroutine(TypeMessage(msg, emptyColor, false));
            }

            // Déclenche le message "balle récupérée" lors du passage de 0 à > 0 balle.
            // lastBulletCount == 0 garantit qu'on réagit uniquement au front montant.
            if (currentBullets > 0 && lastBulletCount == 0)
            {
                hasShownWarning = false;
                StopAllTypewriterCoroutines();
                string msg = reloadMessages[Random.Range(0, reloadMessages.Length)];
                typewriterCoroutine = StartCoroutine(TypeMessage(msg, reloadColor, true));
            }

            // Mémorise le compte de balles pour la détection du changement à la prochaine frame.
            lastBulletCount = currentBullets;
        }

        // ── Surveillance de la fatigue ────────────────────────────────────

        if (staminaSystem != null)
        {
            float currentStamina = staminaSystem.GetCurrentStamina();
            float maxStamina = staminaSystem.GetMaxStamina();
            float staminaPercent = (currentStamina / maxStamina) * 100f;

            // Déclenche un message de fatigue si le joueur tente de sprinter (LeftShift)
            // avec une stamina sous le seuil, en respectant le cooldown anti-spam.
            if (staminaPercent < staminaThreshold && Input.GetKey(KeyCode.LeftShift))
            {
                if (Time.time - lastFatigueMessageTime > fatigueCooldown)
                {
                    // Mémorise le moment de ce déclenchement pour calculer le prochain cooldown.
                    lastFatigueMessageTime = Time.time;
                    StopAllTypewriterCoroutines();
                    string msg = fatigueMessages[Random.Range(0, fatigueMessages.Length)];
                    typewriterCoroutine = StartCoroutine(TypeMessage(msg, fatigueColor, true));
                }
            }
        }
    }


    // ─── Interruption des coroutines ──────────────────────────────────────

    // Arrête proprement le typewriter et le fade en cours avant d'en démarrer un nouveau.
    // Indispensable pour éviter que deux messages s'écrivent simultanément sur le même texte.
    void StopAllTypewriterCoroutines()
    {
        if (typewriterCoroutine != null) { StopCoroutine(typewriterCoroutine); typewriterCoroutine = null; }
        if (fadeCoroutine != null) { StopCoroutine(fadeCoroutine); fadeCoroutine = null; }
    }


    // ─── Typewriter ───────────────────────────────────────────────────────

    // Affiche le message caractère par caractère avec la couleur donnée.
    // Si autoFade est true, lance la coroutine de disparition après la fin du message.
    IEnumerator TypeMessage(string message, Color color, bool autoFade)
    {
        // Réinitialise le texte et applique la couleur associée à l'événement.
        warningText.text = "";
        warningText.color = color;

        foreach (char letter in message)
        {
            warningText.text += letter;

            // N'émet pas de bip sur les espaces pour un rendu sonore plus naturel.
            if (letter != ' ')
                audioSource.PlayOneShot(tickSound != null ? tickSound : null, tickVolume);

            yield return new WaitForSeconds(typingSpeed);
        }

        // Remet la référence à null une fois le typewriter terminé.
        typewriterCoroutine = null;

        // Démarre le fade uniquement si demandé (les messages "plus de munitions"
        // restent affichés indéfiniment jusqu'à ce que la balle soit récupérée).
        if (autoFade) fadeCoroutine = StartCoroutine(FadeOutMessage());
    }


    // ─── Disparition du message ───────────────────────────────────────────

    // Attend la durée configurée puis efface le texte.
    // Utilisé pour les messages temporaires (balle récupérée, fatigue).
    IEnumerator FadeOutMessage()
    {
        yield return new WaitForSeconds(messageDuration);
        warningText.text = "";
        fadeCoroutine = null;
    }
}