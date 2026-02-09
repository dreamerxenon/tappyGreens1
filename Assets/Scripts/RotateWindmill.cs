using UnityEngine;

public class RotateWindmill : MonoBehaviour
{
    public float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(gameObject.CompareTag("loader"))
        {
            transform.Rotate(0, 0, -speed * Time.unscaledDeltaTime);
        }
        else
        {
            transform.Rotate(0, 0, speed * Time.deltaTime);
        }
        
    }
}
