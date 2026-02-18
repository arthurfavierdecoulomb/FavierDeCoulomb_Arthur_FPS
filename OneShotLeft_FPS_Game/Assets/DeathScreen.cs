using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject deathScreenPanel;
    public Image darkOverlay;
    public TextMeshProUGUI mainTitleText;
    public TextMeshProUGUI motivationalText;
    public Button respawnButton;
    public Button quitButton;

    [Header("Game UI to Hide")]
    public GameObject gameUIPanel;

    [Header("Player Reference")]
    private PlayerHealth playerHealth;

    [Header("Animation Settings")]
    [SerializeField] private float titleShakeDuration = 0.4f;
    [SerializeField] private float titleShakeIntensity = 15f;
    [SerializeField] private float slideDuration = 0.7f;
    [SerializeField] private float slideDistance = 200f;
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("Final Positions (Y)")]
    [SerializeField] private float titleFinalYPosition = 150f;
    [SerializeField] private float motivationalFinalYPosition = 0f;
    [SerializeField] private float buttonFinalYPosition = -130f; // Position Y commune aux deux boutons

    [Header("Boutons - Alignement horizontal")]
    [SerializeField] private float buttonSpacing = 120f; // Distance entre les deux boutons sur l'axe X

    [Header("Messages aléatoires")]
    private string[] deathTitles = new string[]
    {
        "Dommage...",
        "Tu es mort !",
        "Mort subite !",
        "Oof...",
        "Adieu.",
        "Echec.",
        "Au-revoir..."
    };

    private string[] motivationalMessages = new string[]
    {
        "Chef, il est mort comme une merde",
        "Tu aurais pu faire mieux...",
        "Je m'y attendais pas",
        "Pathétique, non je rigole...",
        "L'intention y est, c'est déjà ça !",
        "Nooon, pas toi, pas aujourd'hui ! Pas après tout ce que tu as fait",
        "Je mettrais une étoile pour l'effort... les quatre autres ? Bah...",
        "Je mettrais pas ça sur ton rapport de performance.",
        "Je dirais que tu as fait de ton mieux, mais je mentirais.",
        "Je ne sais pas quoi dire, c'est tellement triste... tu as tellement de potentiel, et pourtant... tu as échoué.",
        "Je suis déçu, mais je ne perds pas espoir. Peut-être que la prochaine fois tu feras mieux ?",
        "Tu es, comment dire... un cas désespéré ? Non, je plaisante ! Mais sérieusement, tu devrais peut-être revoir ta stratégie ?",
        "Pff... là je ne sais pas.",
        "Pouah, t'as glissé ?",
        "Les zombies ont eu pitié de toi ?",
        "T'as pensé que les zombies font des câlins ?",
        "Tu manques de chance ou de compétence ? Ou les deux ?",
        "Ha ! ha ha ! Je suis mort de rire !",
        "Tu es tellement nul que même les zombies ont refusé de te manger !",
        "Bruh",
        "Bon, je vais être honnête, c'est pas la mort la plus impressionnante que j'ai vue, mais bon, tu as réussi à mourir quand même, félicitations !",
        "Je ne sais pas si je dois être triste pour toi ou juste impressionné par ta capacité à mourir de manière aussi créative !",
        "Attends... tu es sûr que tu es mort ? Parce que franchement, ça ressemble plus à une sieste prolongée qu'à une mort héroïque !",
        "Je ne dirais rien à ta famille, mais je pense qu'ils devraient être fiers de toi... d'avoir réussi à mourir de manière aussi épique !",
        "Attends, laisse-moi appeler le Guinness des records, j'ai un truc là...",
        "Hah ! t nul !",
        "C'est finito pour toi !",
        "Même un tutoriel n'aurait pas pu te sauver là...",
        "J'ai vu des plantes en pot avec plus de réflexes que toi.",
        "Ta grand-mère jouerait mieux... et elle ne sait même pas ce qu'est un ordinateur.",
        "Les zombies vont raconter cette blague pendant des années !",
        "Tu t'es fait éliminer par un zombie qui n'avait même pas de cerveau... ironique, non ?",
        "Je crois que tu as confondu 'survivre' avec 'mourir rapidement'.",
        "Félicitations ! Tu as débloqué l'achievement : 'Comment mourir en 5 secondes'",
        "Même les PNJ se moquent de toi en coulisses.",
        "Tu devrais peut-être essayer le mode facile... ah non attends, TU ES en mode facile.",
        "Les zombies t'ont remercié pour ce repas gratuit.",
        "Je pense que ta stratégie était... comment dire... inexistante ?",
        "Tu as transformé 'survie' en 'mort volontaire'.",
        "Waouh... juste... waouh. Aucun mot.",
        "Ta mort était si rapide que j'ai même pas eu le temps de préparer du pop-corn.",
        "Les zombies ont à peine eu besoin de faire un effort.",
        "Tu as au moins essayé ? Non ? Ça se voit.",
        "Je retiens mon fou rire... difficilement.",
        "Ta tactique de combat : foncer tête baissée et espérer. Spoiler : ça marche pas.",
        "On dirait que quelqu'un a oublié qu'il était mortel.",
        "Les zombies viennent de t'ajouter à leur menu 'plat du jour'.",
        "Franchement, j'aurais parié sur toi... mais j'aurais tout perdu.",
        "Tu t'es fait avoir par un zombie qui marchait à reculons ?!",
        "Respect pour cette mort absolument catastrophique.",
        "T'as essayé de négocier avec un zombie ? Ça marche pas comme ça.",
        "Bon, au moins tu meurs avec style... non en fait même pas.",
        "Les zombies t'ont pris pour un snack gratuit.",
        "Tu pensais vraiment gagner avec CETTE stratégie ?",
        "Même un pigeon aurait mieux esquivé.",
        "Ta mort sera dans les annales... dans la section 'Epic Fails'.",
        "Les zombies hésitent même à te manger tellement c'était pathétique.",
        "10/10 pour l'effort, 0/10 pour l'exécution.",
        "Tu viens de perdre contre l'ennemi le plus lent du jeu. Bravo.",
        "Quelqu'un a enregistré ça ? Non ? Dommage, c'était hilarant.",
        "Ta famille vient de recevoir une notification 'Votre proche est décédé... encore.'",
        "Les développeurs pleurent en voyant comment tu joues à leur jeu.",
        "Tu devrais peut-être lire le manuel... oh attends, personne ne le fait.",
        "Même le zombie avait pitié en te tuant.",
        "C'était quoi le plan exactement ? Mourir le plus vite possible ?",
        "Les zombies ont même pas transpiré pour t'avoir.",
        "Ta performance était... comment dire... mémorable pour de mauvaises raisons.",
        "J'espère que tu as une bonne assurance vie.",
        "Le bouton 'Esquiver' existe, tu sais ?",
        "Les zombies pensaient que c'était un entraînement facile. Ils avaient raison.",
        "Statistiquement, tu aurais dû survivre. Statistiquement.",
        "Je vais devoir revoir ma définition du mot 'compétent'.",
        "Tu as réussi à transformer une victoire facile en défaite catastrophique.",
        "Les zombies vont utiliser ta mort comme exemple de 'ce qu'il ne faut pas faire'.",
        "Bravo, tu as réussi à perdre dans un tutoriel."
    };

    private string currentMessage = "";

    void Start()
    {
        HideDeathScreen();

        if (respawnButton != null)
            respawnButton.onClick.AddListener(OnRespawnClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
            Debug.LogWarning("PlayerHealth non trouvé dans la scène !");
    }

    public void ShowDeathScreen()
    {
        if (deathScreenPanel != null)
        {
            ResetUIElements();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            deathScreenPanel.SetActive(true);

            if (gameUIPanel != null)
                gameUIPanel.SetActive(false);

            if (mainTitleText != null)
            {
                int randomTitleIndex = Random.Range(0, deathTitles.Length);
                mainTitleText.text = deathTitles[randomTitleIndex];
                mainTitleText.rectTransform.anchoredPosition = new Vector2(0, 0);
            }

            if (motivationalText != null)
            {
                int randomIndex = Random.Range(0, motivationalMessages.Length);
                currentMessage = motivationalMessages[randomIndex];
                motivationalText.text = "";
                motivationalText.gameObject.SetActive(false);
            }

            if (respawnButton != null)
                respawnButton.gameObject.SetActive(false);

            if (quitButton != null)
                quitButton.gameObject.SetActive(false);

            StartCoroutine(DeathScreenAnimation());
        }
    }

    private IEnumerator DeathScreenAnimation()
    {
        // PHASE 1 : Tremblement du titre
        if (mainTitleText != null)
        {
            float elapsed = 0f;
            Vector3 centerPosition = new Vector2(0, 0);

            while (elapsed < titleShakeDuration)
            {
                float offsetX = Random.Range(-titleShakeIntensity, titleShakeIntensity);
                float offsetY = Random.Range(-titleShakeIntensity, titleShakeIntensity);
                mainTitleText.rectTransform.anchoredPosition = centerPosition + new Vector3(offsetX, offsetY, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }

            mainTitleText.rectTransform.anchoredPosition = centerPosition;
        }

        yield return new WaitForSeconds(0.15f);

        // PHASE 2 : Titre glisse vers le haut
        if (mainTitleText != null)
        {
            float elapsed = 0f;
            Vector3 startPos = new Vector2(0, 0);
            Vector3 endPos = new Vector2(0, titleFinalYPosition);

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                mainTitleText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            mainTitleText.rectTransform.anchoredPosition = endPos;
        }

        yield return new WaitForSeconds(0.1f);

        // PHASE 3 : Texte motivationnel - slide + machine à écrire
        if (motivationalText != null)
        {
            motivationalText.gameObject.SetActive(true);

            float elapsed = 0f;
            Vector3 startPos = new Vector2(0, motivationalFinalYPosition - slideDistance);
            Vector3 endPos = new Vector2(0, motivationalFinalYPosition);

            Color textColor = motivationalText.color;
            textColor.a = 0f;
            motivationalText.color = textColor;

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                motivationalText.rectTransform.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                textColor.a = t;
                motivationalText.color = textColor;
                yield return null;
            }

            motivationalText.rectTransform.anchoredPosition = endPos;
            textColor.a = 1f;
            motivationalText.color = textColor;

            motivationalText.text = "";
            for (int i = 0; i < currentMessage.Length; i++)
            {
                motivationalText.text += currentMessage[i];
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // PHASE 4 : Les deux boutons apparaissent ENSEMBLE, côte à côte sur le même Y
        if (respawnButton != null) respawnButton.gameObject.SetActive(true);
        if (quitButton != null) quitButton.gameObject.SetActive(true);

        // Respawn à gauche, Quitter à droite — même Y
        Vector3 respawnEndPos = new Vector2(-buttonSpacing, buttonFinalYPosition);
        Vector3 quitEndPos = new Vector2(buttonSpacing, buttonFinalYPosition);
        Vector3 respawnStartPos = new Vector2(-buttonSpacing, buttonFinalYPosition - slideDistance);
        Vector3 quitStartPos = new Vector2(buttonSpacing, buttonFinalYPosition - slideDistance);

        CanvasGroup respawnCG = respawnButton != null
            ? respawnButton.GetComponent<CanvasGroup>() ?? respawnButton.gameObject.AddComponent<CanvasGroup>()
            : null;
        CanvasGroup quitCG = quitButton != null
            ? quitButton.GetComponent<CanvasGroup>() ?? quitButton.gameObject.AddComponent<CanvasGroup>()
            : null;

        if (respawnCG != null) respawnCG.alpha = 0f;
        if (quitCG != null) quitCG.alpha = 0f;

        float btnElapsed = 0f;
        while (btnElapsed < slideDuration)
        {
            btnElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(btnElapsed / slideDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);

            if (respawnButton != null)
            {
                respawnButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(respawnStartPos, respawnEndPos, t);
                if (respawnCG != null) respawnCG.alpha = t;
            }
            if (quitButton != null)
            {
                quitButton.GetComponent<RectTransform>().anchoredPosition = Vector3.Lerp(quitStartPos, quitEndPos, t);
                if (quitCG != null) quitCG.alpha = t;
            }

            yield return null;
        }

        // Position finale exacte
        if (respawnButton != null)
        {
            respawnButton.GetComponent<RectTransform>().anchoredPosition = respawnEndPos;
            if (respawnCG != null) respawnCG.alpha = 1f;
        }
        if (quitButton != null)
        {
            quitButton.GetComponent<RectTransform>().anchoredPosition = quitEndPos;
            if (quitCG != null) quitCG.alpha = 1f;
        }
    }

    public void HideDeathScreen()
    {
        Debug.Log("HideDeathScreen appelé !");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StopAllCoroutines();

        if (respawnButton != null) respawnButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (motivationalText != null) motivationalText.gameObject.SetActive(false);
        if (mainTitleText != null) mainTitleText.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);

        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(false);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);
    }

    private void ResetUIElements()
    {
        if (mainTitleText != null)
        {
            mainTitleText.gameObject.SetActive(true);
            mainTitleText.rectTransform.anchoredPosition = new Vector2(0, 0);
        }
        if (motivationalText != null)
        {
            motivationalText.gameObject.SetActive(false);
            motivationalText.text = "";
        }
        if (respawnButton != null) respawnButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);
        if (darkOverlay != null) darkOverlay.gameObject.SetActive(true);
    }

    void OnRespawnClicked()
    {
        HideDeathScreen();
        RespawnPlayer();
    }

    void OnQuitClicked()
    {
        Debug.Log("Quitter le jeu !");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void RespawnPlayer()
    {
        if (playerHealth != null)
            playerHealth.Respawn();
        else
            Debug.LogError("Impossible de respawn : PlayerHealth non trouvé !");
    }
}