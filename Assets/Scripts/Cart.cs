using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class Cart : MonoBehaviour
{
    public InputActionAsset inputActions;
    public Rigidbody2D rb;

    public float moveAmount = 0;

    public TextMeshPro numberText;

    public Pole ownPole;

    public float speed = 15;

    void OnEnable()
    {
        IgnoreCollision();
    }

    public void Move(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;
    }

    public void IgnoreCollision()
    {
        var allCarts = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var cart in allCarts)
        {
            if (cart.CompareTag("Cart") && cart != gameObject)
            {
                Physics2D.IgnoreCollision(cart.GetComponent<Collider2D>(), GetComponent<Collider2D>());
            }
        }
        var allPoles = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var pole in allPoles)
        {
            if (pole.CompareTag("Pole") && pole != ownPole.gameObject)
            {
                Physics2D.IgnoreCollision(pole.GetComponent<Collider2D>(), GetComponent<Collider2D>());
            }
        }
    }

    public void SetNumber(int number)
    {
        numberText.text = number.ToString();
    }
}
