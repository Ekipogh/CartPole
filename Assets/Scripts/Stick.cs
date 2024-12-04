using UnityEngine;
using UnityEngine.InputSystem;

public class Stick : MonoBehaviour
{
    public InputActionAsset inputActions;
    private InputAction moveAction;
    private Vector3 originalPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var map = inputActions.FindActionMap("Player");
        moveAction = map.FindAction("Move");
        originalPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        var moveValue = moveAction.ReadValue<Vector2>();
        transform.position = originalPosition + new Vector3(moveValue.x, moveValue.y);
    }
}
