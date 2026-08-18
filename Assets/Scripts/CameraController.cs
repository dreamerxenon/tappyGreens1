using DG.Tweening;
using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private struct CameraState
    {
        public Vector3 position;
        public Quaternion rotation;

        public CameraState(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }

    private CameraState[] cameraStates;
    private int currentIndex = 0;
    private ZoomController zoomController;
    private float lastTapTime;
    private float doubleTapTime = 0.3f;
    public bool canControlCamera;
    public float rotationSpeed = 0.2f;
    public float minVerticalAngle = -60f;
    private float maxVerticalAngle = 90f;
    public float minHorizontalAngle = -90f;
    public float maxHorizontalAngle = 90f;

    private Vector2 lastTouchPosition;
    private bool isDragging = false;
    private float currentVerticalRotation = 0f;
    private float currentHorizontalRotation = 0f;
    public Transform lookAt;
    private float initialLookAtZposition;
    private float finalLookAtZposition;
    private float zPosition;
    public float initialZoom;
    public Transform target;
    private Camera cam;
    private float zoomSpeed = .7f;
    private float minFov = 10f, maxFov = 100f;
    private float targetFov;
    private Quaternion targetRotation;
    private Quaternion targetRotationafterMoving;
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private bool canRotateCamera, canSetInitialRotation;
    private Vector3 currentPos;
    private Quaternion currentRot;

    // Main-camera "return to ball" memory
    // Instead of freezing an absolute position/rotation when leaving the main
    // camera, we remember the player's chosen height/angle and recompute the
    // position relative to the ball's CURRENT location when we come back.
    private float savedMainCamHeight;
    private float savedMainCamHorizontalRotation;
    private float savedMainCamVerticalRotation;
    private bool hasSavedMainCamState = false;

    // Shake
    [Header("Camera Shake")]
    [SerializeField] private float defaultShakeDuration = 0.35f;
    [SerializeField] private float defaultShakeMagnitude = 0.25f;
    [SerializeField] private float defaultShakeDamping = 4f;

    private Vector3 _shakeOffset = Vector3.zero;
    private Quaternion _shakeRotation = Quaternion.identity;
    private Coroutine _shakeRoutine;

    public void Shake(float duration = -1f, float magnitude = -1f, float damping = -1f)
    {
        if (duration < 0) duration = defaultShakeDuration;
        if (magnitude < 0) magnitude = defaultShakeMagnitude;
        if (damping < 0) damping = defaultShakeDamping;

        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);

        _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude, damping));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude, float damping)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float currentMag = magnitude * (1f - progress);

            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * currentMag;

            float rotMag = currentMag * 3f;
            _shakeOffset = randomOffset;
            _shakeRotation = Quaternion.Euler(
                UnityEngine.Random.Range(-rotMag, rotMag),
                UnityEngine.Random.Range(-rotMag * 0.5f, rotMag * 0.5f),
                UnityEngine.Random.Range(-rotMag, rotMag)
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        _shakeOffset = Vector3.zero;
        _shakeRotation = Quaternion.identity;
        _shakeRoutine = null;
    }
    //

    private void Awake()
    {
        zoomController = FindAnyObjectByType<ZoomController>();
        currentPos = transform.position;
        currentRot = transform.rotation;
        cameraStates = new CameraState[]
        {
            new CameraState(currentPos, currentRot),
            new CameraState(
                new Vector3(-10.05f, 6.81f, 11.2f),
                Quaternion.Euler(21.004f, -298.213f, -360.647f)
            ),
            new CameraState(
                new Vector3(8.611f, 5.393f, 10.45f),
                Quaternion.Euler(14.268f, -418.784f, -357.337f)
            )
        };
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        canControlCamera = true;
        targetFov = cam.fieldOfView;
        targetRotation = targetRotationafterMoving = transform.rotation;
        initialLookAtZposition = lookAt.position.z;
        targetPosition = transform.position;
        Vector3 eulerRotation = transform.eulerAngles;
        currentHorizontalRotation = eulerRotation.y;
        currentVerticalRotation = eulerRotation.x;
        zPosition = 2.0f;
        initialZoom = GetComponent<Camera>().fieldOfView;
    }

    private void LateUpdate()
    {
        FollowBall();

        if (_shakeOffset != Vector3.zero || _shakeRotation != Quaternion.identity)
        {
            transform.position += _shakeOffset;
            transform.rotation = _shakeRotation * transform.rotation;
        }
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        DetectDraggingAndZoomEditor();
        DetectDoubleTapEditor();
#else
        DetectDraggingAndZoom();
        DetectDoubleTap();
#endif
    }

    // Mobile (touch) 

    private void DetectDoubleTap()
    {
        if (!GameManager.Instance.isGamePaused && GameManager.Instance.isGameStarted && GameManager.Instance.isConnected)
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    if (Time.time - lastTapTime <= doubleTapTime)
                        SwitchCamera();

                    lastTapTime = Time.time;
                }
            }
        }
    }

    private void DetectDraggingAndZoom()
    {
        if (!GameManager.Instance.isGamePaused)
        {
            if (Input.touchCount == 1 && !GameManager.Instance.ballClicked && GameManager.Instance.isGameStarted)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        lastTouchPosition = touch.position;
                        isDragging = true;
                        break;

                    case TouchPhase.Moved:
                        if (isDragging)
                        {
                            Vector2 delta = touch.position - lastTouchPosition;
                            ApplyRotationDelta(delta);
                            lastTouchPosition = touch.position;
                        }
                        break;

                    case TouchPhase.Ended:
                        isDragging = false;
                        break;
                }
            }

            if (Input.touchCount == 2 && !GameManager.Instance.ballClicked && GameManager.Instance.isGameStarted)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                Vector2 touch0Prev = touch0.position - touch0.deltaPosition;
                Vector2 touch1Prev = touch1.position - touch1.deltaPosition;

                float prevMagnitude = (touch0Prev - touch1Prev).magnitude;
                float currentMagnitude = (touch0.position - touch1.position).magnitude;
                float difference = currentMagnitude - prevMagnitude;

                ApplyZoom(difference);
            }

            if (canRotateCamera)
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }

    //  Editor (mouse)

