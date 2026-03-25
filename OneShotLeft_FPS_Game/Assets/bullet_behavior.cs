using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Physics")]
<<<<<<< Updated upstream
    [SerializeField] private float launchForce = 20f;                               // Force de lancement de la balle à l'instantiation
    [SerializeField] private float damage = 50f;                                    // Dégâts infligés — deux tirs suffisent pour tuer un zombie de base

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1.5f;                             // Rayon de détection pour récupérer la balle au sol
    [SerializeField] private LayerMask playerLayer;                                 // Layer du joueur pour le OverlapSphere de pickup
=======
    [SerializeField] private float launchForce = 20f;
    [SerializeField] private float damage = 50f;  // deux tires pour tuer un zombie

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1.5f; // ajustement dans l'inspecteur pour trouver le bon rayon de pickup
    [SerializeField] private LayerMask playerLayer;
>>>>>>> Stashed changes

    [Header("Visuals")]
    [SerializeField] private float bulletScale = 1f;                                // Taille visuelle de la balle, ajustable dans l'Inspector

    private Rigidbody rb;                                                           // Rigidbody de la balle pour lui appliquer une vélocité
    private bool hasHit = false;                                                    // Empêche la balle de déclencher OnCollisionEnter plusieurs fois
    private WeaponController weaponController;                                      // Référence au WeaponController pour signaler le pickup

    void Start()
    {
<<<<<<< Updated upstream
        rb = GetComponent<Rigidbody>();                                             // Récupère le Rigidbody pour contrôler le mouvement physique
=======
        // Applique une échelle uniforme à la balle
        rb = GetComponent<Rigidbody>();
>>>>>>> Stashed changes
        if (rb != null)
            rb.linearVelocity = transform.forward * launchForce;                   // Lance la balle vers l'avant au moment de son apparition
    }

    void Update()
    {
<<<<<<< Updated upstream
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer); // Détecte le joueur dans le rayon de pickup
        if (colliders.Length > 0)
        {
            weaponController?.ReloadBullet();                                       // Recharge l'arme si le WeaponController est assigné
            Debug.Log("Balle recuperee");                                           // Log de confirmation dans la console
            Destroy(gameObject);                                                    // Supprime la balle une fois récupérée
=======
        // Vérifie si la balle est au sol et peut être ramassée
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius, playerLayer);
        if (colliders.Length > 0)
        {
            // Ramasse la balle et recharge le pistolet
            weaponController?.ReloadBullet();
            Debug.Log("Balle recuperee");
            Destroy(gameObject);
>>>>>>> Stashed changes
        }
    }

    void OnCollisionEnter(Collision collision)
    {
<<<<<<< Updated upstream
        if (hasHit) return;                                                         // Ignore les collisions suivantes si la balle a déjà touché quelque chose
        hasHit = true;                                                              // Marque la balle comme ayant déjà impacté
=======
        // Empêche les collisions multiples
        if (hasHit) return;
        hasHit = true;
>>>>>>> Stashed changes

        Debug.Log("Balle a touche : " + collision.gameObject.name);                // M'avetit que la balle a touché quelque chose, avec le nom de l'objet pour faciliter le debug

<<<<<<< Updated upstream
        ZombieEnemy zombie = collision.gameObject.GetComponentInParent<ZombieEnemy>(); // Cherche un ZombieEnemy sur l'objet ou ses parents
        if (zombie != null)
        {
            zombie.TakeDamage((int)damage);                                        // Inflige les dégâts au zombie touché
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;                                      // Stoppe le déplacement de la balle après l'impact
            rb.angularVelocity = Vector3.zero;                                     // Stoppe aussi la rotation pour un arrêt propre
=======
        // Vérifie si la balle a touché un zombie
        ZombieEnemy zombie = collision.gameObject.GetComponentInParent<ZombieEnemy>();
        if (zombie != null)
        {
            // Applique les dégâts au zombie
            zombie.TakeDamage((int)damage);   
            
        }

        // Arrête la balle après la collision
        if (rb != null)
        {
            // Désactive la physique pour que la balle reste en place après l'impact
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
>>>>>>> Stashed changes
        }
    }

    public void SetWeaponController(WeaponController controller)
    {
<<<<<<< Updated upstream
        weaponController = controller;                                              // Appelé par le WeaponController lors de l'instantiation de la balle
=======
        // Permet au pistolet de référencer le contrôleur d'armes pour recharger les balles
        weaponController = controller;
>>>>>>> Stashed changes
    }

    void OnDrawGizmosSelected()
    {
<<<<<<< Updated upstream
        Gizmos.color = Color.green;                                                 // Couleur verte pour distinguer le rayon de pickup dans la Scene View
        Gizmos.DrawWireSphere(transform.position, pickupRadius);                   // Visualise le rayon de détection autour de la balle
=======
        // Affiche le rayon de pickup dans l'éditeur pour faciliter le réglage
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
>>>>>>> Stashed changes
    }
}