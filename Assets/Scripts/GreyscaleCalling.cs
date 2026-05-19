using UnityEngine;

public class GreyscaleCalling : MonoBehaviour
{
    private Material _material;
    public Shader _shader;

    void Start() 
    {
        _material = new Material(_shader);
    }
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, _material);
    }
}

