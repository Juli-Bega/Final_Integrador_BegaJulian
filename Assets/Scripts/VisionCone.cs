using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    [SerializeField] private float _range = 10f;
    [SerializeField] private float _angle = 90f;
    [SerializeField] private int _rayCount = 30;

    private Mesh _mesh;

    private void Start()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        DrawCone();
    }

    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.enabled = false;
    }

    public void SetVisible(bool visible)
    {
        _meshRenderer.enabled = visible;
    }

    public void DrawCone()
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
            vertices[i + 1] = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * _range;
        }

        for (int i = 0; i < _rayCount; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }
}