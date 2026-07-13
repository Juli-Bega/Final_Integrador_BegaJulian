using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    private int _totalCollectibles = 0;
    private int _collectedCount = 0;

    public int CollectedCount => _collectedCount;
    public int TotalCollectibles => _totalCollectibles;


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
        UIManager.Instance.ShowGameOver();
    }

    public void CompleteLevel()
    {
        UIManager.Instance.ShowVictory(_collectedCount, _totalCollectibles);
    }

}