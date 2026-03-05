using UnityEngine;
using System.Collections;

public class FlickeringLight : MonoBehaviour
{
    [Header("Comportement global")]
    [SerializeField] private bool startOn = true;
    [SerializeField][Range(0f, 1f)] private float flickerChance = 0.6f;
    [SerializeField] private Vector2 stateChangeInterval = new Vector2(3f, 12f);

    [Header("Flickering")]
    [SerializeField] private Vector2 flickerDuration = new Vector2(0.5f, 4f);
    [SerializeField] private Vector2 flickerSpeed = new Vector2(0.02f, 0.15f);
    [SerializeField][Range(0f, 1f)] private float burnOutChance = 0.1f;

    [Header("Son (optionnel)")]
    [SerializeField] private AudioClip flickerSound;
    [SerializeField][Range(0f, 1f)] private float flickerSoundVolume = 0.4f;
    private AudioSource _audio;

    // Garde le contrôle de la coroutine principale
    private Coroutine _mainCoroutine;
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
        SetLight(startOn);
        _mainCoroutine = StartCoroutine(StateMachine());
    }

    void OnDisable()
    {
        // Stoppe proprement toutes les coroutines quand l'objet est désactivé
        StopAllCoroutines();
        _mainCoroutine = null;
        _isFlickering = false;
    }

    void OnEnable()
    {
        // Relance la machine d'état si l'objet est réactivé
        if (_mainCoroutine == null && Application.isPlaying)
            _mainCoroutine = StartCoroutine(StateMachine());
    }

    // ─── Machine d'état principale ───────────────────────────────────────────
    IEnumerator StateMachine()
    {
        while (true)
        {
            float wait = Random.Range(stateChangeInterval.x, stateChangeInterval.y);
            yield return new WaitForSeconds(wait);

            if (_isFlickering) continue;    // sécurité : jamais deux flickerings en parallèle

            if (Random.value < flickerChance)
            {
                yield return DoFlicker();   // attend la fin avant de continuer
            }
            else
            {
                bool isOn = GetLight();
                if (isOn && Random.value < 0.4f)
                {
                    SetLight(false);
                    yield return new WaitForSeconds(Random.Range(1f, 6f));
                    if (Random.value > burnOutChance)
                        SetLight(true);
                }
                else if (!isOn)
                {
                    SetLight(true);
                }
            }
        }
    }

    // ─── Session de flickering (sans récursion) ───────────────────────────────
    IEnumerator DoFlicker()
    {
        _isFlickering = true;

        float duration = Random.Range(flickerDuration.x, flickerDuration.y);
        float timer = 0f;

        // Soubresaut final autorisé une seule fois maximum
        int extraFlickerCount = Random.value < 0.3f ? 1 : 0;
        int pass = 0;

        do
        {
            float sessionDuration = (pass == 0) ? duration : Random.Range(0.1f, 0.4f);
            timer = 0f;

            while (timer < sessionDuration)
            {
                bool next = !GetLight();
                SetLight(next);

                if (next && _audio != null && flickerSound != null)
                    _audio.PlayOneShot(flickerSound, flickerSoundVolume * Random.Range(0.5f, 1f));

                float interval = Random.Range(flickerSpeed.x, flickerSpeed.y);
                // Sécurité : intervalle minimum pour éviter une boucle infinie trop rapide
                interval = Mathf.Max(interval, 0.016f);
                yield return new WaitForSeconds(interval);
                timer += interval;
            }

            // Entre les deux sessions : petite pause lumière allumée
            if (pass < extraFlickerCount)
            {
                SetLight(true);
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            }

            pass++;
        }
        while (pass <= extraFlickerCount);

        // Fin : burn out ou retour normal
        SetLight(Random.value >= burnOutChance);

        _isFlickering = false;
    }

    // ─── Active/désactive tous les enfants ───────────────────────────────────
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

    public void SetOn(bool on) => SetLight(on);
    public void TriggerFlicker()
    {
        if (!_isFlickering)
            StartCoroutine(DoFlicker());
    }
}