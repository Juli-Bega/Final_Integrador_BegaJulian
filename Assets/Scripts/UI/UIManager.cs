using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private CanvasGroup _gameOverPanel;
    [SerializeField] private CanvasGroup _victoryPanel;

    [Header("Victory")]
    [SerializeField] private TextMeshProUGUI _collectiblesText;

    [Header("Fade")]
    [SerializeField] private float _fadeDuration = 1.5f;

    [Header("Scenes")]
    [SerializeField] private string _menuSceneName = "MainMenu";

    private bool _isPaused;
    private bool _isLevelOver;

    public bool IsGameplayBlocked => _isPaused || _isLevelOver;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (_isLevelOver) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        _isPaused = true;
        _pausePanel.SetActive(true);
        FreezeGame(true);
    }

    public void ResumeGame()
    {
        _isPaused = false;
        _pausePanel.SetActive(false);
        FreezeGame(false);
    }


    public void ShowGameOver()
    {
        _isLevelOver = true;
        StartCoroutine(ShowPanelWithFade(_gameOverPanel));
    }

    public void ShowVictory(int collected, int total)
    {
        _isLevelOver = true;
        _collectiblesText.text = $"Objetos recolectados: {collected}/{total}";
        StartCoroutine(ShowPanelWithFade(_victoryPanel));
    }

    private IEnumerator ShowPanelWithFade(CanvasGroup panel)
    {
        panel.gameObject.SetActive(true);
        panel.alpha = 0f;
        panel.interactable = false;

        FreezeGame(true);

        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            panel.alpha = timer / _fadeDuration;
            yield return null;
        }

        panel.alpha = 1f;
        panel.interactable = true;
    }


    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextIndex);
        else
            SceneManager.LoadScene("Credits");
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(_menuSceneName);
    }

    private void FreezeGame(bool frozen)
    {
        Time.timeScale = frozen ? 0f : 1f;
        Cursor.lockState = frozen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = frozen;
    }
}