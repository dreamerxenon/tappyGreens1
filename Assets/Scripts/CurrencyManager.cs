using TMPro;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    private const string CoinKey = "Money";
    public TextMeshProUGUI currencyText;

    private void Awake()
    {
        Instance = this;
    }

   void Start()
    {
       //PlayerPrefs.SetInt(CoinKey, 10000);
        int currency = PlayerPrefs.GetInt(CoinKey, 0);
        currencyText.text = currency.ToString();
    }

    private void Update()
    {
        int currency = PlayerPrefs.GetInt(CoinKey, 0);
        currencyText.text = currency.ToString();
    }

    public int GetCoins()
    {
        return PlayerPrefs.GetInt(CoinKey, 0);
    }

    public void AddCoins(int amount)
    {
        PlayerPrefs.SetInt(CoinKey, GetCoins() + amount);
        PlayerPrefs.Save();
    }

    public void SpendCoins(int amount)
    {
        PlayerPrefs.SetInt(CoinKey, GetCoins() - amount);
        PlayerPrefs.Save();
    }
}
