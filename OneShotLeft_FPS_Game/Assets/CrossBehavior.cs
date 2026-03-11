using UnityEngine;
using UnityEngine.UI;

public class ZombieHeartHUD : MonoBehaviour                                         // Affiche la vie du zombie visé via un cœur UI en visée centrale
{
    [Header("Caméra")]
    [Tooltip("Caméra utilisée pour le raycast — prend Main Camera si non assignée")]
    [SerializeField] private Camera cam;                                            // Caméra depuis laquelle part le rayon de détection

    [Header("Détection")]
    [Tooltip("Distance maximale à laquelle un zombie peut être détecté par la visée")]
    [SerializeField] private float detectionDistance = 20f;                        // Au-delà de cette distance, le cœur disparaît
    [Tooltip("Layer des zombies — le raycast ne détecte que ce layer")]
    [SerializeField] private LayerMask zombieLayer;                                 // Filtre le raycast pour ne toucher que les zombies

    [Header("UI")]
    [Tooltip("Image UI en mode Filled qui représente la vie du zombie visé")]
    [SerializeField] private Image heartFill;                                       // Image dont le fillAmount reflète la vie du zombie

    [Header("Animation")]
    [Tooltip("Vitesse de transition du cœur — plus élevé = plus réactif")]
    [SerializeField] private float fillSpeed = 5f;                                  // Contrôle la fluidité de la transition du remplissage

    private float currentFill = 0f;                                                 // Valeur actuelle interpolée du fillAmount
    private float targetFill = 0f;                                                 // Valeur cible calculée depuis la vie du zombie

    void Start()
    {
        if (cam == null) cam = Camera.main;                                         // Fallback : utilise la caméra taguée MainCamera si non assignée
        heartFill.fillAmount = 0f;                                                  // Cache le cœur au démarrage
    }

    void Update()
    {
        if (heartFill == null) return;                                              // Sécurité : ne fait rien si l'image UI n'est pas assignée

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));            // Rayon depuis le centre exact de l'écran (visée)

        ZombieEnemy zombie = null;
        if (Physics.Raycast(ray, out RaycastHit hit, detectionDistance, zombieLayer))
            zombie = hit.collider.GetComponentInParent<ZombieEnemy>();             // Cherche un ZombieEnemy sur l'objet touché ou ses parents

        bool detected = zombie != null && !zombie.IsDead();                        // Vrai uniquement si un zombie vivant est dans la ligne de visée

        targetFill = detected
            ? Mathf.Clamp01((float)zombie.GetHealth() / zombie.GetMaxHealth())     // Normalise la vie entre 0 et 1 pour le fillAmount
            : 0f;                                                                   // Cible zéro si aucun zombie visé — le cœur se vide progressivement

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * fillSpeed); // Transition fluide vers la valeur cible
        heartFill.fillAmount = currentFill;                                         // Applique la valeur interpolée à l'image UI
    }
}