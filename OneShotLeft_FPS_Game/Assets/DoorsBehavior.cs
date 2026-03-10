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
    [SerializeField] private float detectionDepth = 2.5f;
    [SerializeField] private float detectionWidth = 2f;

    [Header("Zombies")]
    [SerializeField] private bool zombiesCanOpen = true;
    [SerializeField] private string zombieTag = "Zombie";

    [Header("Sons")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Atténuation par distance")]
    [Tooltip("Distance en dessous de laquelle le volume est maximal.")]
    [SerializeField] private float minDistance = 3f;
    [Tooltip("Distance au-delà de laquelle le son est inaudible.")]
    [SerializeField] private float maxDistance = 20f;
    [Tooltip("Volume maximal quand le joueur est tout proche.")]
    [SerializeField][Range(0f, 1f)] private float maxVolume = 1f;

    private AudioSource audioSource;
    private Transform playerTransform;

    // Rotations cible
    private Quaternion closedRotation;
    private Quaternion openRotation;

    // État
    private bool isOpen = false;
    private bool isMoving = false;
    private int inZoneCount = 0;
    private float closeTimer = -1f;

    // ─────────────────────────────────────────────────────────────────────
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

        // Trouve le joueur une seule fois
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) playerTransform = playerGO.transform;
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

        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f; // on gère le volume manuellement
            audioSource.playOnAwake = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────
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

    bool IsAllowed(Collider other)
    {
        if (other.CompareTag("Player")) return true;
        if (zombiesCanOpen && other.CompareTag(zombieTag)) return true;
        return false;
    }

    // ─────────────────────────────────────────────────────────────────────
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
        if (audioSource == null || clip == null) return;

        float vol = GetVolumeByDistance();
        if (vol <= 0f) return; // trop loin, on ne joue même pas le son

        audioSource.PlayOneShot(clip, vol);
    }

    // ─── Volume calculé selon la distance au joueur ───────────────────────
    float GetVolumeByDistance()
    {
        if (playerTransform == null)
        {
            // Cherche le joueur si pas encore trouvé
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) playerTransform = playerGO.transform;
            else return maxVolume; // pas de joueur trouvé : volume max par défaut
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= minDistance) return maxVolume;
        if (dist >= maxDistance) return 0f;

        // Interpolation linéaire entre minDistance et maxDistance
        float t = 1f - Mathf.InverseLerp(minDistance, maxDistance, dist);
        return Mathf.Lerp(0f, maxVolume, t);
    }

    // ─────────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        // Zone de détection
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
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
        }

        // Cercles de distance audio
        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}