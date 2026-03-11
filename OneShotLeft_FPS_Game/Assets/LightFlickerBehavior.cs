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

    [Header("Sons")]
    [Tooltip("Bourdonnement en boucle quand la lumière est allumée")]
    [SerializeField] private AudioClip humSound;
    [Tooltip("Son joué quand la lumière s'éteint")]
    [SerializeField] private AudioClip turnOffSound;
    [Tooltip("Son joué quand la lumière se rallume")]
    [SerializeField] private AudioClip turnOnSound;
    [Tooltip("Son joué pendant le flickering")]
    [SerializeField] private AudioClip flickerSound;

    [SerializeField][Range(0f, 1f)] private float humVolume = 0.3f;
    [SerializeField][Range(0f, 1f)] private float eventVolume = 0.7f;
    [SerializeField][Range(0f, 1f)] private float flickerVolume = 0.4f;

    // Deux sources : loop bourdonnement + FX ponctuels
    private AudioSource _humSource;
    private AudioSource _fxSource;

    private Coroutine _mainCoroutine;
    private bool _isFlickering;
    private bool _currentLightState;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Initialise l'état selon la réalité des enfants dans la scène
        _currentLightState = false;
        foreach (Transform child in transform)
        { _currentLightState = child.gameObject.activeSelf; break; }

        // Source bourdonnement
        _humSource = gameObject.AddComponent<AudioSource>();
        _humSource.clip = humSound;
        _humSource.loop = true;
        _humSource.playOnAwake = false;
        _humSource.spatialBlend = 1f;
        _humSource.volume = humVolume;

        // Source FX ponctuels
        _fxSource = gameObject.AddComponent<AudioSource>();
        _fxSource.loop = false;
        _fxSource.playOnAwake = false;
        _fxSource.spatialBlend = 1f;
    }

    void Start()
    {
        SetLight(startOn);
        _mainCoroutine = StartCoroutine(StateMachine());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        _mainCoroutine = null;
        _isFlickering = false;
        if (_humSource != null) _humSource.Stop();
    }

    void OnEnable()
    {
        if (_mainCoroutine == null && Application.isPlaying)
            _mainCoroutine = StartCoroutine(StateMachine());
    }

    // ─── Machine d'état principale ────────────────────────────────────────
    IEnumerator StateMachine()
    {
        while (true)
        {
            float wait = Random.Range(stateChangeInterval.x, stateChangeInterval.y);
            yield return new WaitForSeconds(wait);

            if (_isFlickering) continue;

            if (Random.value < flickerChance)
            {
                yield return DoFlicker();
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

    // ─── Flickering ───────────────────────────────────────────────────────
    IEnumerator DoFlicker()
    {
        _isFlickering = true;

        // Coupe le bourdonnement pendant le flickering
        if (_humSource != null) _humSource.Stop();

        float duration = Random.Range(flickerDuration.x, flickerDuration.y);
        float timer = 0f;

        int extraFlickerCount = Random.value < 0.3f ? 1 : 0;
        int pass = 0;

        do
        {
            float sessionDuration = (pass == 0) ? duration : Random.Range(0.1f, 0.4f);
            timer = 0f;

            while (timer < sessionDuration)
            {
                bool next = !GetLight();
                // Pas de son on/off pendant flicker — juste le flickerSound
                SetLightSilent(next);

                if (_fxSource != null && flickerSound != null)
                    _fxSource.PlayOneShot(flickerSound, flickerVolume * Random.Range(0.5f, 1f));

                float interval = Mathf.Max(Random.Range(flickerSpeed.x, flickerSpeed.y), 0.016f);
                yield return new WaitForSeconds(interval);
                timer += interval;
            }

            if (pass < extraFlickerCount)
            {
                SetLightSilent(true);
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            }

            pass++;
        }
        while (pass <= extraFlickerCount);

        // Fin du flickering : burn out ou rallumage
        bool finalState = Random.value >= burnOutChance;
        SetLight(finalState);

        _isFlickering = false;
    }

    // ─── SetLight avec sons ───────────────────────────────────────────────
    void SetLight(bool on)
    {
        bool wasOn = _currentLightState; // lu AVANT SetLightSilent
        SetLightSilent(on);              // met à jour _currentLightState

        if (!_isFlickering)
        {
            if (wasOn && !on)
            {
                Debug.Log($"[FlickeringLight] Extinction — turnOffSound: {(turnOffSound != null ? turnOffSound.name : "NULL")}");
                if (turnOffSound != null) _fxSource.PlayOneShot(turnOffSound, eventVolume);
            }
            if (!wasOn && on)
            {
                Debug.Log($"[FlickeringLight] Rallumage — turnOnSound: {(turnOnSound != null ? turnOnSound.name : "NULL")}");
                if (turnOnSound != null) _fxSource.PlayOneShot(turnOnSound, eventVolume);
            }
        }

        // Bourdonnement — démarre/stoppe selon l'état
        if (humSound != null && _humSource != null)
        {
            if (on && !_humSource.isPlaying)
                _humSource.Play();
            else if (!on && _humSource.isPlaying)
                _humSource.Stop();
        }
    }

    // SetLight sans déclencher de sons (utilisé pendant le flickering)
    void SetLightSilent(bool on)
    {
        _currentLightState = on;
        foreach (Transform child in transform)
            child.gameObject.SetActive(on);
    }

    bool GetLight()
    {
        foreach (Transform child in transform)
            return child.gameObject.activeSelf;
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────
    public void SetOn(bool on) => SetLight(on);
    public void TriggerFlicker()
    {
        if (!_isFlickering)
            StartCoroutine(DoFlicker());
    }
}