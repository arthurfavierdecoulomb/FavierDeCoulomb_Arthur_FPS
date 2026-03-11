using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.AI.Navigation;

// Gère la génération (bake) du NavMesh en runtime, nécessaire pour que les zombies
// puissent calculer leurs chemins de déplacement sur la carte générée de façon procédurale.
// Sans NavMesh valide, le NavMeshAgent des zombies ne peut trouver aucune destination.
public class NavMeshBaker : MonoBehaviour
{
    // ─── Références privées ───────────────────────────────────────────────

    // Composant Unity responsable du calcul et du stockage du NavMesh sur cet objet.
    private NavMeshSurface surface;


    // ─── Paramètres ───────────────────────────────────────────────────────

    [Header("Paramètres")]

    // Délai en secondes avant de lancer le bake, pour laisser la géométrie
    // de la map le temps d'être entièrement instanciée dans la scène.
    [SerializeField] private float bakeDelay = 0.5f;

    // Durée maximale en secondes accordée pour confirmer que le NavMesh est actif
    // après le bake. Au-delà, un avertissement est émis et la routine se termine.
    [SerializeField] private float bakeTimeout = 8f;

    // Si true, affiche dans la console le temps de calcul du bake en millisecondes.
    [SerializeField] private bool logBakeTime = true;


    // ─── Point de vérification ────────────────────────────────────────────

    // Point 3D utilisé pour confirmer que le NavMesh est bien interrogeable après le bake.
    // Doit être défini par MapGenerator au centre de la map générée,
    // car Vector3.zero peut se trouver hors de la surface jouable.
    [HideInInspector] public Vector3 navMeshCheckPoint = Vector3.zero;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Awake()
    {
        // Récupère le NavMeshSurface sur le même GameObject.
        surface = GetComponent<NavMeshSurface>();

        // Signale une erreur critique si le composant est absent :
        // sans lui, aucun bake n'est possible et les zombies ne pourront pas naviguer.
        if (surface == null)
            Debug.LogError("NavMeshBaker : NavMeshSurface manquant !");
    }


    // ─── Routine de bake ─────────────────────────────────────────────────

    // Coroutine principale à appeler depuis MapGenerator une fois la carte générée.
    // Calcule le NavMesh en runtime puis attend confirmation qu'il est interrogeable
    // avant de rendre la main, garantissant que les zombies peuvent naviguer dès le spawn.
    public IEnumerator BakeNavMeshRoutine()
    {
        // Attend le délai configuré pour s'assurer que tous les objets de la map
        // sont bien présents dans la scène avant que le bake ne démarre.
        yield return new WaitForSeconds(bakeDelay);

        // Deux frames supplémentaires pour laisser Unity finaliser
        // l'enregistrement des colliders et des meshes dans le moteur physique.
        yield return null;
        yield return null;

        // Sécurité : annule le bake si la surface est introuvable (cas d'erreur d'init).
        if (surface == null)
        {
            Debug.LogError("NavMeshBaker : surface est null, bake annulé !");
            yield break;
        }

        // Lance le calcul synchrone du NavMesh sur la géométrie de la scène.
        // BuildNavMesh() est bloquant : il peut causer un freeze de quelques frames
        // sur des maps complexes, ce qui est acceptable en phase de chargement.
        float startTime = Time.realtimeSinceStartup;
        surface.BuildNavMesh();

        // Mesure et affiche optionnellement le temps de calcul pour aider au tuning.
        float ms = (Time.realtimeSinceStartup - startTime) * 1000f;
        if (logBakeTime)
            Debug.Log($"NavMesh baked en {ms:F1}ms — attente confirmation au point {navMeshCheckPoint}...");

        // ── Confirmation que le NavMesh est interrogeable ─────────────────

        // BuildNavMesh() peut retourner avant que le NavMesh soit réellement
        // accessible aux requêtes de pathfinding. On attend donc qu'une
        // SamplePosition réussisse AU CENTRE DE LA MAP (navMeshCheckPoint),
        // et non à Vector3.zero qui peut être hors de la surface jouable.
        float elapsed = 0f;
        NavMeshHit hit;

        while (elapsed < bakeTimeout)
        {
            // SamplePosition cherche le point du NavMesh le plus proche de navMeshCheckPoint
            // dans un rayon de 50 unités, sur toutes les surfaces disponibles.
            // Retourne true dès que le NavMesh est prêt et actif.
            if (NavMesh.SamplePosition(navMeshCheckPoint, out hit, 50f, NavMesh.AllAreas))
            {
                Debug.Log($"NavMesh confirmé actif (+{elapsed * 1000f:F0}ms) !");

                // Le NavMesh est validé : les zombies peuvent maintenant utiliser
                // leur NavMeshAgent pour calculer des chemins vers le joueur.
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Si le timeout est atteint sans confirmation, le NavMesh est probablement
        // mal configuré (surface trop petite, géométrie non statique, couche incorrecte...).
        Debug.LogWarning($"NavMeshBaker : timeout — NavMesh pas détecté autour de {navMeshCheckPoint}. Vérifie le NavMeshSurface !");
    }
}