using UnityEngine;
using UnityEngine.UI;

public class WorldBallController : MonoBehaviour
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
    public WorldCameraController cameraController;
    private Vector3 startPosition;
    public AudioSource audioSource;

    public ParticleSystem[] particles;
    public Transform[] fans;
    public float windStrength = 5f;
    public float windRange = 10f;
    public bool autoFindFans;
    private Vector3? lastWorldPoint;
    private bool wasMoving = false;
    private float stillTime = 0f;
    private float stopDelay = 0.1f;
    public GameObject loadingScreen;

    [Header("Lightning Trail")]
    public GameObject lightningTrailVFX; // drag VFX_Trail_Electric here

    private void Awake()
    {
        isAiming = false;
        lineRenderer.enabled = false;
        startPosition = transform.position;

        if (lightningTrailVFX != null)
            lightningTrailVFX.SetActive(false);
    }

    private void Start()
    {
        cameraController = FindFirstObjectByType<WorldCameraController>();
        lineRenderer.alignment = LineAlignment.View;
        if (autoFindFans && (fans == null || fans.Length == 0))
        {
            GameObject[] fanObjects = GameObject.FindGameObjectsWithTag("windmill");
            fans = new Transform[fanObjects.Length];
            for (int i = 0; i < fanObjects.Length; i++)
            {
                fans[i] = fanObjects[i].transform;
            }
        }
    }

    private void Update()
    {
        if (WorldGameManager.Instance.isGameStarted && !WorldGameManager.Instance.isGamePaused)
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
        bool isBallClicked = WorldGameManager.Instance.ballClicked;
        if (isBallClicked)
            powerupSlider.gameObject.SetActive(true);
        else
            powerupSlider.gameObject.SetActive(false);
    }

    private void StabilizeMovementState()
    {
        float speed = ballRigidbody.linearVelocity.magnitude;

        if (speed < stopVelocity)
        {
            isIdle = true;
        }
        else
        {
            isIdle = false;
        }

        if (speed > stopVelocity)
        {
            WorldGameManager.Instance.ballMoving = true;
            wasMoving = true;
            stillTime = 0f;

            // ⚡ Ball is rolling — show lightning
            SetLightning(true);
        }
        else
        {
            if (wasMoving)
            {
                stillTime += Time.fixedDeltaTime;
                if (stillTime >= stopDelay)
                {
                    WorldGameManager.Instance.ballMoving = false;
                    wasMoving = false;
                    WorldGameManager.Instance.ballClicked = false;

                    // ⚡ Ball has stopped — hide lightning
                    SetLightning(false);
                }
            }
        }
    }

    private void SetLightning(bool active)
    {
        if (lightningTrailVFX != null)
            lightningTrailVFX.SetActive(active);
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
            foreach (Transform fan in fans)
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
            int ballLayerMask = LayerMask.GetMask("Ball");

            if (Physics.SphereCast(ray, 0.1f, out RaycastHit hit, 100f, ballLayerMask))
            {
                Debug.Log("Hit: " + hit.collider.name);
                if (hit.collider.CompareTag("Golf") && isIdle)
                {
                    isAiming = true;
                    WorldGameManager.Instance.ballClicked = true;
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

        if (!worldPoint.HasValue && lastWorldPoint.HasValue)
            worldPoint = lastWorldPoint;

        if (!worldPoint.HasValue)
            return;

        lastWorldPoint = worldPoint;
        DrawLine(worldPoint.Value);

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            Shoot(worldPoint.Value);
            lastWorldPoint = null;
            isAiming = false;
            WorldGameManager.Instance.ballClicked = false;
            lastWorldPoint = null;
        }
    }

    private void Shoot(Vector3 worldPoint)
    {
        isAiming = false;
        lineRenderer.enabled = false;
        WorldGameManager.Instance.ballMoving = true;

        Vector3 horizontalWorldPoint = new Vector3(
            worldPoint.x,
            transform.position.y,
            worldPoint.z
        );

        Vector3 playerDirection = (horizontalWorldPoint - transform.position).normalized;

        Vector3 holePosition = GameManager.Instance.currentHolePosition;
        Vector3 perfectDirection = (holePosition - transform.position).normalized;

        float assistStrength = CharacterManager.Instance.GetAccuracyAssist();

        Vector3 finalDirection;
        float angle = Vector3.Angle(playerDirection, perfectDirection);

        if (angle < 25f)
        {
            finalDirection = Vector3.Lerp(playerDirection, perfectDirection, assistStrength).normalized;
        }
        else
        {
            finalDirection = playerDirection;
        }

        float finalPower = CharacterManager.Instance.GetPowerMultiplier();

        ballRigidbody.AddForce(finalDirection * shotPower * power * finalPower);

        if (particles.Length > 0 && particles != null)
        {
            if (power >= 0.6f && power < 1)
            {
                ParticleSystem ps1 = Instantiate(particles[0], transform.position, Quaternion.identity);
                ParticleSystem ps2 = Instantiate(particles[1], transform.position, Quaternion.identity);
                ps1.Play();
                ps2.Play();
            }
            else if (power >= 0.9f)
            {
                ParticleSystem ps1 = Instantiate(particles[0], transform.position, Quaternion.identity);
                ParticleSystem ps2 = Instantiate(particles[1], transform.position, Quaternion.identity);
                ParticleSystem ps3 = Instantiate(particles[2], transform.position, Quaternion.identity);
                ps1.Play();
                ps2.Play();
                ps3.Play();
            }
        }

        audioSource.Play();
        isIdle = false;
        power = powerupTime = 0;
        powerupSlider.gameObject.SetActive(false);

        WorldGameManager.Instance.RegisterStroke();
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
        power = Mathf.PingPong(powerupTime, 1);
        powerupSlider.value = power;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && WorldGameManager.Instance.isGameStarted)
        {
            transform.position = startPosition;
        }
        else if (collision.gameObject.CompareTag("Hole"))
        {
            loadingScreen.SetActive(true);
            Invoke("CourseCompleted", 1f);
            ballRigidbody.linearVelocity = new Vector3(ballRigidbody.linearVelocity.x, 0f, ballRigidbody.linearVelocity.z);
        }
    }

    private void CourseCompleted()
    {
        WorldGameManager.Instance.GameWon();
        loadingScreen.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("pittrigger"))
        {
            // WorldGameManager.Instance.hitPitTrigger = true;
        }
    }

    public void ResetPlayerPosition()
    {
        Debug.Log(startPosition);
        transform.position = startPosition;
    }

    public void MoveToSpawnPoint(Transform spawn)
    {
        Debug.Log("Moving ball to spawn point");
        ballRigidbody.position = spawn.position;
        startPosition = spawn.position;
        cameraController.UpdateCameraAfterLoadingCourse(spawn.position.x);
    }
}