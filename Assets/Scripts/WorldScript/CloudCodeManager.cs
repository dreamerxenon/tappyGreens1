using Unity.Services.CloudCode;
using UnityEngine;
using System.Threading.Tasks;

public class CloudCodeManager : MonoBehaviour
{
    public static CloudCodeManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<DailyFreeWorldData> GetDailyFreeWorld()
    {
        var result = await CloudCodeService.Instance.CallEndpointAsync<DailyFreeWorldData>(
            "GetDailyFreeWorld");

        return result;
    }
}