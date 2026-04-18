using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        Playing,
        GameOver,
        GameClear
    }

    public GameState CurrentState;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Keep across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        CurrentState = GameState.Playing;
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        CurrentState = GameState.Playing;
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        Debug.Log("Game Over!");
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowResult("GameOver");
        }
        StartCoroutine(ResetGameAfterDelay(3f));
    }

    public void GameClear()
    {
        if (CurrentState == GameState.GameClear || CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameClear;
        Debug.Log("Game Clear!");
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowResult("Goal");
        }
        StartCoroutine(ResetGameAfterDelay(3f));
    }

    System.Collections.IEnumerator ResetGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Reload the current active scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
