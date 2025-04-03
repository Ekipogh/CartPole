using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private Transform _target;
    public Transform target
    {
        get => _target;
        set
        {
            _target = value;
            if (_target != null)
            {
                transform.position = new Vector3(_target.position.x, transform.position.y, transform.position.z);
            }
        }
    }
    public float smoothSpeed = 0.125f;

    private float _zCoordinate;
    private float _yCoordinate;

    void Start()
    {
        _zCoordinate = transform.position.z;
        _yCoordinate = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        if (_target == null) return;
        Vector3 desiredPosition = new(target.position.x, _yCoordinate, _zCoordinate);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
