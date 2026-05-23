using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dracul.Player;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Player References")]
    [SerializeField] private PlayerStats _playerStats;

    [Header("Slider UI Elements")]
    [SerializeField] private Slider _healthSlider;
    [SerializeField] private Slider _bloodSlider;
    [SerializeField] private Image _bloodFillImage;

    [Header("Legacy/Text UI Elements")]
    public GameObject resultCanvas;
    public TMP_Text resultText;
    public TMP_Text lifeText;
    public TMP_Text timeText;

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

    private void Start()
    {
        // Auto-find player if not assigned
        if (_playerStats == null)
        {
            _playerStats = FindObjectOfType<PlayerStats>();
        }

        if (_healthSlider != null && _playerStats != null)
        {
            _healthSlider.maxValue = _playerStats.MaxHealth;
            _healthSlider.value = _playerStats.CurrentHealth;
        }

        if (_bloodSlider != null && _playerStats != null)
        {
            _bloodSlider.maxValue = _playerStats.MaxBlood;
            _bloodSlider.value = _playerStats.CurrentBlood;
        }
    }

    private void Update()
    {
        if (_playerStats == null) return;

        // Update Sliders
        if (_healthSlider != null)
        {
            _healthSlider.value = Mathf.Lerp(_healthSlider.value, _playerStats.CurrentHealth, Time.deltaTime * 5f);
        }

        if (_bloodSlider != null)
        {
            _bloodSlider.value = Mathf.Lerp(_bloodSlider.value, _playerStats.CurrentBlood, Time.deltaTime * 5f);
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

    public void UpdateTimeText(int hours, int minutes)
    {
        if (timeText != null)
        {
            timeText.text = string.Format("{0:00}:{1:00}", hours, minutes);
        }
    }
}
