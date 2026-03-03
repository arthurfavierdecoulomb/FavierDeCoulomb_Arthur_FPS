using UnityEngine;

/// <summary>
/// Porte à rotation automatique avec détection par zone (Collider Trigger).
/// S'ouvre quand le joueur entre dans la zone, se referme quand il en sort.
///
/// SETUP :
/// 1. Attache ce script sur le GameObject pivot de la porte (le gond).
///    Si le pivot n'est pas au bord, crée un GameObject vide à la position du gond,
///    mets la porte en enfant, et attache ce script sur le parent vide.
/// 2. Ajoute un Collider (BoxCollider ou SphereCollider) sur ce même GameObject.
///    → Coche "Is Trigger" dans l'Inspector.
/// 3. Assure-toi que le joueur a le tag "Player" ET un Rigidbody (requis pour OnTrigger).
/// 4. Règle les paramètres dans l'Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DoorZone : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float openAngle = 90f;       // Angle d'ouverture
    [SerializeField] private float openSpeed = 3f;        // Vitesse d'ouverture  (degrés/s × 60)
    [SerializeField] private float closeSpeed = 2f;        // Vitesse de fermeture
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // Axe de rotation (Y = vertical)
    [SerializeField] private bool invertDirection = false;   // Inverse le sens d'ouverture

    [Header("Comportement")]
    [Tooltip("Délai en secondes avant que la porte se referme après que le joueur soit sorti.")]
    [SerializeField] private float closeDelay = 1.5f;

    [Tooltip("Distance devant/derrière la porte où le joueur est détecté (crée la zone auto si aucun Collider configuré).")]
    [SerializeField] private float detectionDepth = 2.5f;   // profondeur de la zone de chaque côté
    [SerializeField] private float detectionWidth = 2f;     // largeur de la zone (axe X de la porte)

    [Header("Sons (optionnel)")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    private AudioSource audioSource;

    // Rotations cible
    private Quaternion closedRotation;
    private Quaternion openRotation;

    // État
    private bool isOpen = false;
    private bool isMoving = false;
    private int playersInZone = 0;     // compteur (plusieurs joueurs possibles)
    private float closeTimer = -1f;

    // ─────────────────────────────────────────────
    void Start()
    {
        Init();

        // Configure automatiquement le BoxCollider pour qu'il dépasse des deux côtés
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = true;
            // La zone s'étend devant ET derrière la porte (axe Z local = direction de passage)
            box.size = new Vector3(detectionWidth, box.size.y > 0 ? box.size.y : 2f, detectionDepth * 2f);
            box.center = Vector3.zero;
        }
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }
    }

    /// Appelé par un générateur de map pour initialiser depuis le code
    public void SetupFromGenerator(float angle, float openSpd, float closeSpd, float delay = 0f)
    {
        openAngle = angle;
        openSpeed = openSpd;
        closeSpeed = closeSpd;
        closeDelay = delay;
        Init();
    }

    void Init()
    {
        closedRotation = transform.localRotation;
        float dir = invertDirection ? -1f : 1f;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle * dir, rotationAxis);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (openSound != null || closeSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ─────────────────────────────────────────────
    void Update()
    {
        // Gestion du délai de fermeture
        if (closeTimer >= 0f)
        {
            closeTimer -= Time.deltaTime;
            if (closeTimer < 0f)
                Close();
        }

        // Animation
        if (!isMoving) return;

        Quaternion target = isOpen ? openRotation : closedRotation;
        float speed = isOpen ? openSpeed : closeSpeed;

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation, target, speed * 60f * Time.deltaTime
        );

        if (Quaternion.Angle(transform.localRotation, target) < 0.5f)
        {
            transform.localRotation = target;
            isMoving = false;
        }
    }

    // ─────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInZone++;
        closeTimer = -1f;   // annule un éventuel délai de fermeture en cours

        if (!isOpen)
            Open();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInZone = Mathf.Max(0, playersInZone - 1);

        if (playersInZone == 0)
        {
            if (closeDelay > 0f)
                closeTimer = closeDelay;    // fermeture différée
            else
                Close();
        }
    }

    // ─────────────────────────────────────────────
    void Open()
    {
        isOpen = true;
        isMoving = true;
        PlaySound(openSound);
    }

    void Close()
    {
        isOpen = false;
        isMoving = true;
        closeTimer = -1f;
        PlaySound(closeSound);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    // ─────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        // Visualise la zone trigger dans la scène
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.35f);

        if (col is BoxCollider box)
        {
            Matrix4x4 prev = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                transform.TransformPoint(box.center),
                transform.rotation,
                transform.lossyScale
            );
            Gizmos.DrawCube(Vector3.zero, box.size);
            Gizmos.DrawWireCube(Vector3.zero, box.size);
            Gizmos.matrix = prev;
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.TransformPoint(sphere.center),
                              sphere.radius * Mathf.Max(transform.lossyScale.x,
                                                        transform.lossyScale.z));
            Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center),
                                  sphere.radius * Mathf.Max(transform.lossyScale.x,
                                                            transform.lossyScale.z));
        }
    }
}