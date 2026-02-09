
using UnityEngine;

public class WorldCameraController : MonoBehaviour
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
    public WorldZoomController worldZoomController;
    private float lastTapTime;
    private float doubleTapTime = 0.3f;
    public bool canControlCamera;
    public float rotationSpeed = 0.2f; // Sensitivity of rotation
    public float minVerticalAngle = -60f; // Lower vertical limit
    private float maxVerticalAngle = 90f; // Upper vertical limit
    public float minHorizontalAngle = -90f; // Left rotation limit
    public float maxHorizontalAngle = 90f; // Right rotation limit

    private Vector2 lastTouchPosition; // Store last touch position
    private bool isDragging = false; // Track if user is swiping
    private float currentVerticalRotation = 0f; // Tracks vertical rotation
    private float currentHorizontalRotation = 0f; // Tracks horizontal rotation
    public Transform lookAt;
    private float initialLookAtZposition;
    private float finalLookAtZposition;
    private float zPosition;
    public float initialZoom;
    public Transform target;
    private Camera cam;
    private float zoomSpeed = .7f;
    private float minFov = 10f, maxFov = 100f;
    private float targetFov; private Quaternion targetRotation; private Quaternion targetRotationafterMoving;
    private Vector3 velocity = Vector3.zero; // for SmoothDamp
    private Vector3 targetPosition;
    private bool canRotateCamera, canSetInitialRotation;
    private Vector3 currentPos;
    private Quaternion currentRot;

    public Vector3 secondCamPos, thirdCamPos;
    public Quaternion secondCamAng, thirdCamAngle;

    private void Awake()
    {
       
            currentPos = transform.position;
            currentRot = transform.rotation;
        
        cameraStates = new CameraState[]
      {
            new CameraState(
                currentPos,
                currentRot
            ),
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
        targetFov = cam.fieldOfView; targetRotation = targetRotationafterMoving = transform.rotation;
        initialLookAtZposition = lookAt.position.z;
        targetPosition = transform.position;
        // Initialize rotation values
        Vector3 eulerRotation = transform.eulerAngles;
        currentHorizontalRotation = eulerRotation.y;
        currentVerticalRotation = eulerRotation.x;
        zPosition = 2.0f;
        initialZoom = GetComponent<Camera>().fieldOfView;
    }

    private void LateUpdate()
    {
        FollowBall();
    }

    void Update()
    {

        DetectDraggingAndZoom();
        DetectDoubleTap();

    }

    private void DetectDoubleTap()
    {
        if (!WorldGameManager.Instance.isGamePaused && WorldGameManager.Instance.isGameStarted)
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    if (Time.time - lastTapTime <= doubleTapTime)
                    {
                        SwitchCamera();
                    }

                    lastTapTime = Time.time;
                }
            }
        }
    }

    public void UpdateCameraAfterLoadingCourse(float xPos)
    {
        if(currentIndex == 0)
        {
            Vector3 newPos = new Vector3(xPos, transform.position.y, transform.position.z);
            transform.position = newPos;
            targetPosition = newPos;
        }
        currentPos = transform.position;
    }
    void SwitchCamera()
    {
        currentIndex = (currentIndex + 1) % cameraStates.Length;
        if (currentIndex > 0)
        {
            canControlCamera = false;
            worldZoomController.MakeSliderNonInteractable();
            cam.fieldOfView = 35;
        }
        else if (currentIndex == 0)
        {
            canControlCamera = true;
            worldZoomController.MakeSliderInteractable();
            cam.fieldOfView = 53;
        }
        canRotateCamera = false;
        isDragging = false;
        ApplyCameraState(currentIndex);

    }

    void ApplyCameraState(int index)
    {
        transform.position = cameraStates[index].position;
        transform.rotation = cameraStates[index].rotation;
        transform.LookAt(lookAt);
        Vector3 euler = transform.eulerAngles;
        currentHorizontalRotation = euler.y;
        currentVerticalRotation = euler.x;
        //targetPosition = transform.position;
    }
    private void DetectDraggingAndZoom()
    {
        if (!WorldGameManager.Instance.isGamePaused)
        {
            // --- Single finger drag (rotation) ---
            if (Input.touchCount == 1 && !WorldGameManager.Instance.ballClicked && WorldGameManager.Instance.isGameStarted)
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
                            float rotX = delta.x * rotationSpeed;
                            float rotY = -delta.y * rotationSpeed;

                            currentHorizontalRotation += rotX;
                            currentVerticalRotation += rotY;

                            currentVerticalRotation = Mathf.Clamp(currentVerticalRotation, minVerticalAngle, maxVerticalAngle);

                            // Set target rotation instead of applying directly
                            targetRotation = Quaternion.Euler(currentVerticalRotation, currentHorizontalRotation, 0f);
                            canRotateCamera = true;

                            lastTouchPosition = touch.position;


                        }
                        break;

                    case TouchPhase.Ended:
                        isDragging = false;
                        break;
                }
            }

            // --- Two finger pinch (zoom) ---
            if (Input.touchCount == 2 && !WorldGameManager.Instance.ballClicked && WorldGameManager.Instance.isGameStarted)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                Vector2 touch0Prev = touch0.position - touch0.deltaPosition;
                Vector2 touch1Prev = touch1.position - touch1.deltaPosition;

                float prevMagnitude = (touch0Prev - touch1Prev).magnitude;
                float currentMagnitude = (touch0.position - touch1.position).magnitude;

                float difference = currentMagnitude - prevMagnitude;

                // Adjust target FOV
                targetFov -= difference * zoomSpeed * 0.1f;
                targetFov = Mathf.Clamp(targetFov, minFov, maxFov);
                if (cam.fieldOfView >= minFov && cam.fieldOfView <= maxFov)
                    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 5f * Time.deltaTime);
            }







            if (canRotateCamera)
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);




        }
    }

    public void FollowBall()
    {
        if (!canControlCamera)
            return;
        // Calculate desired camera position (x stays with ball, y stays constant, z offset)
        float finalLookAtZposition = lookAt.position.z - zPosition;
        targetPosition = new Vector3(lookAt.position.x, transform.position.y, finalLookAtZposition);

        // Smoothly move camera towards target position


        if (WorldGameManager.Instance.ballMoving)
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
            currentVerticalRotation = euler.x;// keep rotation synced
        }

    }
}
