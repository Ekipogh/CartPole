using UnityEngine;

public class Wind : MonoBehaviour
{
    public GameObject windAffectedObject;
    public float windForce = 10.0f;
    private Vector3 windDirection = Vector3.zero;



    // Update is called once per frame
    void Update()
    {
        UpdateWindDirection();
        UpdateSprite();
    }

    void UpdateWindDirection()
    {
        windDirection = new Vector3(Mathf.Sin(Time.time), 0, 0);
        if (windAffectedObject != null)
        {
            Rigidbody2D rb = windAffectedObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(windDirection * windForce);
            }
        }
    }
    void UpdateSprite()
    {
        // When windDirection.x is -1, rotationY is 180 degrees (facing left), and when windDirection.x is 1, rotationY is 0 degrees (facing right)
        float rotationY = 90 - windDirection.x * 90;
        transform.rotation = Quaternion.Euler(0, rotationY, 0);
    }
}
