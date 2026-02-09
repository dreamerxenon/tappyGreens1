using System.Collections;
using UnityEngine;

public class CarController : MonoBehaviour
{
    private Rigidbody rb;
    private float speed = 5f;
    public float turnDuration  = 0.5f;
    private bool isTurning = false;
    private float lockedY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lockedY = transform.eulerAngles.y;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    private void Update()
    {
        // HARD Y LOCK — this is the key
        if (!isTurning)
        {
            rb.MoveRotation(Quaternion.Euler(0f, lockedY, 0f));
        }
    }
    void FixedUpdate()
    {
        
        Vector3 forward = Quaternion.Euler(0f, lockedY, 0f) * Vector3.forward;

        if (CompareTag("forwardcar"))
        {
            rb.linearVelocity = forward * speed;
        }
        else if (CompareTag("backwardcar"))
        {
            rb.linearVelocity = forward * speed;
        }


       
        rb.angularVelocity = Vector3.zero;
    }






    private void Turn(float angle)
    {
        if (!isTurning)
        {
            StartCoroutine(RotateSupports(angle));
        }
    }

    IEnumerator RotateSupports(float angle)
    {
        isTurning = true;

        Quaternion startRotation = rb.rotation;
        Quaternion endRotation = Quaternion.Euler(0, angle, 0) * startRotation;

        float elapsedTime = 0f;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime / turnDuration;
            rb.MoveRotation(Quaternion.Slerp(startRotation, endRotation, elapsedTime));
            yield return null;
        }

        rb.MoveRotation(endRotation);
        lockedY = endRotation.eulerAngles.y;
        isTurning = false;
    }



    private void OnTriggerEnter(Collider other)
    {
       // if (other.CompareTag("straighten"))
        //{
         //   Straighten();
       // }

        if (other.gameObject.CompareTag("turnleft"))
        {
            Turn(-90f);
        }
        else if (other.gameObject.CompareTag("turnright"))
        {
            Turn(90f);
        }
        if (other.gameObject.CompareTag("deletecar"))
        {
            Destroy(this.gameObject);
        }
    }

    private void Straighten()
    {
        float y = transform.eulerAngles.y;

        // Snap to nearest 90-degree direction
        float snappedY = Mathf.Round(y / 90f) * 90f;

        transform.rotation = Quaternion.Euler(0f, snappedY, 0f);

        // Reset angular drift
        rb.angularVelocity = Vector3.zero;
    }
}
