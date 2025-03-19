using UnityEngine;

public class Wall : MonoBehaviour
{
    public Pole pole;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Pole")
        {
            pole.Fall(); // the pole is fallen of the cart, the current specimen is dead
        }
    }
}
