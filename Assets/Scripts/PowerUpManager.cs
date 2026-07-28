using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpManager : MonoBehaviour
{
    public TextMeshProUGUI currencyText;
    public Button[] powerUpButtons;
    public Animator powerupPanel;
    public GameObject clickPowerUpButton;
    public GameObject[] allObstacles;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateCurrencyAmount();
        CheckIfCanDisableButton();
    }

    private void UpdateCurrencyAmount()
    {
        float currencyAmount = CurrencyManager.Instance.GetMoney();
        currencyText.text = currencyAmount.ToString();
    }

    private void CheckIfCanDisableButton()
    {
        float currencyAmount = CurrencyManager.Instance.GetMoney();
        if (currencyAmount >= 15)
        {
            foreach (Button button in powerUpButtons)
            {
                button.enabled = true;
            }
        }
        else
        {
            foreach (Button button in powerUpButtons)
            {
                button.enabled = false;
            }
        }

    }
    public async void RedoLastShot()
    {
        float currencyAmount = CurrencyManager.Instance.GetMoney();
        if (currencyAmount >= 15)
        {
            GameManager.Instance.RedoLastShot();
           await CurrencyManager.Instance.SpendMoney(15f);
        }else
        {
            Debug.Log("Not enough money");
        }
    }

    public async void FreezeTimer()
    {
        float currencyAmount = CurrencyManager.Instance.GetMoney();
        if (currencyAmount >= 15)
        {
            GameManager.Instance.PauseTimerForFiveSeconds();
            await CurrencyManager.Instance.SpendMoney(15f);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

    public async void RemoveObstacles()
    {
        float currencyAmount = CurrencyManager.Instance.GetMoney();
        if (currencyAmount >= 15)
        {
            RemoveObstacle();
            await CurrencyManager.Instance.SpendMoney(15f);
        }
        else
        {
            Debug.Log("Not enough money");
        }
    }

    public void HandlePowerUpPanel(int index)
    {

        if (index == 0)
        {
            powerupPanel.SetTrigger("show");
            clickPowerUpButton.SetActive(false);
        }
        else if (index == 1)
        {
            powerupPanel.SetTrigger("hide");
            clickPowerUpButton.SetActive(true);
        }

    }

    private void RemoveObstacle()
    {
        if (allObstacles.Length > 0 && allObstacles != null)
        {
            for (int i = 0; i < allObstacles.Length; i++)
            {

                if (allObstacles[i].activeInHierarchy)
                {
                    allObstacles[i].SetActive(false);
                    break;
                }
            }
        }
    }

   
}
