using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log("Signed in! PlayerID: " +
                      AuthenticationService.Instance.PlayerId);
        }

        // Initialize currency after login
        if (CurrencyManager.Instance != null)
        {
            await CurrencyManager.Instance.Initialize();
            Debug.Log("Cloud Money: " + CurrencyManager.Instance.GetMoney());
        }

        // Check for an unclaimed daily reward once currency is ready, so
        // AddMoney has a valid loaded balance to add on top of.
        if (DailyRewardManager.Instance != null)
        {
            await DailyRewardManager.Instance.CheckAndClaimDailyRewardAsync();
        }

        IsReady = true;

        Debug.Log("UGS Ready");
    }

    public static async Task WaitUntilReady()
    {
        while (!IsReady)
        {
            await Task.Delay(100);
        }
    }
}