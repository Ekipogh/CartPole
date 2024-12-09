using UnityEngine;

public class Pole : MonoBehaviour
{
    public bool isRotated = false;

    private Vector3 _initialPosition;

    private float _verticalness = 0.0f; //how vertical the pole staid

    public float Verticallness
    {
        get { return _verticalness; }
    }

    private bool _isFallen = false;

    public void Fall()
    {
        _isFallen = true;
    }

    public bool IsFallen()
    {
        return _isFallen;
    }

    [Range(0.0f, 5.0f)]
    public float rotaion = 0.0f;
    void Start()
    {
        if (isRotated)
        {
            var randomRotation = Random.Range(-rotaion, rotaion);
            transform.Rotate(0, 0, randomRotation);
        }
        _initialPosition = transform.position;
    }

    void Update()
    {
        _verticalness += transform.rotation.eulerAngles.z;
    }


    public void Reset()
    {
        transform.position = _initialPosition;
        transform.rotation = Quaternion.identity;
        if (isRotated)
        {
            var randomRotation = Random.Range(-rotaion, rotaion);
            transform.Rotate(0, 0, randomRotation);
        }
        var rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0.0f;
        _isFallen = false;
        _verticalness = 0.0f;
    }
}
