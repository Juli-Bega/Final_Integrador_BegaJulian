using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    [SerializeField] private int _rayCount = 30;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private float _updateInterval = 0.1f;

    private Mesh _mesh;
    private MeshRenderer _meshRenderer;
    private float _range;
    private float _angle;
    private float _updateTimer;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.enabled = false;
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
    }

    private void Update()
    {
        if (!_meshRenderer.enabled) return;

        _updateTimer += Time.deltaTime;
        if (_updateTimer < _updateInterval) return;

        _updateTimer = 0f;
        DrawCone();
    }

    public void SetConeParameters(float range, float angle)
    {
        _range = range;
        _angle = angle;
        DrawCone();
    }

    public void SetVisible(bool visible)
    {
        _meshRenderer.enabled = visible;
        if (visible) DrawCone();
    }

    private void DrawCone()
    {
        Vector3[] vertices = new Vector3[_rayCount + 2];
        int[] triangles = new int[_rayCount * 3];

        vertices[0] = Vector3.zero;

        float angleStep = _angle / _rayCount;
        float startAngle = -_angle / 2;

        for (int i = 0; i <= _rayCount; i++)
        {
            float currentAngle = startAngle + angleStep * i;
            float rad = currentAngle * Mathf.Deg2Rad;

            Vector3 localDirection = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            Vector3 worldDirection = transform.TransformDirection(localDirection);

            float distance = _range;

            if (Physics.Raycast(transform.position, worldDirection, out RaycastHit hit, _range, _obstacleMask))
                distance = hit.distance;

            vertices[i + 1] = localDirection * distance;
        }

        for (int i = 0; i < _rayCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }
}