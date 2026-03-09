using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Unity.AI.Navigation;

public class NavMeshBaker : MonoBehaviour
{
    private NavMeshSurface surface;

    [Header("Paramètres")]
    [SerializeField] private float bakeDelay = 0.5f;
    [SerializeField] private float bakeTimeout = 8f;
    [SerializeField] private bool logBakeTime = true;

    // Point de référence pour vérifier que le NavMesh est actif
    // (sera défini par MapGenerator au centre de la map)
    [HideInInspector] public Vector3 navMeshCheckPoint = Vector3.zero;

    void Awake()
    {
        surface = GetComponent<NavMeshSurface>();
        if (surface == null)
            Debug.LogError("NavMeshBaker : NavMeshSurface manquant !");
    }

    public IEnumerator BakeNavMeshRoutine()
    {
        yield return new WaitForSeconds(bakeDelay);
        yield return null;
        yield return null;

        if (surface == null)
        {
            Debug.LogError("NavMeshBaker : surface est null, bake annulé !");
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;
        surface.BuildNavMesh();
        float ms = (Time.realtimeSinceStartup - startTime) * 1000f;

        if (logBakeTime)
            Debug.Log($"NavMesh baked en {ms:F1}ms — attente confirmation au point {navMeshCheckPoint}...");

        // Attend que NavMesh soit queryable AU CENTRE DE LA MAP (pas à Vector3.zero)
        float elapsed = 0f;
        NavMeshHit hit;
        while (elapsed < bakeTimeout)
        {
            if (NavMesh.SamplePosition(navMeshCheckPoint, out hit, 50f, NavMesh.AllAreas))
            {
                Debug.Log($"NavMesh confirmé actif (+{elapsed * 1000f:F0}ms) !");
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"NavMeshBaker : timeout — NavMesh pas détecté autour de {navMeshCheckPoint}. Vérifie le NavMeshSurface !");
    }
}