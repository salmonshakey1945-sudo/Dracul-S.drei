using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using TMPro; // Required for TextMeshPro

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Elements")]
    public GameObject resultCanvas;
    public TMP_Text resultText;
    public TMP_Text lifeText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ShowResult(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
        }
        
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(true);
        }
    }

    public void UpdateLifeText(int health)
    {
        if (lifeText != null)
        {
            lifeText.text = "Life: " + health.ToString();
        }
    }
}
