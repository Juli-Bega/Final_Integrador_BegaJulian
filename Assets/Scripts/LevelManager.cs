using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private int _totalCollectibles = 0;
    private int _collectedCount = 0;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterCollectible()
    {
        _totalCollectibles++;
    }

    public void CollectibleCollected()
    {
        _collectedCount++;
    }

    public void PlayerDetected()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void CompleteLevel()
    {
        Debug.Log("Level Complete - Collected: " + _collectedCount + "/" + _totalCollectibles);
    }
}