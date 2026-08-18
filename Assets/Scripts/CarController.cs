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

    [HideInInspector] public GameObject sourcePrefab;
    [HideInInspector] public CarSpawner spawner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        lockedY = transform.eulerAngles.y;
    }

    public void ResetForReuse(Vector3 position, Quaternion rotation)
    {
        isTurning = false;
        turnElapsed = 0f;
        lockedY = rotation.eulerAngles.y;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = position;
            rb.rotation = rotation;
        }
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
        {
            if (spawner != null && sourcePrefab != null)
                spawner.ReturnToPool(gameObject, sourcePrefab);
            else
                Destroy(gameObject); // fallback safety net if not spawned via pool
        }
    }
}