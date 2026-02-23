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
        float currency = PlayerPrefs.GetFloat(CoinKey, 0);
        currencyText.text = currency.ToString();
    }

    private void Update()
    {
        float currency = PlayerPrefs.GetFloat(CoinKey, 0);
        currencyText.text = currency.ToString();
    }

    public float GetCoins()
    {
        return PlayerPrefs.GetFloat(CoinKey, 0);
    }

    public void AddCoins(float amount)
    {
        PlayerPrefs.SetFloat(CoinKey, GetCoins() + amount);
        PlayerPrefs.Save();
    }

    public void SpendCoins(float amount)
    {
        PlayerPrefs.SetFloat(CoinKey, GetCoins() - amount);
        PlayerPrefs.Save();
    }
}
