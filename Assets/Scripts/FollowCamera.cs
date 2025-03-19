using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
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
        Vector3 desiredPosition = new Vector3(target.position.x, _yCoordinate, _zCoordinate);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}
