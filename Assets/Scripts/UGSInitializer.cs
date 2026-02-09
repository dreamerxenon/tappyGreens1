using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in! PlayerID: " + AuthenticationService.Instance.PlayerId);
        }

        IsReady = true;
    }

    public static async Task WaitUntilReady()
    {
        while (!IsReady)
        {
            await Task.Delay(100);
        }
    }
}
