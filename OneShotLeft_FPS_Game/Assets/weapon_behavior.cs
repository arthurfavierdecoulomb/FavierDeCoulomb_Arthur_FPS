using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera fpsCamera;
    [SerializeField] private Transform handTransform;

    [Header("Weapon Stats")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 100f;
    [SerializeField] private float fireRate = 0.3f;

    [Header("Bullet System")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private int maxBullets = 1;
    private int currentBullets = 1;

    [Header("Local Offset")]
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;

    [Header("FX")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private Vector3 explosionOffset = new Vector3(0, 0, 0.4f);

    [Header("Sons")]
    [Tooltip("Son joué à chaque tir")]
    [SerializeField] private AudioClip shootSound;
    [Tooltip("Son joué quand la balle est récupérée / rechargée")]
    [SerializeField] private AudioClip reloadSound;
    [Tooltip("Son joué quand on tire sans munitions")]
    [SerializeField] private AudioClip emptySound;
    [Tooltip("Volume général des sons de l'arme")]
    [SerializeField][Range(0f, 1f)] private float weaponVolume = 1f;

    private AudioSource audioSource;
    private float nextFireTime;

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        if (fpsCamera == null)
            fpsCamera = Camera.main;

        currentBullets = maxBullets;

        // Crée ou récupère l'AudioSource sur cet objet
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // son 2D (FPS = pas de positionnement 3D)
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryShoot();
    }

    // ─────────────────────────────────────────────────────────────────────
    void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        if (currentBullets <= 0)
        {
            PlaySound(emptySound);
            Debug.Log("Plus de balles ! Récupère ta balle pour recharger");
            return;
        }

        nextFireTime = Time.time + fireRate;

        // Effets visuels
        Vector3 spawnPosition = transform.position + transform.TransformDirection(explosionOffset);
        GameObject myExplosion = Instantiate(explosionEffect, spawnPosition, transform.rotation);
        ParticleSystem ps = myExplosion.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            Destroy(myExplosion, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else Destroy(myExplosion, 2f);

        if (muzzleFlash != null) muzzleFlash.Play();

        // Son de tir
        PlaySound(shootSound);

        ShootBullet();
        currentBullets--;
        Debug.Log("Tir ! Balles restantes : " + currentBullets);
    }

    void ShootBullet()
    {
        if (bulletPrefab == null) { Debug.LogError("Bullet Prefab non assigné !"); return; }

        Vector3 spawnPos = (bulletSpawnPoint != null) ? bulletSpawnPoint.position : fpsCamera.transform.position;
        Quaternion spawnRot = fpsCamera.transform.rotation;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.SetWeaponController(this);
    }

    // Appelé par la balle quand elle est récupérée
    public void ReloadBullet()
    {
        currentBullets = maxBullets;

        // Son de rechargement
        PlaySound(reloadSound);

        Debug.Log("Balle rechargée !");
        Debug.Log("Reload sur arme : " + gameObject.name);
    }

    // ─────────────────────────────────────────────────────────────────────
    void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip, weaponVolume);
    }

    // ─────────────────────────────────────────────────────────────────────
    void LateUpdate()
    {
        if (handTransform == null) return;
        transform.position = handTransform.position + handTransform.rotation * positionOffset;
        transform.rotation = handTransform.rotation * Quaternion.Euler(rotationOffset);
    }

    public int GetCurrentBullets() => currentBullets;
}