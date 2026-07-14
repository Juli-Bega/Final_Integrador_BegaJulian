using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class VictoryScreen : MonoBehaviour
{
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TextMeshProUGUI _collectiblesText;
    [SerializeField] private float _fadeDuration = 1f;

    public bool IsVictory { get; private set; }

    public void Show(int collected, int total)
    {
        IsVictory = true;
        _collectiblesText.text = $"Objetos recolectados: {collected}/{total}";
        _victoryPanel.SetActive(true);
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
        SceneManager.LoadScene("MainMenu");
    }
}