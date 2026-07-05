using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private GameObject _doorObject;
    [SerializeField] private Button[] _buttons;
    [SerializeField] private float _openHeight = 3f;
    [SerializeField] private float _moveSpeed = 2f;

    private Vector3 _closedPosition;
    private Vector3 _openPosition;
    private bool _isOpen = false;

    private void Start()
    {
        foreach (var button in _buttons)
            button.SetDoor(this);

        _closedPosition = _doorObject.transform.localPosition;
        _openPosition = _closedPosition + Vector3.up * _openHeight;

        if (_buttons.Length == 0)
            Open();
    }

    public void OnButtonChanged()
    {
        foreach (var button in _buttons)
        {
            if (!button.IsActivated)
            {
                Close();
                return;
            }
        }
        Open();
    }

    private void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        StopAllCoroutines();
        StartCoroutine(MoveDoor(_openPosition));
    }

    private void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        StopAllCoroutines();
        StartCoroutine(MoveDoor(_closedPosition));
    }

    private IEnumerator MoveDoor(Vector3 targetPosition)
    {
        while (Vector3.Distance(_doorObject.transform.localPosition, targetPosition) > 0.01f)
        {
            _doorObject.transform.localPosition = Vector3.MoveTowards(
                _doorObject.transform.localPosition,
                targetPosition,
                _moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        _doorObject.transform.localPosition = targetPosition;
    }
}