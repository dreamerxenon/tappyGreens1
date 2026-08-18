using System.Collections.Generic;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    public GameObject[] forwardCars;
    public GameObject[] backCars;

    private Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
    private int _poolSizePerPrefab = 6; // tune based on how many can be alive at once

    void Start()
    {
        PrewarmPools(forwardCars);
        PrewarmPools(backCars);
        InvokeRepeating(nameof(SpawnCars), 0.1f, 2.5f);
    }

    private void PrewarmPools(GameObject[] prefabs)
    {
        foreach (var prefab in prefabs)
        {
            if (_pools.ContainsKey(prefab)) continue;

            var queue = new Queue<GameObject>();
            for (int i = 0; i < _poolSizePerPrefab; i++)
            {
                GameObject obj = Instantiate(prefab);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }
            _pools[prefab] = queue;
        }
    }

    public void SpawnCars()
    {
        int leftForwardIndex = Random.Range(0, forwardCars.Length);
        int leftBackIndex = Random.Range(0, backCars.Length);
        int rightForwardIndex = Random.Range(0, forwardCars.Length);
        int rightBackIndex = Random.Range(0, backCars.Length);

        Vector3 leftForwardPos = new Vector3(1.460524f, -0.2999573f, -17.72983f);
        Vector3 leftbackPos = new Vector3(-2.782722f, -0.2999587f, -19.63924f);
        Vector3 rightForwardPos = new Vector3(1.69f, -0.2999573f, 56.73f);
        Vector3 rightbackPos = new Vector3(-3.073438f, -0.2999582f, 55.11863f);

        SpawnFromPool(forwardCars[leftForwardIndex], leftForwardPos);
        SpawnFromPool(backCars[leftBackIndex], leftbackPos);
        SpawnFromPool(forwardCars[rightForwardIndex], rightForwardPos);
        SpawnFromPool(backCars[rightBackIndex], rightbackPos);
    }

    private void SpawnFromPool(GameObject prefab, Vector3 position)
    {
        Queue<GameObject> queue = _pools[prefab];
        GameObject car = queue.Count > 0 ? queue.Dequeue() : Instantiate(prefab);

        car.transform.SetPositionAndRotation(position, prefab.transform.rotation);

        var controller = car.GetComponent<CarController>();
        controller.sourcePrefab = prefab;
        controller.spawner = this;
        controller.ResetForReuse(position, prefab.transform.rotation);

        car.SetActive(true);
    }

    public void ReturnToPool(GameObject car, GameObject sourcePrefab)
    {
        car.SetActive(false);
        _pools[sourcePrefab].Enqueue(car);
    }
}