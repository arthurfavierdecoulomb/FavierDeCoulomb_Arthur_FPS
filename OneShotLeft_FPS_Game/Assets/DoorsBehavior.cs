using UnityEngine;

/// <summary>
/// Porte à rotation automatique.
/// S'ouvre en pivotant quand le joueur s'approche, se referme quand il s'éloigne.
///
/// SETUP :
/// 1. Attache ce script sur le GameObject de la porte (Door ou Glass_door)
/// 2. IMPORTANT : le pivot de rotation doit être au bord de la porte (le gond)
///    Si ce n'est pas le cas, crée un GameObject vide à la position du gond,
///    mets la porte en enfant, et attache ce script sur le parent vide.
/// 3. Règle les paramètres dans l'Inspector
/// </summary>
public class DoorRotate : MonoBehaviour
{
    [Header("Détection")]
    [SerializeField] private float openDistance = 3f;   // Distance de détection du joueur

    [Header("Rotation")]
    [SerializeField] private float openAngle = 90f;   // Angle d'ouverture (90° = grand ouvert)
    [SerializeField] private float openSpeed = 3f;    // Vitesse d'ouverture
    [SerializeField] private float closeSpeed = 2f;    // Vitesse de fermeture
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // Axe de rotation (Y = vertical)
    [SerializeField] private bool invertDirection = false;       // Inverse le sens d'ouverture

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
    private Transform player;

    void Start()
    {
        Init();
    }

    /// Appelé par le MapGenerator au lieu de Start() pour configurer les paramètres
    public void SetupFromGenerator(float openAngle, float openSpd, float closeSpd)
    {
        this.openAngle = openAngle;
        this.openSpeed = openSpd;
        this.closeSpeed = closeSpd;
        Init();
    }

    void Init()
    {
        closedRotation = transform.localRotation;

        float dir = invertDirection ? -1f : 1f;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle * dir, rotationAxis);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("DoorRotate : Aucun joueur trouvé avec le tag 'Player' !");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (openSound != null || closeSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= openDistance && !isOpen)
            Open();
        else if (dist > openDistance && isOpen)
            Close();

        // Animation de rotation
        if (isMoving)
        {
            Quaternion target = isOpen ? openRotation : closedRotation;
            float speed = isOpen ? openSpeed : closeSpeed;

            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation, target, speed * 60f * Time.deltaTime
            );

            // Arrivé à destination
            if (Quaternion.Angle(transform.localRotation, target) < 0.5f)
            {
                transform.localRotation = target;
                isMoving = false;
            }
        }
    }

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
        PlaySound(closeSound);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    void OnDrawGizmosSelected()
    {
        // Zone de détection
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, openDistance);
    }
}