using System.Collections;
using TMPro;
using UnityEngine;

public class TriggerMessages : MonoBehaviour
{
    [SerializeField] private TMP_Text _label;
    [SerializeField] private float _charactersPerSecond = 30f;
    [SerializeField] private float _timeToDisappear = 3f;

    private void Awake()
    {
        _label.maxVisibleCharacters = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        StopAllCoroutines();
        StartCoroutine(ShowMessage());
    }

    private IEnumerator ShowMessage()
    {
        _label.ForceMeshUpdate();
        int total = _label.textInfo.characterCount;

        for (int i = 1; i <= total; i++)
        {
            _label.maxVisibleCharacters = i;
            yield return new WaitForSeconds(1f / _charactersPerSecond);
        }

        yield return new WaitForSeconds(_timeToDisappear);
        _label.maxVisibleCharacters = 0;
    }
}