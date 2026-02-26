using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float launchForce = 20f;
    [SerializeField] private float damage = 25f;

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

        // Lance la balle vers l'avant
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * launchForce;
        }
    }

    void Update()
    {
        // Vérifie si le joueur est proche pour récupérer la balle
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer);

        if (colliders.Length > 0)
        {
            // Récupération de la balle
            if (weaponController != null)
            {
                weaponController.ReloadBullet();
                Debug.Log("Balle recuperee");
            }
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;
        Debug.Log("Balle a touche: " + collision.gameObject.name);

    }

    public void SetWeaponController(WeaponController controller)
    {
        weaponController = controller;
    }

    void OnDrawGizmosSelected()
    {
        // Visualise la zone de pickup
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}