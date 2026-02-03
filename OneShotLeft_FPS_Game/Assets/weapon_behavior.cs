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

    private float nextFireTime;

    void Start()
    {
        if (fpsCamera == null)
            fpsCamera = Camera.main;

        currentBullets = maxBullets;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryShoot();
        }
    }

    void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        // Vérifie s'il reste des balles
        if (currentBullets <= 0)
        {
            Debug.Log("Plus de balles ! Recupere ta balle pour recharger");
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
        else
        {
            Destroy(myExplosion, 2f);
        }

        if (muzzleFlash != null)
            muzzleFlash.Play();

        // Tire la balle physique
        ShootBullet();

        currentBullets--;
        Debug.Log("Tir ! Balles restantes: " + currentBullets);
    }

    void ShootBullet()
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("Bullet Prefab non assigne !");
            return;
        }

        // Détermine la position de spawn
        Vector3 spawnPos = (bulletSpawnPoint != null) ? bulletSpawnPoint.position : fpsCamera.transform.position;
        Quaternion spawnRot = fpsCamera.transform.rotation;

        // Crée la balle
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, spawnRot);

        // Donne la référence de l'arme à la balle
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.SetWeaponController(this);
        }
    }

    // Appelé par la balle quand elle est récupérée
    public void ReloadBullet()
    {
        currentBullets = maxBullets;
        Debug.Log("Balle rechargee !");

        Debug.Log("Reload sur arme : " + gameObject.name);
    }

    void LateUpdate()
    {
        if (handTransform == null) return;

        transform.position =
            handTransform.position +
            handTransform.rotation * positionOffset;

        transform.rotation =
            handTransform.rotation * Quaternion.Euler(rotationOffset);
    }

    public int GetCurrentBullets()
    {
        return currentBullets;
    }

}