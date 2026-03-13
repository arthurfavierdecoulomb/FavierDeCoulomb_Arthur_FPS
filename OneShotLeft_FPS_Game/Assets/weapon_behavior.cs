using UnityEngine;

// Gère le tir, la physique de la balle unique, les effets visuels et sonores de l'arme,
// ainsi que son positionnement en main du joueur via LateUpdate.
public class WeaponController : MonoBehaviour
{
    // ─── Références ───────────────────────────────────────────────────────

    [Header("References")]

    // Caméra FPS utilisée comme origine du rayon de tir et de la balle.
    [SerializeField] private Camera fpsCamera;

    // Transform représentant la main du joueur, utilisé pour coller l'arme en LateUpdate.
    [SerializeField] private Transform handTransform;


    // ─── Statistiques de l'arme ───────────────────────────────────────────

    [Header("Weapon Stats")]

    // Dégâts infligés par chaque balle à l'impact.
    [SerializeField] private float damage = 25f;

    // Portée maximale du tir en unités Unity.
    [SerializeField] private float range = 100f;

    // Délai minimum en secondes entre deux tirs consécutifs (cadence de tir).
    [SerializeField] private float fireRate = 0.3f;


    // ─── Système de balle unique ──────────────────────────────────────────

    [Header("Bullet System")]

    // Prefab de la balle instanciée à chaque tir.
    [SerializeField] private GameObject bulletPrefab;

    // Point d'origine utilisé pour spawner la balle dans la scène.
    [SerializeField] private Transform bulletSpawnPoint;

    // Nombre maximum de balles disponibles (ici 1 : le joueur n'a qu'une seule balle).
    [SerializeField] private int maxBullets = 1;

    // Nombre de balles actuellement disponibles, décrémenté au tir et restauré par ReloadBullet().
    private int currentBullets = 1;


    // ─── Décalage local de l'arme ─────────────────────────────────────────

    [Header("Local Offset")]

    // Décalage de position de l'arme par rapport à la main du joueur.
    [SerializeField] private Vector3 positionOffset;

    // Décalage de rotation de l'arme par rapport à la main du joueur (en degrés Euler).
    [SerializeField] private Vector3 rotationOffset;


    // ─── Effets visuels ───────────────────────────────────────────────────

    [Header("FX")]

    // Système de particules joué à la bouche du canon lors d'un tir.
    [SerializeField] private ParticleSystem muzzleFlash;

    // Point de référence pour l'effet de flamme en bouche (non utilisé directement ici).
    [SerializeField] private Transform firePoint;

    // Prefab de l'effet d'explosion instancié devant l'arme à chaque tir.
    [SerializeField] private GameObject explosionEffect;

    // Décalage local de l'effet d'explosion par rapport à l'arme (devant le canon).
    [SerializeField] private Vector3 explosionOffset = new Vector3(0, 0, 0.4f);


    // ─── Sons ─────────────────────────────────────────────────────────────

    [Header("Sons")]

    // Son joué à chaque tir réussi.
    [Tooltip("Son joué à chaque tir")]
    [SerializeField] private AudioClip shootSound;

    // Son joué lorsque la balle est récupérée et l'arme rechargée.
    [Tooltip("Son joué quand la balle est récupérée / rechargée")]
    [SerializeField] private AudioClip reloadSound;

    // Son joué lorsque le joueur tente de tirer sans munitions.
    [Tooltip("Son joué quand on tire sans munitions")]
    [SerializeField] private AudioClip emptySound;

    // Volume appliqué à tous les sons de l'arme (0 = muet, 1 = plein volume).
    [Tooltip("Volume général des sons de l'arme")]
    [SerializeField][Range(0f, 1f)] private float weaponVolume = 1f;


    // ─── Variables privées ────────────────────────────────────────────────

    // Source audio 2D créée dynamiquement pour les sons de l'arme.
    private AudioSource audioSource;

