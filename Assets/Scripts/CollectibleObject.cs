using UnityEngine;

public class CollectibleObject : MonoBehaviour
{
    private void Start()
    {
        LevelManager.Instance.RegisterCollectible();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager.Instance.CollectibleCollected();
            gameObject.SetActive(false);
        }
    }
}