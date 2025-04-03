using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    private Transform _target;
    public Transform Target
    {
        get => _target;
        set
        {
            _target = value;
        }
    }

    public float smoothSpeed = 0.125f;
    private Vector3 _velocity = Vector3.zero;

    private float _zCoordinate;
    private float _yCoordinate;

    void Start()
    {
        _zCoordinate = transform.position.z;
        _yCoordinate = transform.position.y;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_target == null) return;
        Vector3 desiredPosition = new(Target.position.x, _yCoordinate, _zCoordinate);
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
