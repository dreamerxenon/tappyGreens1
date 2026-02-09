using System.Collections;
using UnityEngine;

public class MovingWall : MonoBehaviour
{
    private bool movingUp = false;
    private Vector3 upPos, bottomPos;
    private float moveDuration = 1f;
    private float waitTime = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        upPos = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
        bottomPos = transform.position;

        StartCoroutine(HoopMovement());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    
    IEnumerator HoopMovement()
    {
        while (true)
        {


            Vector3 startPos = transform.position;
            Vector3 endPos = movingUp ? bottomPos : upPos;
            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                float t = elapsed / moveDuration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endPos;
            movingUp = !movingUp;
            yield return new WaitForSeconds(waitTime);
        }
    }
}
