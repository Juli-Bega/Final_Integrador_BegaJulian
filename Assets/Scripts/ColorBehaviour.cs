using UnityEngine;

public enum ObjectColor
{
    Red,
    Blue,
    Green,
    Yellow
}

public class ColorBehaviour : MonoBehaviour
{
    [SerializeField] private ObjectColor _objectColor;

    public ObjectColor ObjectColor => _objectColor;

    public void OnDetected()
    {
        Debug.Log(gameObject.name + " detectado con color: " + _objectColor);
    }
}