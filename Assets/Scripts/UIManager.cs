using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private PauseManager _pauseManager;
    [SerializeField] private GameOverScreen _gameOverScreen;

    public bool IsPaused => _pauseManager.IsPaused;
    public bool IsGameOver => _gameOverScreen.IsGameOver;
    public bool IsGameplayBlocked => IsPaused || IsGameOver;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowGameOver()
    {
        _gameOverScreen.Show();
    }
}