using UnityEngine;

public class Pole : MonoBehaviour
{
    public Transform poleTopPoint;
    public Transform poleMiddlePoint;
    public Transform poleBottomPoint;

    public Cart ownCart;

    private bool _isFallen = false;

    public void Fall()
    {
        _isFallen = true;
    }

    public bool IsFallen()
    {
        return _isFallen;
    }

    void OnEnable()
    {
        IgnoreCollision();
    }

    public void IgnoreCollision()
    {
        var allCarts = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var cart in allCarts)
        {
            if (cart.CompareTag("Cart") && cart != ownCart.gameObject)
            {
                Physics2D.IgnoreCollision(cart.GetComponent<Collider2D>(), GetComponent<Collider2D>());
            }
        }

        var allPoles = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var pole in allPoles)
        {
            if (pole.CompareTag("Pole") && pole != gameObject)
            {
                Physics2D.IgnoreCollision(pole.GetComponent<Collider2D>(), GetComponent<Collider2D>());
            }
        }
    }

    public Rigidbody2D GetRigidbody()
    {
        return GetComponent<Rigidbody2D>();
    }
}
