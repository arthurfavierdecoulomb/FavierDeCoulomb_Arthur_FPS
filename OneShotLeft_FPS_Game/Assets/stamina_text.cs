using UnityEngine;
using TMPro;

public class StaminaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI staminaText;
    [SerializeField] private StaminaSystem staminaSystem;

    [Header("Flash Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private float flashSpeed = 5f;

    
    private bool isFlashing = false;
    private float flashTimer = 0f;

    void Update()
    {
        if (staminaText == null || staminaSystem == null) return;

        float currentStamina = staminaSystem.GetCurrentStamina();

        // Affiche la stamina arrondie
        staminaText.text = Mathf.RoundToInt(currentStamina).ToString();


        if (currentStamina <= 20)
        {
            if (isFlashing)
            {
                flashTimer += Time.deltaTime * flashSpeed;
                // Ping-pong entre normalColor et emptyColor
                staminaText.color = Color.Lerp(normalColor, emptyColor, Mathf.PingPong(flashTimer, 1f));
            }
            else
            {
                staminaText.color = emptyColor;
            }
        }
        else
        {
            isFlashing = false;
            staminaText.color = normalColor;
        }
    }
}