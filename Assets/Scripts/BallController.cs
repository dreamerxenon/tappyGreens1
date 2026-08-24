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
    private float stopDelay = 0.1f;

    private RectTransform sliderRectTransform;
    private Canvas canvas;
    private float sliderDistancePastTip = 40f; // pixels past the tip, along arrow direction

    [Header("Shoot Confirmation")]
    public Button shootButton;
    public string shootButtonTag = "ShootButton";
    private bool isAwaitingShot;
    private Vector3 confirmedWorldPoint;

    [Header("Lightning Trail")]
    public GameObject lightningTrailVFX; // drag VFX_Trail_Electric here

    private void Awake()
    {
        isAiming = false;
        lineRenderer.enabled = false;
        startPosition = transform.position;

        if (lightningTrailVFX != null)
            lightningTrailVFX.SetActive(false);

        sliderRectTransform = powerupSlider.GetComponent<RectTransform>();
        canvas = powerupSlider.GetComponentInParent<Canvas>();

        sliderRectTransform.localScale *= 0.5f;
        sliderRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRectTransform.pivot = new Vector2(0.5f, 0.5f);

        powerupSlider.gameObject.SetActive(false);
    }

    private void Start()
    {
        cameraController = FindFirstObjectByType<CameraController>();
        lineRenderer.alignment = LineAlignment.View;

        if (shootButton == null)
        {
            GameObject shootButtonObj = GameObject.FindGameObjectWithTag(shootButtonTag);
            if (shootButtonObj != null)
                shootButton = shootButtonObj.GetComponent<Button>();
            else
                Debug.LogWarning($"No GameObject found with tag '{shootButtonTag}' for shoot button.");
        }

        if (shootButton != null)
            shootButton.gameObject.SetActive(false);

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
        if (GameManager.Instance.isGameStarted && !GameManager.Instance.isGameWon && !GameManager.Instance.isGamePaused)
        {
            HandleTouchInput();
            ProcessAim();
            HandleSlider();
            UpdateAwaitingShotPower();
        }
    }

    void FixedUpdate()
    {
        StabilizeMovementState();
        ApplyWindForces();
    }

    private void HandleSlider()
    {
        // Only visible once the finger has been released and a shot is pending confirmation.
        // Hidden again the moment the player starts re-dragging to change direction.
        bool shouldShow = isAwaitingShot && !isAiming && !GameManager.Instance.tapOnSlider;
        powerupSlider.gameObject.SetActive(shouldShow);
    }

    private void UpdateAwaitingShotPower()
    {
        // Only oscillate power here when frozen/awaiting confirmation and NOT actively re-dragging
        // (while re-dragging, Power() inside CastMouseClickRay drives it instead, to avoid double-increment).
        if (!isAwaitingShot || isAiming) return;

        powerupTime += Time.deltaTime;
        power = Mathf.PingPong(powerupTime, 1);
        powerupSlider.value = power;
    }

    private void StabilizeMovementState()
    {
        float speed = ballRigidbody.linearVelocity.magnitude;
        float volume = PlayerPrefs.GetFloat("effectsVolume");

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
            GameManager.Instance.ballMoving = true;
            wasMoving = true;
            stillTime = 0f;

            // Ball is rolling — show lightning
            SetLightning(true);
            lightningTrailVFX.GetComponent<AudioSource>().volume = volume;
            lightningTrailVFX.GetComponent<AudioSource>().Play();
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
                    GameManager.Instance.ballClicked = false;
                    //cameraController.ShowBall();
                    // Ball has stopped — hide lightning
                    SetLightning(false);
                    lightningTrailVFX.GetComponent<AudioSource>().volume = volume;
                    lightningTrailVFX.GetComponent<AudioSource>().Stop();

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
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleRealTouchInput();
#endif
    }

    private void HandleRealTouchInput()
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
                // isIdle still allows re-touching the ball even while a shot is awaiting confirmation,
                // so the player can redirect before pressing Shoot.
                if (hit.collider.CompareTag("Golf") && isIdle)
                {
                    StartAiming();
                    Debug.Log("Ball touched — aiming started");
                }
            }
            else
            {
                Debug.Log("No ball hit");
            }
        }
    }

    private void HandleMouseInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        int ballLayerMask = LayerMask.GetMask("Ball");

        if (Physics.SphereCast(ray, 0.1f, out RaycastHit hit, 100f, ballLayerMask))
        {
            Debug.Log("Hit: " + hit.collider.name);
            if (hit.collider.CompareTag("Golf") && isIdle)
            {
                StartAiming();
                Debug.Log("Ball clicked — aiming started");
            }
        }
        else
        {
            Debug.Log("No ball hit");
        }
    }

    private void StartAiming()
    {
        isAiming = true;
        GameManager.Instance.ballClicked = true;
        GameManager.Instance.canShowTutorial = false;
        // Re-entering aim (even if a shot was already pending) hides the button/slider
        // until the player releases again.
        if (shootButton != null)
            shootButton.gameObject.SetActive(false);
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

        bool released =
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) ||
            Input.GetMouseButtonUp(0);

        if (released)
        {
            confirmedWorldPoint = worldPoint.Value;
            isAiming = false;
            isAwaitingShot = true;
            lastWorldPoint = null;

            if (shootButton != null)
                shootButton.gameObject.SetActive(true);
        }
    }

    public void ConfirmShot()
    {
        if (!isAwaitingShot) return;

        isAwaitingShot = false;
        if (shootButton != null)
            shootButton.gameObject.SetActive(false);

        Shoot(confirmedWorldPoint);
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

        UpdateSliderPosition(start, endPoint);
    }

    private void UpdateSliderPosition(Vector3 ballWorldPos, Vector3 arrowTipWorldPos)
    {
        if (sliderRectTransform == null || canvas == null) return;

        RectTransform parentRect = sliderRectTransform.parent as RectTransform;
        if (parentRect == null) return;

        Camera uiCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector3 ballScreenPoint = Camera.main.WorldToScreenPoint(ballWorldPos);
        Vector3 tipScreenPoint = Camera.main.WorldToScreenPoint(arrowTipWorldPos);

        Vector2 ballLocal, tipLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, ballScreenPoint, uiCam, out ballLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, tipScreenPoint, uiCam, out tipLocal);

        Vector2 dir = (tipLocal - ballLocal).normalized;
        if (dir == Vector2.zero) dir = Vector2.up;

        sliderRectTransform.anchoredPosition = tipLocal + dir * sliderDistancePastTip;
    }

    private void Stop()
    {
        ballRigidbody.linearVelocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;
    }

    private Vector3? CastMouseClickRay()
    {
        Vector2 screenPos;

#if UNITY_EDITOR || UNITY_STANDALONE
        screenPos = Input.mousePosition;
        Power();
#else
        if (Input.touchCount == 0)
            return null;

        Touch touch = Input.GetTouch(0);
        screenPos = touch.position;
        Power();
#endif

        Vector3 screenMousePosFar = new Vector3(screenPos.x, screenPos.y, Camera.main.farClipPlane);
        Vector3 screenMousePosNear = new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane);

        Vector3 worldMousePosFar = Camera.main.ScreenToWorldPoint(screenMousePosFar);
        Vector3 worldMousePosNear = Camera.main.ScreenToWorldPoint(screenMousePosNear);

        if (Physics.Raycast(worldMousePosNear, worldMousePosFar - worldMousePosNear, out RaycastHit hit, float.PositiveInfinity))
            return hit.point;

        return null;
    }

    private void Power()
    {
        powerupTime += Time.deltaTime;
        power = Mathf.PingPong(powerupTime, 1);
        powerupSlider.value = power;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") && GameManager.Instance.isGameWon == false)
        {
            transform.position = startPosition;
            GameManager.Instance.outOfBounds = true;
            GameManager.Instance.outOfBoundsInt++;
        }
        else if (collision.gameObject.CompareTag("Hole") && isGameWon == false)
        {
            GameManager.Instance.GameWon();
            isGameWon = true;
            ballRigidbody.linearVelocity = new Vector3(ballRigidbody.linearVelocity.x, 0f, ballRigidbody.linearVelocity.z);
        }
        if (collision.gameObject.CompareTag("middlewall"))
            GameManager.Instance.hitmiddleWall = true;
        if (collision.gameObject.CompareTag("supports"))
            GameManager.Instance.hitSupports = true;
        if (collision.gameObject.CompareTag("bankshotstone"))
            GameManager.Instance.useBankShot = true;
        if (collision.gameObject.CompareTag("wood"))
            GameManager.Instance.hitWood = true;
        if (collision.gameObject.CompareTag("Ramp"))
            GameManager.Instance.useRamp = true;
        if (collision.gameObject.CompareTag("mushroom"))
            GameManager.Instance.hitMushroom = true;
        if (collision.gameObject.CompareTag("windmill"))
            GameManager.Instance.hitWindMill = true;
        if (collision.gameObject.CompareTag("movingwall"))
            GameManager.Instance.hitMovingWall = true;
        if (collision.gameObject.CompareTag("redWall"))
            GameManager.Instance.hitBlackWall = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("pittrigger"))
            GameManager.Instance.hitPitTrigger = true;
    }

    public void ResetPlayerPosition()
    {
        Debug.Log(startPosition);
        transform.position = startPosition;
    }
}