#if UNITY_EDITOR || UNITY_STANDALONE
    private float lastClickTime;

    private void DetectDoubleTapEditor()
    {
        if (!GameManager.Instance.isGamePaused && GameManager.Instance.isGameStarted && GameManager.Instance.isConnected)
        {
            // Double left-click switches camera, mirroring the double-tap behaviour
            if (Input.GetMouseButtonDown(0))
            {
                if (Time.time - lastClickTime <= doubleTapTime)
                    SwitchCamera();

                lastClickTime = Time.time;
            }
        }
    }

    private void DetectDraggingAndZoomEditor()
    {
        if (!GameManager.Instance.isGamePaused)
        {
            if (!GameManager.Instance.ballClicked && GameManager.Instance.isGameStarted)
            {
                bool rotateModifierHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                // Modifier + left-click drag → rotate
                if (rotateModifierHeld && Input.GetMouseButtonDown(0))
                {
                    lastTouchPosition = Input.mousePosition;
                    isDragging = true;
                }

                if (rotateModifierHeld && Input.GetMouseButton(0) && isDragging)
                {
                    Vector2 delta = (Vector2)Input.mousePosition - lastTouchPosition;
                    ApplyRotationDelta(delta);
                    lastTouchPosition = Input.mousePosition;
                }

                if (Input.GetMouseButtonUp(0) || !rotateModifierHeld)
                    isDragging = false;

                // Scroll wheel / trackpad pinch → zoom
                // Using mouseScrollDelta (raw, unsmoothed) instead of GetAxis to avoid
                // the "wrong direction blip" caused by Input Manager axis smoothing
                // (sensitivity/gravity) plus trackpad momentum jitter at gesture start.
                float scroll = Input.mouseScrollDelta.y;
                if (Mathf.Abs(scroll) > 0.05f)
                    ApplyZoom(scroll * 100f);
            }

            if (canRotateCamera)
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }
    }
