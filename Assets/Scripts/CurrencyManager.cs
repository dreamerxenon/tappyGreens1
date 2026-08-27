using TMPro;
using UnityEngine;
using System.Threading.Tasks;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    private const string MoneyKey = "Money";
    private const string PendingSyncKey = "Money_PendingCloudSync";

    [SerializeField] private TextMeshProUGUI currencyText;

    private float currentMoney;

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

    public async Task Initialize()
    {
        System.Exception firstError = null;

        // First launch only. CloudSaveManager.MigratePlayerPrefs() already
        // guards itself with its own "CloudMigration" flag and returns
        // immediately (no network call) once migrated, so it's safe and
        // cheap to call unconditionally here - no need for a second flag.
        try
        {
            await CloudSaveManager.Instance.MigratePlayerPrefs();
        }
        catch (System.Exception e)
        {
            firstError = e;
            Debug.LogError($"[CurrencyManager] Migration failed, will retry next launch: {e.Message}");
        }

        bool hasPendingLocalChange = PlayerPrefs.GetInt(PendingSyncKey, 0) == 1;

        if (hasPendingLocalChange)
        {
            // A previous spend/add never made it to the cloud. The cached
            // local value (already updated at the time of that spend/add)
            // is the correct one - loading from cloud now would silently
            // undo that change. Trust local, then try to push it up.
            currentMoney = PlayerPrefs.GetFloat(MoneyKey, 0f);
            Debug.LogWarning($"[CurrencyManager] Resuming with unsynced local balance {currentMoney}, retrying cloud push...");
            RefreshUI();

            await SafeSaveMoney();
        }
        else
        {
            // No pending local change - safe to trust Cloud Save as source of truth.
            try
            {
                currentMoney = await CloudSaveManager.Instance.LoadMoney();

                PlayerPrefs.SetFloat(MoneyKey, currentMoney);
                PlayerPrefs.Save();

                Debug.Log($"Money Loaded: {currentMoney}");
            }
            catch (System.Exception e)
            {
                currentMoney = PlayerPrefs.GetFloat(MoneyKey, 0f);
                firstError ??= e;
                Debug.LogError(
                    $"[CurrencyManager] LoadMoney failed, using cached value {currentMoney}: {e.Message}");
            }

            RefreshUI();
        }

        // UI is already showing the best data we have at this point - this
        // rethrow is purely so UGSInitializer knows the sync wasn't clean
        // and can show its error/retry panel.
        if (firstError != null)
            throw new System.Exception("CurrencyManager.Initialize did not fully succeed.", firstError);
    }

    private void RefreshUI()
    {
        if (currencyText != null)
            currencyText.text = currentMoney.ToString("0");
    }

    public float GetMoney()
    {
        return currentMoney;
    }

    public async Task AddMoney(float amount)
    {
        currentMoney += amount;

        PlayerPrefs.SetFloat(MoneyKey, currentMoney);
        PlayerPrefs.Save();

        RefreshUI();

        await SafeSaveMoney();
    }

    /// <summary>
    /// Same as AddMoney, but skips the Cloud Save write. Use this only
    /// when the caller already knows the server independently wrote the
    /// authoritative new total (e.g. a server-authoritative Cloud Code
    /// script), so re-saving the same number client-side would just be a
    /// wasted round trip.
    /// </summary>
    public void AddMoneyLocalOnly(float amount)
    {
        currentMoney += amount;

        PlayerPrefs.SetFloat(MoneyKey, currentMoney);
        PlayerPrefs.Save();

        RefreshUI();
    }

    public async Task SpendMoney(float amount)
    {
        currentMoney -= amount;

        if (currentMoney < 0)
            currentMoney = 0;

        PlayerPrefs.SetFloat(MoneyKey, currentMoney);
        PlayerPrefs.Save();

        RefreshUI();

        await SafeSaveMoney();
    }

    public async Task SetMoney(float amount)
    {
        currentMoney = amount;

        PlayerPrefs.SetFloat(MoneyKey, currentMoney);
        PlayerPrefs.Save();

        RefreshUI();

        await SafeSaveMoney();
    }

    /// <summary>
    /// Local state and UI are already updated by the time this runs (see
    /// AddMoney/SpendMoney/SetMoney above) - a failed cloud write here
    /// means we're temporarily out of sync with the server, not that the
    /// player's action failed. Log it instead of throwing so callers
    /// (purchase flows, reward grants, etc.) don't get an exception for
    /// something that, from the player's perspective, already succeeded.
    ///
    /// On failure, marks the balance as pending so Initialize() knows to
    /// trust this local value instead of overwriting it with stale cloud
    /// data on next launch, and retries the push then.
    /// </summary>
    private async Task SafeSaveMoney()
    {
        try
        {
            await CloudSaveManager.Instance.SaveMoney(currentMoney);
            PlayerPrefs.SetInt(PendingSyncKey, 0);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CurrencyManager] SaveMoney failed, marking pending for retry next launch: {e.Message}");
            PlayerPrefs.SetInt(PendingSyncKey, 1);
            PlayerPrefs.Save();
        }
    }
}