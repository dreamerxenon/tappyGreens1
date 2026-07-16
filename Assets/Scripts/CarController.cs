using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float turnDuration = 0.5f;

    private bool isTurning = false;
    private float lockedY;
    private float turnStartY;
    private float turnEndY;
    private float turnElapsed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lockedY = transform.eulerAngles.y;
    }

    private void FixedUpdate()
    {
        if (isTurning)
        {
            turnElapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(turnElapsed / turnDuration);
            float y = Mathf.LerpAngle(turnStartY, turnEndY, t);
            rb.MoveRotation(Quaternion.Euler(0f, y, 0f));

            if (t >= 1f)
            {
                lockedY = turnEndY;
                isTurning = false;
            }
        }
        else
        {
            rb.MoveRotation(Quaternion.Euler(0f, lockedY, 0f));
        }

        Vector3 forward = Quaternion.Euler(0f, lockedY, 0f) * Vector3.forward;
        rb.linearVelocity = forward * speed;
        rb.angularVelocity = Vector3.zero;
    }

    private void Turn(float angle)
    {
        if (isTurning) return;
        turnStartY = transform.eulerAngles.y;
        turnEndY = turnStartY + angle;
        turnElapsed = 0f;
        isTurning = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("turnleft"))
            Turn(-90f);
        else if (other.gameObject.CompareTag("turnright"))
            Turn(90f);

        if (other.gameObject.CompareTag("deletecar"))
            Destroy(gameObject);
    }
}