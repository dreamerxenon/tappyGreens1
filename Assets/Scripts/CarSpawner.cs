using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public GameObject[] forwardCars;
    public GameObject[] backCars;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating("SpawnCars", .1f, 2.5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnCars()
    {
        int index  =  Random.Range(0, forwardCars.Length);
        Vector3 leftForwardPos = new Vector3(1.460524f, -0.2999573f, -17.72983f);
        Vector3 leftbackPos = new Vector3(-2.782722f, -0.2999587f, -19.63924f);
        Vector3 rightForwardPos = new Vector3(1.69f, -0.2999573f, 56.73f);
        Vector3 rightbackPos = new Vector3(-3.073438f, -0.2999582f, 55.11863f);
        Instantiate(forwardCars[index], leftForwardPos, forwardCars[index].transform.rotation);
        Instantiate(backCars[index], leftbackPos , backCars[index].transform.rotation);
        Instantiate(forwardCars[index], rightForwardPos, forwardCars[index].transform.rotation);
        Instantiate(backCars[index], rightbackPos, backCars[index].transform.rotation);
    }
}
