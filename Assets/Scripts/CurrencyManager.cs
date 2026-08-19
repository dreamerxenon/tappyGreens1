using TMPro;
using UnityEngine;
using System.Threading.Tasks;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    private const string MoneyKey = "Money";

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
            // Don't let a failed migration block the rest of Initialize -
            // MigratePlayerPrefs only marks itself done on success, so
            // it'll safely retry next launch.
            Debug.LogError($"[CurrencyManager] Migration failed, will retry next launch: {e.Message}");
        }

        // Always load latest value from Cloud Save - but fall back to the
        // last cached value on failure instead of throwing, so RefreshUI
        // below always runs and the UI never gets stuck blank/stale.
        try
        {
            currentMoney = await CloudSaveManager.Instance.LoadMoney();

            // Cache locally
            PlayerPrefs.SetFloat(MoneyKey, currentMoney);
            PlayerPrefs.Save();

            Debug.Log($"Money Loaded: {currentMoney}");
        }
        catch (System.Exception e)
        {
            currentMoney = PlayerPrefs.GetFloat(MoneyKey, 0f);
            Debug.LogError(
                $"[CurrencyManager] LoadMoney failed, using cached value {currentMoney}: {e.Message}");
        }

        RefreshUI();
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
    /// PlayerPrefs still holds the correct value locally in the meantime,
    /// and the next successful save (e.g. from a later purchase) will
    /// push the current total up to Cloud Save.
    /// </summary>
    private async Task SafeSaveMoney()
    {
        try
        {
            await CloudSaveManager.Instance.SaveMoney(currentMoney);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CurrencyManager] SaveMoney failed, will retry on next save: {e.Message}");
        }
    }
}