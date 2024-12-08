using UnityEngine;

public class Pole : MonoBehaviour
{
    public bool isRotated = false;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    [Range(0.0f, 5.0f)]
    public float rotaion = 0.0f;
    void Start()
    {
        if (isRotated)
        {
            transform.Rotate(0, 0, rotaion);
        }
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }


    public void Reset()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
        var rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0.0f;
    }
}
