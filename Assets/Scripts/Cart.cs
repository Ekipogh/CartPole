using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Cart : MonoBehaviour
{
    public InputActionAsset inputActions;
    public Rigidbody2D rb;
    private InputAction moveAction;
    private Vector3 _initialPosition;

    public float moveAmount = 0;

    public float speed = 15;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        var map = inputActions.FindActionMap("Player");
        moveAction = map.FindAction("Move");
        _initialPosition = transform.position;
    }

    void OnEnable()
    {
        moveAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        var moveValue = moveAction.ReadValue<Vector2>();
        if (moveValue.x != 0)
            Move(new Vector3(moveValue.x, 0));
    }

    public void Move(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;
    }

    public void Reset()
    {
        transform.position = _initialPosition;
        rb.linearVelocity = Vector2.zero;
        moveAmount = 0;
    }
}