#endif

    //  Shared helpers 

    private void ApplyRotationDelta(Vector2 delta)
    {
        float rotX = delta.x * rotationSpeed;
        float rotY = -delta.y * rotationSpeed;

        currentHorizontalRotation += rotX;
        currentVerticalRotation += rotY;

        currentVerticalRotation = Mathf.Clamp(currentVerticalRotation, minVerticalAngle, maxVerticalAngle);

        targetRotation = Quaternion.Euler(currentVerticalRotation, currentHorizontalRotation, 0f);
        canRotateCamera = true;
    }

    private void ApplyZoom(float difference)
    {
        targetFov -= difference * zoomSpeed * 0.1f;
        targetFov = Mathf.Clamp(targetFov, minFov, maxFov);
        if (cam.fieldOfView >= minFov && cam.fieldOfView <= maxFov)
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 5f * Time.deltaTime);
    }

    // 

    void SwitchCamera()
    {
        // Leaving the main (ball-following) camera: remember height/angle only,
        // NOT an absolute position, since the ball may keep moving while we're away.
        if (currentIndex == 0)
        {
            savedMainCamHeight = transform.position.y;
            savedMainCamHorizontalRotation = currentHorizontalRotation;
            savedMainCamVerticalRotation = currentVerticalRotation;
            hasSavedMainCamState = true;
        }

        currentIndex = (currentIndex + 1) % cameraStates.Length;
        if (currentIndex > 0)
        {
            canControlCamera = false;
            zoomController.MakeSliderNonInteractable();
            cam.fieldOfView = 35;
            targetFov = cam.fieldOfView; // keep targetFov in sync so the next zoom doesn't jitter/snap
        }
        else if (currentIndex == 0)
        {
            canControlCamera = true;
            zoomController.MakeSliderInteractable();
            cam.fieldOfView = 53;
            targetFov = cam.fieldOfView; // keep targetFov in sync so the next zoom doesn't jitter/snap
        }
        canRotateCamera = false;
        isDragging = false;
        ApplyCameraState(currentIndex);
    }

    void ApplyCameraState(int index)
    {
        if (index == 0 && hasSavedMainCamState)
        {
            // Recompute the main camera's position relative to the ball's CURRENT
            // location (same formula FollowBall uses), instead of restoring a stale
            // absolute snapshot from before the ball may have moved.
            float newZ = lookAt.position.z - zPosition;
            transform.position = new Vector3(lookAt.position.x, savedMainCamHeight, newZ);
            transform.rotation = Quaternion.Euler(savedMainCamVerticalRotation, savedMainCamHorizontalRotation, 0f);
        }
        else
        {
            transform.position = cameraStates[index].position;
            transform.rotation = cameraStates[index].rotation;
        }

        transform.LookAt(lookAt);
        Vector3 euler = transform.eulerAngles;
        currentHorizontalRotation = euler.y;
        currentVerticalRotation = euler.x;
    }

    public void FollowBall()
    {
        if (!canControlCamera)
            return;

        float finalLookAtZposition = lookAt.position.z - zPosition;
        targetPosition = new Vector3(lookAt.position.x, transform.position.y, finalLookAtZposition);

        if (GameManager.Instance.ballMoving)
        {
            canRotateCamera = false;
            transform.position = Vector3.Lerp(transform.position, targetPosition, 0.1f);
            transform.LookAt(lookAt);
            targetRotation = transform.rotation;
            canSetInitialRotation = true;
        }
        else
        {
            if (canSetInitialRotation)
            {
                Vector3 euler = transform.eulerAngles;
                currentHorizontalRotation = euler.y;
                currentVerticalRotation = euler.x;
                canSetInitialRotation = false;
            }
        }
    }

    public void ShowBall()
    {
        finalLookAtZposition = lookAt.position.z - zPosition;
        Vector3 targetPosition = new Vector3(lookAt.position.x, lookAt.position.y, finalLookAtZposition);
        transform.position = Vector3.Lerp(transform.position, targetPosition, 0.1f);
    }

    public void LookAtBall()
    {
        if (lookAt != null)
        {
            transform.LookAt(lookAt);
            targetRotation = transform.rotation;
            Vector3 euler = transform.eulerAngles;
            currentHorizontalRotation = euler.y;
            currentVerticalRotation = euler.x;
        }
    }
}