    // Timestamp du prochain tir autorisé, calculé à partir de Time.time + fireRate.
    private float nextFireTime;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Start()
    {
        // Utilise la caméra principale si aucune n'est assignée dans l'Inspector.
        if (fpsCamera == null)
            fpsCamera = Camera.main;

        // Initialise les munitions au maximum au démarrage.
        currentBullets = maxBullets;

        // Récupère ou crée la source audio 2D dédiée à l'arme.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // spatialBlend = 0 : son 2D, pas de positionnement spatial (cohérent pour une arme FPS).
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Update()
    {
        // Bloque le tir pendant le chargement — le curseur est libre et peut déclencher un clic involontaire.
        if (LoadingScreen.Instance != null && LoadingScreen.Instance.gameObject.activeSelf) return;

        // Écoute le clic gauche à chaque frame et tente un tir si le bouton est pressé.
        if (Input.GetMouseButtonDown(0))
            TryShoot();
    }


    // ─── Tentative de tir ─────────────────────────────────────────────────

    void TryShoot()
    {
        // Bloque le tir si la cadence n'est pas encore écoulée.
        if (Time.time < nextFireTime) return;

        if (currentBullets <= 0)
        {
            // Joue le son "à vide" pour signaler au joueur qu'il n'a plus de munitions.
            PlaySound(emptySound);
            Debug.Log("Plus de balles ! Récupère ta balle pour recharger");
            return;
        }

        // Calcule le prochain moment où un tir sera autorisé.
        nextFireTime = Time.time + fireRate;

        // ── Effets visuels ────────────────────────────────────────────────

        // Instancie l'effet d'explosion devant le canon, dans l'espace local de l'arme.
        Vector3 spawnPosition = transform.position + transform.TransformDirection(explosionOffset);
        GameObject myExplosion = Instantiate(explosionEffect, spawnPosition, transform.rotation);

        ParticleSystem ps = myExplosion.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            // Détruit l'effet une fois toutes ses particules expirées (durée + lifetime max).
            Destroy(myExplosion, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            // Fallback : détruit l'objet après 2 secondes si aucun ParticleSystem n'est trouvé.
            Destroy(myExplosion, 2f);
        }

        // Joue le muzzle flash si assigné.
        if (muzzleFlash != null) muzzleFlash.Play();

        // ── Tir ───────────────────────────────────────────────────────────

        // Joue le son de tir.
        PlaySound(shootSound);

        // Instancie et configure la balle.
        ShootBullet();

        // Décrémente le compteur de munitions après le tir.
        currentBullets--;
        Debug.Log("Tir ! Balles restantes : " + currentBullets);
    }


    // ─── Instanciation de la balle ────────────────────────────────────────

    void ShootBullet()
    {
        if (bulletPrefab == null) { Debug.LogError("Bullet Prefab non assigné !"); return; }

        // Utilise le bulletSpawnPoint si assigné, sinon part de la caméra FPS.
        Vector3 spawnPos = (bulletSpawnPoint != null) ? bulletSpawnPoint.position : fpsCamera.transform.position;

        // La balle hérite de la rotation de la caméra pour viser dans la direction du regard.
        Quaternion spawnRot = fpsCamera.transform.rotation;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);

        // Injecte la référence à ce WeaponController dans la balle,
        // pour qu'elle puisse appeler ReloadBullet() lors de sa récupération.
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.SetWeaponController(this);
    }


    // ─── Rechargement ─────────────────────────────────────────────────────

    // Appelé par le script Bullet lorsque la balle est récupérée par le joueur.
    // Restaure les munitions au maximum et joue le son de rechargement.
    public void ReloadBullet()
    {
        currentBullets = maxBullets;

        PlaySound(reloadSound);

        Debug.Log("Balle rechargée !");
        Debug.Log("Reload sur arme : " + gameObject.name);
    }


    // ─── Son ─────────────────────────────────────────────────────────────

    // Joue un clip audio en one-shot avec le volume de l'arme défini dans l'Inspector.
    // Ne fait rien si la source ou le clip est manquant.
    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, weaponVolume);
    }


    // ─── Positionnement de l'arme ─────────────────────────────────────────

    void LateUpdate()
    {
        // LateUpdate garantit que la main du joueur a déjà été déplacée ce frame
        // avant de repositionner l'arme, évitant tout décalage visuel d'une frame.
        if (handTransform == null) return;

        // Applique le décalage de position dans l'espace local de la main.
        transform.position = handTransform.position + handTransform.rotation * positionOffset;

        // Applique le décalage de rotation par-dessus la rotation de la main.
        transform.rotation = handTransform.rotation * Quaternion.Euler(rotationOffset);
    }


    // ─── Getter public ────────────────────────────────────────────────────

    // Retourne le nombre de balles restantes, utilisé par TypewriterWarning et StaminaUI.
    public int GetCurrentBullets() => currentBullets;
}