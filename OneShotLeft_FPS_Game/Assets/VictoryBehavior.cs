using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class VictoryScreen : MonoBehaviour
{
    [Header("UI References")]
    public GameObject victoryScreenPanel;
    public Image darkOverlay;
    public TextMeshProUGUI mainTitleText;
    public TextMeshProUGUI motivationalText;
    public Button replayButton;
    public Button quitButton;

    [Header("Game UI to Hide")]
    public GameObject gameUIPanel;

    [Header("Animation Settings")]
    [SerializeField] private float titleShakeDuration = 0.4f;
    [SerializeField] private float titleShakeIntensity = 10f;
    [SerializeField] private float slideDuration = 0.7f;
    [SerializeField] private float slideDistance = 200f;
    [SerializeField] private float typewriterSpeed = 0.03f;

    [Header("Final Positions (Y)")]
    [SerializeField] private float titleFinalYPosition = 150f;
    [SerializeField] private float motivationalFinalYPosition = 0f;
    [SerializeField] private float buttonFinalYPosition = -130f;

    [Header("Boutons - Alignement horizontal")]
    [SerializeField] private float buttonSpacing = 120f;

    private string[] victoryTitles =
    {
        "Victoire !",
        "Mission accomplie !",
        "Bravo !",
        "Succès total !",
        "Objectif atteint !"
    };

    private string[] victoryMessages =
    {
        "Les zombies ne s’en remettront pas.",
        "Franchement... c’était propre.",
        "Mission réussie chef.",
        "Ils n’ont rien compris à ce qui leur est arrivé.",
        "C'était presque trop facile.",
        "On peut dire que tu as géré."
    };

    private string currentMessage = "";

    void Start()
    {
        HideVictoryScreen();

        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void ShowVictoryScreen()
    {
        if (victoryScreenPanel == null) return;

        StopAllCoroutines();

        string chosenTitle = victoryTitles[Random.Range(0, victoryTitles.Length)];
        currentMessage = victoryMessages[Random.Range(0, victoryMessages.Length)];

        ResetUIElements();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        victoryScreenPanel.SetActive(true);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(false);

        if (mainTitleText != null)
        {
            mainTitleText.text = chosenTitle;
            mainTitleText.rectTransform.anchoredPosition = new Vector2(0, 0);
        }

        StartCoroutine(VictoryScreenAnimation());
    }

    private IEnumerator VictoryScreenAnimation()
    {
        // Tremblement du titre
        if (mainTitleText != null)
        {
            float elapsed = 0f;
            Vector3 center = Vector2.zero;

            while (elapsed < titleShakeDuration)
            {
                float ox = Random.Range(-titleShakeIntensity, titleShakeIntensity);
                float oy = Random.Range(-titleShakeIntensity, titleShakeIntensity);

                mainTitleText.rectTransform.anchoredPosition = center + new Vector3(ox, oy, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mainTitleText.rectTransform.anchoredPosition = center;
        }

        yield return new WaitForSeconds(0.15f);

        // Slide du titre
        if (mainTitleText != null)
        {
            float elapsed = 0f;

            Vector3 startPos = Vector2.zero;
            Vector3 endPos = new Vector2(0, titleFinalYPosition);

            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;

                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / slideDuration), 3f);

                mainTitleText.rectTransform.anchoredPosition =
                    Vector3.Lerp(startPos, endPos, t);

                yield return null;
            }

            mainTitleText.rectTransform.anchoredPosition = endPos;
        }

        yield return new WaitForSeconds(0.1f);

        // Message
        if (motivationalText != null)
        {
            motivationalText.gameObject.SetActive(true);
            motivationalText.text = "";

            foreach (char c in currentMessage)
            {
                motivationalText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // Boutons
        if (replayButton != null) replayButton.gameObject.SetActive(true);
        if (quitButton != null) quitButton.gameObject.SetActive(true);
    }

    public void HideVictoryScreen()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        StopAllCoroutines();

        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);

        if (motivationalText != null) motivationalText.gameObject.SetActive(false);
        if (mainTitleText != null) mainTitleText.gameObject.SetActive(false);

        if (darkOverlay != null) darkOverlay.gameObject.SetActive(false);

        if (victoryScreenPanel != null)
            victoryScreenPanel.SetActive(false);

        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);
    }

    private void ResetUIElements()
    {
        if (mainTitleText != null)
        {
            mainTitleText.gameObject.SetActive(true);
            mainTitleText.rectTransform.anchoredPosition = Vector2.zero;
        }

        if (motivationalText != null)
        {
            motivationalText.gameObject.SetActive(false);
            motivationalText.text = "";
        }

        if (replayButton != null) replayButton.gameObject.SetActive(false);
        if (quitButton != null) quitButton.gameObject.SetActive(false);

        if (darkOverlay != null)
            darkOverlay.gameObject.SetActive(true);
    }

    void OnReplayClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}