using UnityEngine;
using System.Collections;

// Gère le comportement d'une lumière vacillante avec machine d'état,
// gestion du burn-out et retour d'audio spatial (bourdonnement + effets ponctuels).
public class FlickeringLight : MonoBehaviour
{
    // ─── Comportement global ──────────────────────────────────────────────

    [Header("Comportement global")]

    // Détermine si la lumière doit être allumée au démarrage.
    [SerializeField] private bool startOn = true;

    // Probabilité (0–1) que l'événement déclenché soit un flickering plutôt qu'un simple on/off.
    [SerializeField][Range(0f, 1f)] private float flickerChance = 0.6f;

    // Intervalle aléatoire (min/max en secondes) entre chaque évaluation de changement d'état.
    [SerializeField] private Vector2 stateChangeInterval = new Vector2(3f, 12f);


    // ─── Flickering ───────────────────────────────────────────────────────

    [Header("Flickering")]

    // Durée totale (min/max en secondes) d'une séquence de flickering.
    [SerializeField] private Vector2 flickerDuration = new Vector2(0.5f, 4f);

    // Intervalle (min/max en secondes) entre deux basculements d'état pendant le flickering.
    [SerializeField] private Vector2 flickerSpeed = new Vector2(0.02f, 0.15f);

    // Probabilité (0–1) que la lumière reste éteinte définitivement après un flickering ou une extinction.
    [SerializeField][Range(0f, 1f)] private float burnOutChance = 0.1f;


    // ─── Sons ─────────────────────────────────────────────────────────────

    [Header("Sons")]

    // Bourdonnement joué en boucle lorsque la lumière est allumée.
    [Tooltip("Bourdonnement en boucle quand la lumière est allumée")]
    [SerializeField] private AudioClip humSound;

    // Son déclenché une seule fois lorsque la lumière s'éteint.
    [Tooltip("Son joué quand la lumière s'éteint")]
    [SerializeField] private AudioClip turnOffSound;

    // Son déclenché une seule fois lorsque la lumière se rallume.
    [Tooltip("Son joué quand la lumière se rallume")]
    [SerializeField] private AudioClip turnOnSound;

    // Son déclenché à chaque basculement rapide pendant le flickering.
    [Tooltip("Son joué pendant le flickering")]
    [SerializeField] private AudioClip flickerSound;

    // Volume du bourdonnement continu (0 = muet, 1 = plein volume).
    [SerializeField][Range(0f, 1f)] private float humVolume = 0.3f;

    // Volume des sons d'extinction et de rallumage.
    [SerializeField][Range(0f, 1f)] private float eventVolume = 0.7f;

    // Volume des sons de flickering.
    [SerializeField][Range(0f, 1f)] private float flickerVolume = 0.4f;


    // ─── Références privées ───────────────────────────────────────────────

    // Source audio dédiée au bourdonnement en boucle.
    private AudioSource _humSource;

    // Source audio dédiée aux effets sonores ponctuels (on, off, flicker).
    private AudioSource _fxSource;

    // Référence à la coroutine principale pour pouvoir l'arrêter proprement.
    private Coroutine _mainCoroutine;

    // Indique si une séquence de flickering est en cours (évite les appels concurrents).
    private bool _isFlickering;

    // État logique actuel de la lumière (true = allumée).
    private bool _currentLightState;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Awake()
    {
        // Lit l'état initial de la lumière directement depuis le premier enfant de l'objet.
        _currentLightState = false;
        foreach (Transform child in transform)
        { _currentLightState = child.gameObject.activeSelf; break; }

        // Crée et configure la source audio pour le bourdonnement continu en 3D.
        _humSource = gameObject.AddComponent<AudioSource>();
        _humSource.clip = humSound;
        _humSource.loop = true;
        _humSource.playOnAwake = false;
        _humSource.spatialBlend = 1f;
        _humSource.volume = humVolume;

        // Crée et configure la source audio pour les effets ponctuels en 3D.
        _fxSource = gameObject.AddComponent<AudioSource>();
        _fxSource.loop = false;
        _fxSource.playOnAwake = false;
        _fxSource.spatialBlend = 1f;
    }

    void Start()
    {
        // Applique l'état de départ défini dans l'Inspector et démarre la machine d'état.
        SetLight(startOn);
        _mainCoroutine = StartCoroutine(StateMachine());
    }

    void OnDisable()
    {
        // Nettoie toutes les coroutines et stoppe le bourdonnement quand l'objet est désactivé.
        StopAllCoroutines();
        _mainCoroutine = null;
        _isFlickering = false;
        if (_humSource != null) _humSource.Stop();
    }

    void OnEnable()
    {
        // Redémarre la machine d'état si l'objet est réactivé en cours de jeu.
        if (_mainCoroutine == null && Application.isPlaying)
            _mainCoroutine = StartCoroutine(StateMachine());
    }


    // ─── Machine d'état principale ────────────────────────────────────────

