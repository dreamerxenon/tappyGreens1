using UnityEngine;
using UnityEngine.UI;

public class BallController : MonoBehaviour
{
    public float shotPower;
    private float stopVelocity = 1f;
    public Rigidbody ballRigidbody;
    private bool isAiming;
    private bool isIdle;
    public LineRenderer lineRenderer;
    public Slider powerupSlider;
    private float powerupTime;
    private float power;
    public CameraController cameraController;
    private Vector3 startPosition;
    public AudioSource audioSource;
    private bool isGameWon;
    public ParticleSystem[] particles;
    public Transform[] fans;
    public float windStrength = 5f;
    public float windRange = 10f;
    public bool autoFindFans;
    private Vector3? lastWorldPoint;
    private bool wasMoving = false;
    private float stillTime = 0f;
    private float stopDelay = 0.1f; // seconds ball must stay still before marking as stopped

    private void Awake()
    {
        isAiming = false;
        lineRenderer.enabled = false;
        startPosition = transform.position;
    }

    private void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();
        lineRenderer.alignment = LineAlignment.View;
        if (autoFindFans && (fans  == null || fans.Length == 0))
        {
            GameObject[] fanObjects = GameObject.FindGameObjectsWithTag("windmill");

            fans = new Transform[fanObjects.Length];
               for(int i = 0;i < fanObjects.Length;i++)
            {
                fans[i] = fanObjects[i].transform;
            }
        }
      
    }
    private void Update()
    {
        if (GameManager.Instance.isGameStarted && !GameManager.Instance.isGameWon && !GameManager.Instance.isGamePaused)
        {
            HandleTouchInput();
            ProcessAim();
            HandleSlider();
        }
    }
    void FixedUpdate()
    {
        StabilizeMovementState();
       
        ApplyWindForces();
    }

    private void HandleSlider()
    {
        bool isBallClicked = GameManager.Instance.ballClicked;
        if(isBallClicked)
            powerupSlider.gameObject.SetActive(true);
        else
            powerupSlider.gameObject.SetActive(false);
    }
    private void StabilizeMovementState()
    {
        float speed = ballRigidbody.linearVelocity.magnitude;

        // Immediately allow aiming when speed is low
        if (speed < stopVelocity)
        {
            isIdle = true; // allow aiming right away
        }
        else
        {
            isIdle = false;
        }

        // Maintain a stable ballMoving flag (avoid flicker)
        if (speed > stopVelocity)
        {
            GameManager.Instance.ballMoving = true;
            wasMoving = true;
            stillTime = 0f;
        }
        else
        {
            if (wasMoving)
            {
                stillTime += Time.fixedDeltaTime;
                if (stillTime >= stopDelay)
                {
                    GameManager.Instance.ballMoving = false;
                    wasMoving = false;

                    // Only clear ballClicked if we're not currently aiming (so we don't steal input mid-aim).
                   
                        GameManager.Instance.ballClicked = false;
                }
            }
            // If ball wasn't moving previously, ballMoving should already be false.
        }

        // Debug (remove or comment out in production)
        // Debug.Log($"Speed: {speed:F6}, isIdle: {isIdle}, ballMoving: {GameManager.Instance.ballMoving}, isAiming: {isAiming}");
    }

    private void ApplyWindForces()
    {
        if (fans == null || fans.Length == 0)
            return;

        foreach (Transform fan in fans)
        {
            if (fan == null)
                continue;

            Vector3 toBall = transform.position - fan.position;
            float distance = toBall.magnitude;

            if (distance < windRange)
            {
                Vector3 windDir = -fan.forward.normalized;
                float forceMultiplier = 1 - (distance / windRange);
                ballRigidbody.AddForce(windDir * windStrength * forceMultiplier, ForceMode.Force);
            }
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (fans != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.2f);
               foreach(Transform fan in fans)
            {
                if (fan != null)
                    Gizmos.DrawWireSphere(fan.position, windRange);
            }
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);
        Ray ray = Camera.main.ScreenPointToRay(touch.position);

        if (touch.phase == TouchPhase.Began)
        {
            // Only hit the "Ball" layer
            int ballLayerMask = LayerMask.GetMask("Ball");

            if (Physics.SphereCast(ray, 0.1f, out RaycastHit hit, 100f, ballLayerMask))
            {
                Debug.Log("Hit: " + hit.collider.name);
                if (hit.collider.CompareTag("Golf") && isIdle)
                {
                    isAiming = true;
                    GameManager.Instance.ballClicked = true;
                    Debug.Log("Ball touched — aiming started");
                }
            }
            else
            {
                Debug.Log("No ball hit");
            }
        }
    }




    private void ProcessAim()
    {
        if (!isAiming || !isIdle)
            return;

        Vector3? worldPoint = CastMouseClickRay();

        // If no new input, fall back to last cached point
        if (!worldPoint.HasValue && lastWorldPoint.HasValue)
            worldPoint = lastWorldPoint;

        if (!worldPoint.HasValue)
            return;

        // Update cache
        lastWorldPoint = worldPoint;

        DrawLine(worldPoint.Value);

        // Only shoot when finger is lifted
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            Shoot(worldPoint.Value);
            lastWorldPoint = null;
            isAiming = false;
            GameManager.Instance.ballClicked = false;
            lastWorldPoint = null; // reset after shooting
        }
    }



    private void Shoot(Vector3 worldPoint)
    {
        isAiming = false;
        lineRenderer.enabled = false;
        GameManager.Instance.ballMoving = true;
        GameManager.Instance.MakeStroke();
        GameManager.Instance.canShowTutorial = false;
        Vector3 horizontalWorldPoint = new Vector3(
            worldPoint.x,
            transform.position.y,
            worldPoint.z
        );

        Vector3 playerDirection =
            (horizontalWorldPoint - transform.position).normalized;

        // 🎯 PERFECT direction (toward hole)
        Vector3 holePosition = GameManager.Instance.currentHolePosition;
        Vector3 perfectDirection =
            (holePosition - transform.position).normalized;

        // 🎯 Get assist strength
        float assistStrength =
            CharacterManager.Instance.GetAccuracyAssist();

        // 🎯 Blend directions
        Vector3 finalDirection;
        float angle = Vector3.Angle(playerDirection, perfectDirection);

        if (angle < 25f)
        {
            finalDirection =
                Vector3.Lerp(playerDirection, perfectDirection, assistStrength).normalized;
        }
        else
        {
            finalDirection = playerDirection;
        }



        float finalPower =
            CharacterManager.Instance.GetPowerMultiplier();

        ballRigidbody.AddForce(
            finalDirection * shotPower * power * finalPower
        );

        
}


    private void DrawLine(Vector3 worldPoint)
    {
        lineRenderer.enabled = true;    
        Vector3 start = transform.position;

        Vector3 direction = (worldPoint - start).normalized;
        float length = Vector3.Distance(start, worldPoint);

        Vector3 endPoint = start + direction * length;
        lineRenderer.startWidth = 0.5f;
        lineRenderer.endWidth = 0.5f;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, endPoint);

    }

    private void Stop()
    {
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
       
        
       

    }


    private Vector3? CastMouseClickRay()
    {
        if (Input.touchCount == 0)
            return null;

        Touch touch = Input.GetTouch(0);
        Power();

        Vector3 screenMousePosFar = new Vector3(touch.position.x, touch.position.y, Camera.main.farClipPlane);
        Vector3 screenMousePosNear = new Vector3(touch.position.x, touch.position.y, Camera.main.nearClipPlane);

        Vector3 worldMousePosFar = Camera.main.ScreenToWorldPoint(screenMousePosFar);
        Vector3 worldMousePosNear = Camera.main.ScreenToWorldPoint(screenMousePosNear);

        if (Physics.Raycast(worldMousePosNear, worldMousePosFar - worldMousePosNear, out RaycastHit hit, float.PositiveInfinity))
            return hit.point;

        return null;
    }

    private void Power()
    {
        powerupSlider.gameObject.SetActive(true);
        powerupTime += Time.deltaTime;
        power = Mathf.PingPong(powerupTime,1);
        powerupSlider.value = power;
    }

    


   


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && GameManager.Instance.isGameWon == false)
        {
            transform.position = startPosition;
            GameManager.Instance.outOfBounds = true;
            GameManager.Instance.outOfBoundsInt++;
        }else if (collision.gameObject.CompareTag("Hole") && isGameWon == false)
        {
            GameManager.Instance.GameWon();
            isGameWon = true;
            ballRigidbody.linearVelocity = new Vector3(ballRigidbody.linearVelocity.x, 0f, ballRigidbody.linearVelocity.z);

        }
        if(collision.gameObject.CompareTag("middlewall"))
        {
            GameManager.Instance.hitmiddleWall = true;
        }
        if (collision.gameObject.CompareTag("supports"))
        {
            GameManager.Instance.hitSupports = true;
        }
        if (collision.gameObject.CompareTag("bankshotstone"))
        {
            GameManager.Instance.useBankShot = true;
        }
        if (collision.gameObject.CompareTag("wood"))
        {
            GameManager.Instance.hitWood = true;
        }
        if (collision.gameObject.CompareTag("Ramp"))
        {
            GameManager.Instance.useRamp = true;
        }
        if (collision.gameObject.CompareTag("mushroom"))
        {
            GameManager.Instance.hitMushroom = true;
        }
        if (collision.gameObject.CompareTag("windmill"))
        {
            GameManager.Instance.hitWindMill = true;
        }
        if (collision.gameObject.CompareTag("movingwall"))
        {
            GameManager.Instance.hitMovingWall = true;
        }
        if (collision.gameObject.CompareTag("redWall"))
        {
            GameManager.Instance.hitBlackWall = true;
        }
       

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("pittrigger"))
        {
            GameManager.Instance.hitPitTrigger = true;
        }
    }
    public void ResetPlayerPosition()
    {
     
        Debug.Log(startPosition);
        transform.position = startPosition;

    }
}