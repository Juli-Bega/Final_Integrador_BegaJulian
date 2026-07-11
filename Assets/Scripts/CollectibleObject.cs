using UnityEngine;

public class CollectibleObject : MonoBehaviour, IInteractable
{
    private void Awake()
    {
        LevelManager.Instance.RegisterCollectible();
    }

    public void Interact()
    {
        LevelManager.Instance.CollectibleCollected();
        gameObject.SetActive(false);
    }
}