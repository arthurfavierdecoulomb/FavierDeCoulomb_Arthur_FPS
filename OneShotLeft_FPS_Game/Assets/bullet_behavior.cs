using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float launchForce = 20f;
    [SerializeField] private float damage = 50f;  // Dégâts de la balle, ajustable dans l'inspecteur, deux tires suffisent pour tuer un zombie de base

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1.5f; // Rayon de détection pour le pickup de la balle
    [SerializeField] private LayerMask playerLayer;

    [Header("Visuals")]
    [SerializeField] private float bulletScale = 1f; // Permet d'ajuster la taille de la balle dans l'inspecteur pour une meilleure visibilité

    private Rigidbody rb;
    private bool hasHit = false;
    private WeaponController weaponController;

    void Start()
    {
        // Ajuste la taille de la balle pour la rendre plus visible
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = transform.forward * launchForce;
    }

    void Update()
    {
        // Le pickup fonctionne toujours, que la balle ait touché quelque chose ou non
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

        // Vérifie si l'objet touché est un zombie
        ZombieEnemy zombie = collision.gameObject.GetComponentInParent<ZombieEnemy>();
        if (zombie != null)
        {
            zombie.TakeDamage((int)damage);
            // La balle est détruite immédiatement après avoir touché un zombie
        }

        // Arrête la balle pour éviter qu'elle ne continue à se déplacer après l'impact
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetWeaponController(WeaponController controller)
    {
        // Permet au WeaponController de s'assigner lui-même à la balle lors de son instantiation
        weaponController = controller;
    }

    void OnDrawGizmosSelected()
    {
        // Affiche une sphère verte dans l'éditeur pour visualiser le rayon de pickup de la balle
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}