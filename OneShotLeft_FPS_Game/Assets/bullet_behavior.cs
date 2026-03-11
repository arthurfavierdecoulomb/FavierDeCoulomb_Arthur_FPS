using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float launchForce = 20f;                               // Force de lancement de la balle à l'instantiation
    [SerializeField] private float damage = 50f;                                    // Dégâts infligés — deux tirs suffisent pour tuer un zombie de base

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1.5f;                             // Rayon de détection pour récupérer la balle au sol
    [SerializeField] private LayerMask playerLayer;                                 // Layer du joueur pour le OverlapSphere de pickup

    [Header("Visuals")]
    [SerializeField] private float bulletScale = 1f;                                // Taille visuelle de la balle, ajustable dans l'Inspector

    private Rigidbody rb;                                                           // Rigidbody de la balle pour lui appliquer une vélocité
    private bool hasHit = false;                                                    // Empêche la balle de déclencher OnCollisionEnter plusieurs fois
    private WeaponController weaponController;                                      // Référence au WeaponController pour signaler le pickup

    void Start()
    {
        rb = GetComponent<Rigidbody>();                                             // Récupère le Rigidbody pour contrôler le mouvement physique
        if (rb != null)
            rb.linearVelocity = transform.forward * launchForce;                   // Lance la balle vers l'avant au moment de son apparition
    }

    void Update()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer); // Détecte le joueur dans le rayon de pickup
        if (colliders.Length > 0)
        {
            weaponController?.ReloadBullet();                                       // Recharge l'arme si le WeaponController est assigné
            Debug.Log("Balle recuperee");                                           // Log de confirmation dans la console
            Destroy(gameObject);                                                    // Supprime la balle une fois récupérée
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;                                                         // Ignore les collisions suivantes si la balle a déjà touché quelque chose
        hasHit = true;                                                              // Marque la balle comme ayant déjà impacté

        Debug.Log("Balle a touche : " + collision.gameObject.name);                // M'avetit que la balle a touché quelque chose, avec le nom de l'objet pour faciliter le debug

        ZombieEnemy zombie = collision.gameObject.GetComponentInParent<ZombieEnemy>(); // Cherche un ZombieEnemy sur l'objet ou ses parents
        if (zombie != null)
        {
            zombie.TakeDamage((int)damage);                                        // Inflige les dégâts au zombie touché
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;                                      // Stoppe le déplacement de la balle après l'impact
            rb.angularVelocity = Vector3.zero;                                     // Stoppe aussi la rotation pour un arrêt propre
        }
    }

    public void SetWeaponController(WeaponController controller)
    {
        weaponController = controller;                                              // Appelé par le WeaponController lors de l'instantiation de la balle
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;                                                 // Couleur verte pour distinguer le rayon de pickup dans la Scene View
        Gizmos.DrawWireSphere(transform.position, pickupRadius);                   // Visualise le rayon de détection autour de la balle
    }
}