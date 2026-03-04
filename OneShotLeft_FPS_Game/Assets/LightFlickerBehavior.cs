using UnityEngine;
using System.Collections;

/// <summary>
/// Lumière défaillante style survival/horreur.
/// Attache ce script sur le GameObject EMPTY parent qui contient la lumière.
/// Le script active/désactive ce GameObject entier pour allumer/éteindre.
/// </summary>
public class FlickeringLight : MonoBehaviour
{
    [Header("Comportement global")]
    [Tooltip("Allumée au départ ?")]
    [SerializeField] private bool startOn = true;

    [Tooltip("Probabilité que la lumière parte en flickering toute seule (0 = jamais, 1 = toujours).")]
    [SerializeField][Range(0f, 1f)] private float flickerChance = 0.6f;

    [Tooltip("Intervalle min/max en secondes entre chaque changement d'état aléatoire.")]
    [SerializeField] private Vector2 stateChangeInterval = new Vector2(3f, 12f);

    [Header("Flickering")]
    [Tooltip("Durée min/max d'une session de flickering (secondes).")]
    [SerializeField] private Vector2 flickerDuration = new Vector2(0.5f, 4f);

    [Tooltip("Vitesse du flickering : intervalle min/max entre chaque clignotement (secondes).")]
    [SerializeField] private Vector2 flickerSpeed = new Vector2(0.02f, 0.15f);

    [Tooltip("Chance que la lumière reste éteinte après un flickering (ampoule qui lâche).")]
    [SerializeField][Range(0f, 1f)] private float burnOutChance = 0.1f;

    [Header("Son (optionnel)")]
    [SerializeField] private AudioClip flickerSound;
    [SerializeField][Range(0f, 1f)] private float flickerSoundVolume = 0.4f;
    private AudioSource _audio;

    private bool _isFlickering;

    void Awake()
    {
        if (flickerSound != null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f;
            _audio.loop = false;
        }
    }

    void Start()
    {
        gameObject.SetActive(true);     // le script doit rester actif
        SetLight(startOn);
        StartCoroutine(StateMachine());
    }

    // ─── Machine d'état principale ───────────────────────────────────────────
    IEnumerator StateMachine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(stateChangeInterval.x, stateChangeInterval.y));

            if (Random.value < flickerChance)
            {
                yield return StartCoroutine(DoFlicker());
            }
            else
            {
                bool isOn = GetLight();
                bool turnOff = isOn && Random.value < 0.4f;
                SetLight(!turnOff);

                if (turnOff)
                {
                    yield return new WaitForSeconds(Random.Range(1f, 6f));
                    if (Random.value > burnOutChance)
                        SetLight(true);
                }
            }
        }
    }

    // ─── Session de flickering ────────────────────────────────────────────────
    IEnumerator DoFlicker()
    {
        _isFlickering = true;
        float duration = Random.Range(flickerDuration.x, flickerDuration.y);
        float timer = 0f;

        while (timer < duration)
        {
            bool next = !GetLight();
            SetLight(next);

            if (next && _audio != null && flickerSound != null)
                _audio.PlayOneShot(flickerSound, flickerSoundVolume * Random.Range(0.5f, 1f));

            float interval = Random.Range(flickerSpeed.x, flickerSpeed.y);
            yield return new WaitForSeconds(interval);
            timer += interval;
        }

        if (Random.value < burnOutChance)
        {
            SetLight(false);    // ampoule grillée
        }
        else
        {
            SetLight(true);
            // Petit soubresaut après stabilisation
            if (Random.value < 0.3f)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
                yield return StartCoroutine(DoFlicker());
                yield break;
            }
        }

        _isFlickering = false;
    }

    // ─── Active/désactive tous les enfants (lumières, mesh, etc.) ────────────
    void SetLight(bool on)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(on);
    }

    bool GetLight()
    {
        foreach (Transform child in transform)
            return child.gameObject.activeSelf;
        return false;
    }

    // ─── API publique ─────────────────────────────────────────────────────────
    public void SetOn(bool on) => SetLight(on);
    public void TriggerFlicker() => StartCoroutine(DoFlicker());
}