using UnityEngine;
using System.Collections;
using Unity.AI.Navigation;

/// <summary>
/// Cuit le NavMesh automatiquement après la génération de la map.
/// Compatible Unity 6 (6000.x) — package AI Navigation intégré.
///
/// SETUP :
/// 1. Sur le même GameObject que MapGenerator, ajoute :
///    - Ce script (NavMeshBaker)
///    - NavMeshSurface (Add Component ? Navigation ? NavMesh Surface)
/// 2. Dans NavMeshSurface :
///    - Collect Objects : All Game Objects
///    - Use Geometry    : Physics Colliders
///    - Default Area    : Walkable
/// 3. Lance le jeu — le NavMesh se cuit automatiquement après la map !
/// </summary>
public class NavMeshBaker : MonoBehaviour
{
    private NavMeshSurface surface;

    [Header("Paramètres")]
    [Tooltip("Délai en secondes pour laisser tous les Instantiate() se terminer")]
    [SerializeField] private float bakeDelay = 0.2f;
    [SerializeField] private bool logBakeTime = true;

    void Awake()
    {
        surface = GetComponent<NavMeshSurface>();

        if (surface == null)
            Debug.LogError(
                "NavMeshBaker : NavMeshSurface manquant !\n" +
                "? Add Component ? Navigation ? NavMesh Surface\n" +
                "? sur le même GameObject que MapGenerator"
            );
    }

    /// <summary>
    /// Appelé par MapGenerator.BakeNavMeshIfPresent() après la génération.
    /// </summary>
    public void BakeNavMesh()
    {
        StartCoroutine(BakeRoutine());
    }

    IEnumerator BakeRoutine()
    {
        // Attendre que tous les objets soient bien instanciés dans la scène
        yield return new WaitForSeconds(bakeDelay);

        if (surface == null)
        {
            Debug.LogError("NavMeshBaker : surface est null, bake annulé !");
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;

        // Reconstruit le NavMesh complet sur la géométrie actuelle
        surface.BuildNavMesh();

        float ms = (Time.realtimeSinceStartup - startTime) * 1000f;

        if (logBakeTime)
            Debug.Log($"NavMesh cuit en {ms:F1}ms — Les zombies peuvent naviguer !");
    }
}