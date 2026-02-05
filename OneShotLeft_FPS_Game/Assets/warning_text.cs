using UnityEngine;
using TMPro;
using System.Collections;

public class TypewriterWarning : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private StaminaSystem staminaSystem;

    [Header("Messages - Plus de balles")]
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

    [Header("Messages - Balle récupérée")]
    [SerializeField]
    private string[] reloadMessages = new string[]
    {
        "Ah la voilà !",
        "aller, encore moins de chance de crever !",
        "ah oui, elle était ici !",
        "je peux desormait faire paw paw !",
        "ouais, je vais pas crever aujourd'hui !",
    };

    [Header("Messages - Trop fatigué pour courir")]
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

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private Color emptyColor = Color.yellow;
    [SerializeField] private Color reloadColor = Color.green;
    [SerializeField] private Color fatigueColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private float messageDuration = 3f;

    [Header("Fatigue Settings")]
    [SerializeField] private float staminaThreshold = 5f; // Seuil de stamina (5%)
    [SerializeField] private float fatigueCooldown = 2f; // Cooldown entre messages de fatigue

    private bool hasShownWarning = false;
    private int lastBulletCount = -1;
    private float lastFatigueMessageTime = -999f;
    private Coroutine typewriterCoroutine;
    private Coroutine fadeCoroutine;

    void Start()
    {
        if (warningText != null)
        {
            warningText.text = "";
        }
    }

    void Update()
    {
        if (warningText == null) return;

        // Gestion des messages d'arme
        if (weaponController != null)
        {
            int currentBullets = weaponController.GetCurrentBullets();

            // Message quand on passe à zéro
            if (currentBullets == 0 && !hasShownWarning)
            {
                hasShownWarning = true;
                StopAllTypewriterCoroutines();
                string randomMessage = emptyMessages[Random.Range(0, emptyMessages.Length)];
                typewriterCoroutine = StartCoroutine(TypeMessage(randomMessage, emptyColor, false));
            }

            // Message quand on récupère la balle
            if (currentBullets > 0 && lastBulletCount == 0)
            {
                hasShownWarning = false;
                StopAllTypewriterCoroutines();
                string randomMessage = reloadMessages[Random.Range(0, reloadMessages.Length)];
                typewriterCoroutine = StartCoroutine(TypeMessage(randomMessage, reloadColor, true));
            }

            lastBulletCount = currentBullets;
        }

        // Gestion des messages de fatigue
        if (staminaSystem != null)
        {
            float currentStamina = staminaSystem.GetCurrentStamina();
            float maxStamina = staminaSystem.GetMaxStamina();
            float staminaPercent = (currentStamina / maxStamina) * 100f;

            // Si le joueur essaie de courir avec moins de 5% de stamina
            if (staminaPercent < staminaThreshold && Input.GetKey(KeyCode.LeftShift))
            {
                // Vérifie le cooldown pour ne pas spammer les messages
                if (Time.time - lastFatigueMessageTime > fatigueCooldown)
                {
                    lastFatigueMessageTime = Time.time;
                    StopAllTypewriterCoroutines();
                    string randomMessage = fatigueMessages[Random.Range(0, fatigueMessages.Length)];
                    typewriterCoroutine = StartCoroutine(TypeMessage(randomMessage, fatigueColor, true));
                }
            }
        }
    }

    void StopAllTypewriterCoroutines()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    IEnumerator TypeMessage(string message, Color color, bool autoFade)
    {
        warningText.text = "";
        warningText.color = color;

        foreach (char letter in message)
        {
            warningText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        typewriterCoroutine = null;

        // Si c'est un message qui doit disparaître
        if (autoFade)
        {
            fadeCoroutine = StartCoroutine(FadeOutMessage());
        }
    }

    IEnumerator FadeOutMessage()
    {
        yield return new WaitForSeconds(messageDuration);
        warningText.text = "";
        fadeCoroutine = null;
    }
}