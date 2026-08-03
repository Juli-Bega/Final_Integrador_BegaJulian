using System.Collections;
using TMPro;
using UnityEngine;

public class TriggerMessages : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float timeToDisappear = 3f;

    private void Awake()
    {
        label.maxVisibleCharacters = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        StopAllCoroutines();
        StartCoroutine(ShowMessage());
    }

    private IEnumerator ShowMessage()
    {
        label.ForceMeshUpdate();
        int total = label.textInfo.characterCount;

        for (int i = 1; i <= total; i++)
        {
            label.maxVisibleCharacters = i;
            yield return new WaitForSeconds(1f / charactersPerSecond);
        }

        yield return new WaitForSeconds(timeToDisappear);
        label.maxVisibleCharacters = 0;
    }
}