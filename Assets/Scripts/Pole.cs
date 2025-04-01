using UnityEngine;

public class Pole : MonoBehaviour
{
    private Vector3 _initialPosition;

    public Transform poleTopPoint;
    public Transform poleMiddlePoint;
    public Transform poleBottomPoint;

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
        _initialPosition = transform.position;
    }

    void OnEnable()
    {
        IgnoreCollision();
    }

    void Update()
    {
        _verticalness += transform.rotation.eulerAngles.z;
    }

    private void IgnoreCollision()
    {
        var poles = GameObject.FindGameObjectsWithTag("Pole");
        foreach (var pole in poles)
        {
            if (pole != gameObject)
            {
                Physics2D.IgnoreCollision(pole.GetComponent<Collider2D>(), GetComponent<Collider2D>());
            }
        }
    }
}
