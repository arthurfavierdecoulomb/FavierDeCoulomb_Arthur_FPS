using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float launchForce = 20f;
    [SerializeField] private float damage = 50f;

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Visuals")]
    [SerializeField] private float bulletScale = 1f;

    private Rigidbody rb;
    private bool hasHit = false;
    private WeaponController weaponController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = transform.forward * launchForce;
    }

    void Update()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer);
        if (colliders.Length > 0)
        {
            weaponController?.ReloadBullet();
            Debug.Log("Balle recuperee");
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        hasHit = true;

        Debug.Log("Balle a touche : " + collision.gameObject.name);

        ZombieEnemy zombie = collision.gameObject.GetComponentInParent<ZombieEnemy>();
        if (zombie != null)
            zombie.TakeDamage((int)damage);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetWeaponController(WeaponController controller)
    {
        weaponController = controller;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}