    IEnumerator StateMachine()
    {
        while (true)
        {
            // Attend un délai aléatoire avant la prochaine évaluation d'état.
            float wait = Random.Range(stateChangeInterval.x, stateChangeInterval.y);
            yield return new WaitForSeconds(wait);

            // N'agit pas si un flickering est déjà en cours.
            if (_isFlickering) continue;

            if (Random.value < flickerChance)
            {
                // Déclenche une séquence de flickering.
                yield return DoFlicker();
            }
            else
            {
                bool isOn = GetLight();

                if (isOn && Random.value < 0.4f)
                {
                    // Éteint la lumière temporairement avec une chance de burn-out définitif.
                    SetLight(false);
                    yield return new WaitForSeconds(Random.Range(1f, 6f));

                    // Rallume seulement si le tirage dépasse la probabilité de burn-out.
                    if (Random.value > burnOutChance)
                        SetLight(true);
                }
                else if (!isOn)
                {
                    // Rallume la lumière si elle était éteinte et que le flickering n'a pas été choisi.
                    SetLight(true);
                }
            }
        }
    }


    // ─── Séquence de flickering ───────────────────────────────────────────

    IEnumerator DoFlicker()
    {
        _isFlickering = true;

        // Coupe le bourdonnement pendant le flickering pour éviter les chevauchements sonores.
        if (_humSource != null) _humSource.Stop();

        float duration = Random.Range(flickerDuration.x, flickerDuration.y);
        float timer = 0f;

        // 30 % de chance d'avoir une courte salve supplémentaire après le flickering principal.
        int extraFlickerCount = Random.value < 0.3f ? 1 : 0;
        int pass = 0;

        do
        {
            // La première passe utilise la durée complète, les suivantes une courte rafale.
            float sessionDuration = (pass == 0) ? duration : Random.Range(0.1f, 0.4f);
            timer = 0f;

            while (timer < sessionDuration)
            {
                // Bascule l'état de la lumière sans déclencher les sons on/off.
                bool next = !GetLight();
                SetLightSilent(next);

                // Joue le son de flickering avec une légère variation de volume aléatoire.
                if (_fxSource != null && flickerSound != null)
                    _fxSource.PlayOneShot(flickerSound, flickerVolume * Random.Range(0.5f, 1f));

                // Assure un intervalle minimum d'une frame pour éviter les boucles infinies.
                float interval = Mathf.Max(Random.Range(flickerSpeed.x, flickerSpeed.y), 0.016f);
                yield return new WaitForSeconds(interval);
                timer += interval;
            }

            if (pass < extraFlickerCount)
            {
                // Petite pause lumière allumée entre deux salves de flickering.
                SetLightSilent(true);
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            }

            pass++;
        }
        while (pass <= extraFlickerCount);

        // Détermine l'état final : allumée normalement ou burn-out définitif.
        bool finalState = Random.value >= burnOutChance;
        SetLight(finalState);

        _isFlickering = false;
    }


    // ─── Contrôle de la lumière avec sons ────────────────────────────────

    void SetLight(bool on)
    {
        // Mémorise l'état précédent AVANT de l'appliquer pour détecter le changement.
        bool wasOn = _currentLightState;
        SetLightSilent(on);

        if (!_isFlickering)
        {
            // Joue le son d'extinction si la lumière vient de s'éteindre.
            if (wasOn && !on)
            {
                
                if (turnOffSound != null) _fxSource.PlayOneShot(turnOffSound, eventVolume);
            }

            // Joue le son de rallumage si la lumière vient de s'allumer.
            if (!wasOn && on)
            {
               
                if (turnOnSound != null) _fxSource.PlayOneShot(turnOnSound, eventVolume);
            }
        }

        // Synchronise le bourdonnement avec l'état courant de la lumière.
        if (humSound != null && _humSource != null)
        {
            if (on && !_humSource.isPlaying)
                _humSource.Play();
            else if (!on && _humSource.isPlaying)
                _humSource.Stop();
        }
    }

    // Applique l'état de la lumière sur tous les enfants sans déclencher de sons.
    // Utilisé pendant le flickering pour éviter les déclenchements audio parasites.
    void SetLightSilent(bool on)
    {
        _currentLightState = on;

        // Active ou désactive chaque objet enfant (ex : Light, mesh émissif).
        foreach (Transform child in transform)
            child.gameObject.SetActive(on);
    }

    // Retourne l'état réel de la lumière en lisant l'activation du premier enfant.
    bool GetLight()
    {
        foreach (Transform child in transform)
            return child.gameObject.activeSelf;
        return false;
    }


    // ─── API publique ─────────────────────────────────────────────────────

    // Allume ou éteint la lumière depuis un script externe (ex : trigger, cinématique).
    public void SetOn(bool on) => SetLight(on);

    // Déclenche manuellement une séquence de flickering si aucune n'est déjà en cours.
    public void TriggerFlicker()
    {
        if (!_isFlickering)
            StartCoroutine(DoFlicker());
    }
}