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
        // First launch only
        await CloudSaveManager.Instance.MigratePlayerPrefs();

        // Always load latest value from Cloud Save
        currentMoney = await CloudSaveManager.Instance.LoadMoney();

        // Cache locally
        PlayerPrefs.SetFloat(MoneyKey, currentMoney);
        PlayerPrefs.Save();

        RefreshUI();

        Debug.Log($"Money Loaded: {currentMoney}");
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

        await CloudSaveManager.Instance.SaveMoney(currentMoney);
    }

    public async Task SpendMoney(float amount)
    {
        currentMoney -= amount;

        if (currentMoney < 0)
            currentMoney = 0;

        PlayerPrefs.SetFloat(MoneyKey, currentMoney);
        PlayerPrefs.Save();

        RefreshUI();

        await CloudSaveManager.Instance.SaveMoney(currentMoney);
    }

    public async Task SetMoney(float amount)
    {
        currentMoney = amount;

        PlayerPrefs.SetFloat(MoneyKey, currentMoney);
        PlayerPrefs.Save();

        RefreshUI();

        await CloudSaveManager.Instance.SaveMoney(currentMoney);
    }
}