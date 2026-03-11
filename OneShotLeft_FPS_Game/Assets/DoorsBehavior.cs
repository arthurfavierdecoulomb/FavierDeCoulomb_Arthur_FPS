using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorZone : MonoBehaviour                                               // Gère l'ouverture/fermeture automatique d'une porte par rotation
{
    [Header("Rotation")]
    [Tooltip("Angle d'ouverture de la porte en degrés")]
    [SerializeField] private float openAngle = 90f;                               // Amplitude de la rotation à l'ouverture
    [Tooltip("Vitesse de rotation à l'ouverture")]
    [SerializeField] private float openSpeed = 3f;                                // Plus élevé = porte qui s'ouvre plus vite
    [Tooltip("Vitesse de rotation à la fermeture")]
    [SerializeField] private float closeSpeed = 2f;                                // Légèrement plus lent que l'ouverture pour un effet naturel
    [Tooltip("Axe autour duquel la porte pivote — Vector3.up pour une porte classique")]
    [SerializeField] private Vector3 rotationAxis = Vector3.up;                  // Axe de rotation — Y pour une porte verticale standard
    [Tooltip("Inverse le sens d'ouverture de la porte")]
    [SerializeField] private bool invertDirection = false;                       // Utile si la porte s'ouvre du mauvais côté

    [Header("Comportement")]
    [Tooltip("Délai en secondes avant que la porte se referme après que le déclencheur soit sorti")]
    [SerializeField] private float closeDelay = 1.5f;                         // Laisse le temps au joueur de passer avant que la porte se ferme
    [Tooltip("Profondeur de la zone de détection devant et derrière la porte")]
    [SerializeField] private float detectionDepth = 2.5f;                         // Taille du trigger en profondeur
    [Tooltip("Largeur de la zone de détection")]
    [SerializeField] private float detectionWidth = 2f;                           // Taille du trigger en largeur

    [Header("Zombies")]
    [Tooltip("Autorise les zombies à ouvrir cette porte")]
    [SerializeField] private bool zombiesCanOpen = true;                         // Si faux, seul le joueur peut ouvrir la porte
    [Tooltip("Tag Unity utilisé pour identifier les zombies")]
    [SerializeField] private string zombieTag = "Zombie";                     // Doit correspondre au tag assigné sur les prefabs zombie

    [Header("Sons")]
    [Tooltip("Son joué quand la porte s'ouvre")]
    [SerializeField] private AudioClip openSound;                                  // Clip audio d'ouverture
    [Tooltip("Son joué quand la porte se ferme")]
    [SerializeField] private AudioClip closeSound;                                 // Clip audio de fermeture

    [Header("Atténuation par distance")]
    [Tooltip("Distance en dessous de laquelle le volume est maximal")]
    [SerializeField] private float minDistance = 3f;                               // En dessous : volume plein
    [Tooltip("Distance au-delà de laquelle le son est inaudible")]
    [SerializeField] private float maxDistance = 20f;                              // Au-delà : son non joué
    [Tooltip("Volume maximal quand le joueur est tout proche")]
    [SerializeField][Range(0f, 1f)] private float maxVolume = 1f;                 // Volume cible à distance minimale

    private AudioSource audioSource;                                               // AudioSource pour jouer les sons d'ouverture/fermeture
    private Transform playerTransform;                                           // Transform du joueur — utilisé pour calculer la distance au son

    private Quaternion closedRotation;                                              // Rotation de repos de la porte (fermée)
    private Quaternion openRotation;                                                // Rotation cible quand la porte est ouverte

    private bool isOpen = false;                                                // Vrai si la porte est en état ouvert
    private bool isMoving = false;                                                // Vrai si la porte est en cours de rotation
    private int inZoneCount = 0;                                                  // Nombre d'entités actuellement dans la zone de détection
    private float closeTimer = -1f;                                                // Timer du délai avant fermeture (-1 = inactif)

    void Start()
    {
        Init();                                                                     // Calcule les rotations et initialise l'AudioSource

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            box.isTrigger = true;                                                   // La porte détecte les entrées sans bloquer physiquement
            box.size = new Vector3(detectionWidth, box.size.y > 0 ? box.size.y : 2f, detectionDepth * 2f); // Redimensionne le trigger selon les paramètres
            box.center = Vector3.zero;
        }
        else
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;                                 // Fallback si ce n'est pas un BoxCollider
        }

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) playerTransform = playerGO.transform;                // Trouve le joueur une seule fois au démarrage
    }

    public void SetupFromGenerator(float angle, float openSpd, float closeSpd, float delay = 0f)
    {
        openAngle = angle;                                                         // Permet au MapGenerator de configurer la porte à la génération
        openSpeed = openSpd;
        closeSpeed = closeSpd;
        closeDelay = delay;
        Init();                                                                     // Recalcule les rotations avec les nouvelles valeurs
    }

    void Init()
    {
        closedRotation = transform.localRotation;                                   // Mémorise la rotation initiale comme position fermée
        float dir = invertDirection ? -1f : 1f;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle * dir, rotationAxis); // Calcule la rotation ouverte selon l'axe et la direction

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (openSound != null || closeSound != null))
            audioSource = gameObject.AddComponent<AudioSource>();                   // Crée l'AudioSource uniquement si des sons sont assignés

        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f;                                          // Volume géré manuellement par GetVolumeByDistance — pas d'atténuation Unity
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (closeTimer >= 0f)
        {
            closeTimer -= Time.deltaTime;                                           // Décompte du délai avant fermeture
            if (closeTimer < 0f) Close();                                          // Déclenche la fermeture quand le timer atteint zéro
        }

        if (!isMoving) return;                                                      // Ne calcule la rotation que si la porte bouge

        Quaternion target = isOpen ? openRotation : closedRotation;                // Cible selon l'état actuel
        float speed = isOpen ? openSpeed : closeSpeed;

        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation, target, speed * 60f * Time.deltaTime         // RotateTowards pour une vitesse constante en degrés/seconde
        );

        if (Quaternion.Angle(transform.localRotation, target) < 0.5f)
        {
            transform.localRotation = target;                                       // Snap final pour éviter une oscillation infinie
            isMoving = false;                                                       // La porte a atteint sa position cible
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsAllowed(other)) return;                                              // Ignore les colliders non autorisés (murs, objets, etc.)
        inZoneCount++;                                                              // Incrémente le compteur d'entités dans la zone
        closeTimer = -1f;                                                           // Annule le délai de fermeture si quelqu'un entre
        if (!isOpen) Open();                                                        // Ouvre uniquement si la porte est fermée
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsAllowed(other)) return;                                              // Ignore les sorties non autorisées
        inZoneCount = Mathf.Max(0, inZoneCount - 1);                              // Décrémente sans passer en négatif
        if (inZoneCount == 0)
        {
            if (closeDelay > 0f) closeTimer = closeDelay;                          // Lance le délai de fermeture si configuré
            else Close();                                                           // Ferme immédiatement si pas de délai
        }
    }

    bool IsAllowed(Collider other)
    {
        if (other.CompareTag("Player")) return true;                               // Le joueur ouvre toujours la porte
        if (zombiesCanOpen && other.CompareTag(zombieTag)) return true;            // Les zombies ouvrent si l'option est activée
        return false;                                                               // Tout autre objet est ignoré
    }

    void Open()
    {
        isOpen = true;                                                            // Passe en état ouvert
        isMoving = true;                                                            // Lance la rotation
        PlaySound(openSound);                                                       // Joue le son d'ouverture
    }

    void Close()
    {
        isOpen = false;                                                         // Passe en état fermé
        isMoving = true;                                                          // Lance la rotation inverse
        closeTimer = -1f;                                                           // Désactive le timer
        PlaySound(closeSound);                                                      // Joue le son de fermeture
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;                           // Sécurité : ne fait rien si les références manquent
        float vol = GetVolumeByDistance();
        if (vol <= 0f) return;                                                      // N'instancie pas le son si le joueur est trop loin
        audioSource.PlayOneShot(clip, vol);                                        // PlayOneShot — permet à ouverture et fermeture de se superposer
    }

    float GetVolumeByDistance()
    {
        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) playerTransform = playerGO.transform;            // Cherche le joueur en dernier recours si non trouvé au Start
            else return maxVolume;                                                   // Pas de joueur dans la scène : joue au volume max par défaut
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position); // Distance entre la porte et le joueur

        if (dist <= minDistance) return maxVolume;                                 // Trop proche : volume maximum
        if (dist >= maxDistance) return 0f;                                        // Trop loin : son inaudible

        float t = 1f - Mathf.InverseLerp(minDistance, maxDistance, dist);
        return Mathf.Lerp(0f, maxVolume, t);                                       // Interpolation linéaire du volume entre les deux distances
    }

    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.35f);                        // Vert translucide pour la zone de détection
            if (col is BoxCollider box)
            {
                Matrix4x4 prev = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(
                    transform.TransformPoint(box.center),
                    transform.rotation,
                    transform.lossyScale
                );
                Gizmos.DrawCube(Vector3.zero, box.size);                           // Volume plein semi-transparent
                Gizmos.DrawWireCube(Vector3.zero, box.size);                       // Contour pour mieux voir les bords
                Gizmos.matrix = prev;
            }
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, minDistance);                    // Cercle jaune = distance de volume maximal

        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, maxDistance);                    // Cercle orange = limite d'audibilité du son
    }
}