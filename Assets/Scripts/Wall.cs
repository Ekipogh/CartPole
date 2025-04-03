using UnityEngine;

public class Wall : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Pole"))
        {
            if (collision.gameObject.TryGetComponent<Pole>(out var pole))
            {
                pole.Fall();
            }
        }
    }
}
