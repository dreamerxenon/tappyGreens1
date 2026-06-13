using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class ZoomController : MonoBehaviour
{
    public Camera mainCamera;
    public Slider zoomSlider;
    private CameraController cameraController;
    private float xPosition, yPosition,zPosition;
   

    private float initialZ;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraController = FindAnyObjectByType<CameraController>();
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
        GameManager.Instance.targetPosition = targetPosition;
        GameManager.Instance.ApplySliderInstructions();
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
        GameManager.Instance.ballClicked = true;
        GameManager.Instance.tapOnSlider = true;
        Debug.Log(GameManager.Instance.ballClicked);
    }

    public void RemoveClickOnSlider()
    {
        GameManager.Instance.ballClicked = false;
        GameManager.Instance.tapOnSlider = false;
    }

    
}
