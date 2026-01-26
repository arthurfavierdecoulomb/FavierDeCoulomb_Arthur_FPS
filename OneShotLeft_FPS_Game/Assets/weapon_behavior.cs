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

    [Header("Local Offset")]
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private Vector3 rotationOffset;

    [Header("FX")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private Transform firePoint;

    private float nextFireTime;

    void Start()
    {
        if (fpsCamera == null)
            fpsCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            TryShoot();
        }
    }

    void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + fireRate;
        Shoot();
    }

    void Shoot()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();

        Vector3 origin = fpsCamera.transform.position;
        Vector3 direction = fpsCamera.transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, range))
        {
            Debug.DrawLine(origin, hit.point, Color.red, 0.2f);
        }
        else
        {
            Debug.DrawLine(origin, origin + direction * range, Color.yellow, 0.2f);
        }
    }

    void LateUpdate()
    {
        if (handTransform == null) return;

        // Copier position + rotation (world space)
        transform.position =
            handTransform.position +
            handTransform.rotation * positionOffset;

        transform.rotation =
            handTransform.rotation * Quaternion.Euler(rotationOffset);
    }
}