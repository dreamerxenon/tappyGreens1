using System.Collections;
using UnityEngine;

public class CameraTurnTrigger : MonoBehaviour
{
    public Camera mainCamera;
    public float duration = 2f;
    public BoxCollider boxCollider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("camera turn trgger entered");
        if (mainCamera.fieldOfView < 85f)
        {
            StartCoroutine(ZoomOut());
        }
    }


    IEnumerator ZoomOut()
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, 82f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            boxCollider.enabled =  false;
            yield return null;
        }
        mainCamera.fieldOfView = 82f;
    }

    public void EnableCollider()
    {
        boxCollider.enabled = true;
    }
}
