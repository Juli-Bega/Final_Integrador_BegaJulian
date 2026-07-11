using System.Collections;
using UnityEngine;
using TMPro;

public class CollectibleCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentCountText;
    [SerializeField] private TextMeshProUGUI _totalCountText;

    [Header("Punch Animation")]
    [SerializeField] private float _punchScale = 1.6f;
    [SerializeField] private float _punchDuration = 0.35f;
    [SerializeField] private Color _punchColor = Color.yellow;

    private int _displayedCount = 0;
    private Vector3 _baseScale;
    private Color _baseColor;

    private void Start()
    {
        _baseScale = _currentCountText.transform.localScale;
        _baseColor = _currentCountText.color;

        _currentCountText.text = "0";
        _totalCountText.text = "/" + LevelManager.Instance.TotalCollectibles;
    }

    private void Update()
    {
        int actualCount = LevelManager.Instance.CollectedCount;

        if (actualCount != _displayedCount)
        {
            _displayedCount = actualCount;
            StopAllCoroutines();
            StartCoroutine(PunchAnimation(actualCount));
        }
    }

    private IEnumerator PunchAnimation(int newValue)
    {
        float halfDuration = _punchDuration / 2f;
        float timer = 0f;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            _currentCountText.transform.localScale = Vector3.Lerp(_baseScale, _baseScale * _punchScale, t);
            _currentCountText.color = Color.Lerp(_baseColor, _punchColor, t);
            yield return null;
        }

        _currentCountText.text = newValue.ToString();

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;
            _currentCountText.transform.localScale = Vector3.Lerp(_baseScale * _punchScale, _baseScale, t);
            _currentCountText.color = Color.Lerp(_punchColor, _baseColor, t);
            yield return null;
        }

        _currentCountText.transform.localScale = _baseScale;
        _currentCountText.color = _baseColor;
    }
}