using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 1.5f;

    public bool IsGameOver { get; private set; }

    public void Show()
    {
        IsGameOver = true;
        _gameOverPanel.SetActive(true);
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _canvasGroup.alpha = timer / _fadeDuration;
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}