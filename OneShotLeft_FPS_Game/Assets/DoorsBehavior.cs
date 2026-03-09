using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorZone : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 3f;
    [SerializeField] private float closeSpeed = 2f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    [SerializeField] private bool invertDirection = false;

    [Header("Comportement")]
    [Tooltip("Délai en secondes avant que la porte se referme après que le déclencheur soit sorti.")]
    [SerializeField] private float closeDelay = 1.5f;

    [Tooltip("Distance devant/derrière la porte où le joueur/zombie est détecté.")]
    [SerializeField] private float detectionDepth = 2.5f;
    [SerializeField] private float detectionWidth = 2f;

    [Header("Zombies")]
    [Tooltip("Les zombies peuvent ouvrir la porte en passant dedans.")]
    [SerializeField] private bool zombiesCanOpen = true;

    [Tooltip("Tag utilisé sur les zombies (doit correspondre à celui de tes prefabs).")]
    [SerializeField] private string zombieTag = "Zombie";

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
    private int inZoneCount = 0;   // joueurs + zombies dans la zone
    private float closeTimer = -1f;

    // ─────────────────────────────────────────────
    void Start()
    {
        Init();

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = true;
            box.size = new Vector3(detectionWidth, box.size.y > 0 ? box.size.y : 2f, detectionDepth * 2f);
            box.center = Vector3.zero;
        }
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }
    }

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
        if (closeTimer >= 0f)
        {
            closeTimer -= Time.deltaTime;
            if (closeTimer < 0f) Close();
        }

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
        if (!IsAllowed(other)) return;

        inZoneCount++;
        closeTimer = -1f;

        if (!isOpen) Open();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsAllowed(other)) return;

        inZoneCount = Mathf.Max(0, inZoneCount - 1);

        if (inZoneCount == 0)
        {
            if (closeDelay > 0f) closeTimer = closeDelay;
            else Close();
        }
    }

    // ─── Vérifie si le collider est autorisé à ouvrir la porte ───────────
    bool IsAllowed(Collider other)
    {
        if (other.CompareTag("Player")) return true;
        if (zombiesCanOpen && other.CompareTag(zombieTag)) return true;
        return false;
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
            float r = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            Gizmos.DrawSphere(transform.TransformPoint(sphere.center), r);
            Gizmos.DrawWireSphere(transform.TransformPoint(sphere.center), r);
        }
    }
}