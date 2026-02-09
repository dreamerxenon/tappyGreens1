using UnityEngine;

public class HoleTrigger : MonoBehaviour
{
    public Transform holeCenter;
    public float attractionForce = 1f;
    public AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Golf") || other.CompareTag("2ndBall"))
        {
              Rigidbody rb  = other.GetComponent<Rigidbody>();
            if(rb != null )
            {
                if (rb.linearVelocity.magnitude < 5.5f)
                {


                    Vector3 direction = (holeCenter.position - other.transform.position).normalized;
                    rb.linearVelocity = direction * attractionForce;
                }
                if(Vector3.Distance(other.transform.position, holeCenter.position) < 0.1f)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    audioSource.Play();
                }

            }
        }
        if(other.CompareTag("2ndBall"))
        {
            GameManager.Instance.secondBall = true;
        }
    }
}
