using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class WorldZoomController : MonoBehaviour
{
    public Camera mainCamera;
    public Slider zoomSlider;
    private WorldCameraController cameraController;
    private float xPosition, yPosition, zPosition;


    private float initialZ;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraController = FindAnyObjectByType<WorldCameraController>();
        if (mainCamera == null)
            mainCamera = Camera.main;






        // Save the starting Z position
        initialZ = mainCamera.transform.position.z;

        // Setup slider
        zoomSlider.minValue = 12.5f;   // Move back
        zoomSlider.maxValue = 17.5f;    // Move forward
        zoomSlider.value = mainCamera.transform.position.z;        // Start at default

        zoomSlider.onValueChanged.AddListener(UpdatePosition);
        Debug.Log(zoomSlider.value);
    }

    private void UpdatePosition(float value)
    {
        Debug.Log("Zoom Slider Changed:");
        Debug.Log(value);
        // Camera moves smoothly forward/backward along z-axis
        Vector3 targetPosition = new Vector3(
            mainCamera.transform.position.x,
            mainCamera.transform.position.y,
            value   // Adjust relative to starting Z
        );
        Debug.Log(targetPosition);
        WorldGameManager.Instance.targetPosition = targetPosition;
       WorldGameManager.Instance.ApplySliderInstructions();
        cameraController.LookAtBall();


    }
    void LateUpdate()
    {

        if (!zoomSlider.interactable) return;
        zoomSlider.value = mainCamera.transform.position.z;
    }
    // Update is called once per frame
    void Update()
    {

    }

    public void MakeSliderNonInteractable()
    {
        zoomSlider.interactable = false;
    }

    public void MakeSliderInteractable()
    {
        zoomSlider.interactable = true;
    }

    public void ClickOnSlider()
    {
        WorldGameManager.Instance.ballClicked = true;
        Debug.Log(WorldGameManager.Instance.ballClicked);
    }

    public void RemoveClickOnSlider()
    {
        WorldGameManager.Instance.ballClicked = false;
    }


}
