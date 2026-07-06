using UnityEngine;

public class CollectibleObject : MonoBehaviour, IInteractable
{
    private void Start()
    {
        LevelManager.Instance.RegisterCollectible();
    }

    public void Interact()
    {
        LevelManager.Instance.CollectibleCollected();
        gameObject.SetActive(false);
    }
}