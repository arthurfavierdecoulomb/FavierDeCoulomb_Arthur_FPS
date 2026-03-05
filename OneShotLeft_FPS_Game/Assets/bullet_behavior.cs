using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float launchForce = 20f;
    [SerializeField] private float damage = 9999f;  // one shot

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
        if (hasHit) return;

        // Récupération de la balle par le joueur
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

        // Cherche le ZombieEnemy sur le GO touché ou ses parents
        ZombieEnemy zombie = collision.gameObject.GetComponentInParent<ZombieEnemy>();
        if (zombie != null)
        {
            zombie.TakeDamage((int)damage);    // one shot
            // La balle continue de tomber pour être récupérée
        }

        // Pas un zombie : la balle reste au sol pour être récupérée
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