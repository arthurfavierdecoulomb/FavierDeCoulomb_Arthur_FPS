using UnityEngine;

// Gère la rotation de la caméra à la souris en FPS :
// rotation verticale sur la caméra elle-même, rotation horizontale sur le corps du joueur.
public class MouseLook : MonoBehaviour
{
    // ─── Paramètres ───────────────────────────────────────────────────────

    // Sensibilité de la souris ; valeur élevée = rotation plus rapide.
    public float mouseSensitivity = 100f;

    // Référence au Transform du corps du joueur, utilisé pour la rotation horizontale.
    public Transform playerBody;


    // ─── Variables privées ────────────────────────────────────────────────

    // Accumule la rotation verticale pour permettre le clamp et éviter le retournement.
    float xRotation = 0f;


    // ─── Initialisation ───────────────────────────────────────────────────

    void Start()
    {
        // Verrouille et cache le curseur au centre de l'écran pour un contrôle FPS standard.
        Cursor.lockState = CursorLockMode.Locked;
    }


    // ─── Mise à jour ──────────────────────────────────────────────────────

    void Update()
    {
        // Récupère le delta souris cette frame, mis à l'échelle par la sensibilité et deltaTime.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Soustrait mouseY pour inverser l'axe vertical (mouvement naturel : souris vers le bas = regard vers le bas).
        xRotation -= mouseY;

        // Limite la rotation verticale entre -90° (regarder en bas) et 90° (regarder en haut).
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Applique la rotation verticale uniquement sur la caméra (axe X local).
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Applique la rotation horizontale sur le corps entier du joueur (axe Y monde).
        playerBody.Rotate(Vector3.up * mouseX);
    }
}