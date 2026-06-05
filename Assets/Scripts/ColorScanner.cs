using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorScanner : MonoBehaviour
{
    [SerializeField] private ShaderService _shaderService;
    [SerializeField] private float _scanInterval = 0.1f;

    private Camera _camera;
    private List<ColorBehaviour> _detectedObjects = new List<ColorBehaviour>();

    private void Awake()
    {
        _camera = Camera.main;
    }

    public void StartScanning()
    {
        StartCoroutine(ScanRoutine());
    }

    public void StopScanning()
    {
        StopAllCoroutines();
        foreach (var obj in _detectedObjects)
            obj.OnDetected();
        _detectedObjects.Clear();
    }

    private IEnumerator ScanRoutine()
    {
        while (true)
        {
            Scan();
            yield return new WaitForSeconds(_scanInterval);
        }
    }

    private void Scan()
    {
        ColorBehaviour[] allObjects = FindObjectsByType<ColorBehaviour>(FindObjectsSortMode.None);
        List<ColorBehaviour> currentlyDetected = new List<ColorBehaviour>();

        foreach (var obj in allObjects)
        {
            if (IsInsideCircle(obj) && HasLineOfSight(obj))
                currentlyDetected.Add(obj);
        }

        foreach (var obj in currentlyDetected)
        {
            if (!_detectedObjects.Contains(obj))
                obj.OnDetected();
        }

        _detectedObjects = currentlyDetected;
    }

    private bool IsInsideCircle(ColorBehaviour obj)
    {
        Vector3 viewportPos = _camera.WorldToViewportPoint(obj.transform.position);

        if (viewportPos.z < 0)
            return false;

        Vector2 centeredPos = new Vector2(viewportPos.x - 0.5f, viewportPos.y - 0.5f);
        centeredPos.x *= (float)Screen.width / Screen.height;

        return centeredPos.magnitude < _shaderService.GetCircleRadius();
    }

    private bool HasLineOfSight(ColorBehaviour obj)
    {
        Vector3 direction = obj.transform.position - _camera.transform.position;
        float distance = direction.magnitude;

        if (Physics.Raycast(_camera.transform.position, direction.normalized, out RaycastHit hit, distance))
            return hit.collider.gameObject == obj.gameObject;

        return false;
    }
}