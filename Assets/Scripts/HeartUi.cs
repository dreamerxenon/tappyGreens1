using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class HeartUi : MonoBehaviour
{
    public TextMeshProUGUI fuelText;
    public TextMeshProUGUI timerText;
    public GameObject timerPanel,alarmButton;
    void Update()
    {
        if (HeartSystem.Instance == null) return;

        // Show fuel
        fuelText.text = $" {HeartSystem.Instance.currentHeart}/{HeartSystem.Instance.maxHeart}";

        // Show countdown until next fuel
        if (HeartSystem.Instance.currentHeart < HeartSystem.Instance.maxHeart && SceneManager.GetActiveScene().buildIndex == 0 )
        {
            timerPanel.SetActive(true);
            timerText.gameObject.SetActive(true);
            alarmButton.SetActive(true);
            TimeSpan t = HeartSystem.Instance.GetNextHeartTime();
            timerText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);

        }else if(HeartSystem.Instance.currentHeart == 0)
        {
            timerPanel.SetActive(true);
            timerText.gameObject.SetActive(true);
            TimeSpan t = HeartSystem.Instance.GetNextHeartTime();
            timerText.text = string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }
        else 
        {
            timerPanel.SetActive(false);
            alarmButton.SetActive(false);
            timerText.gameObject.SetActive(false);
            if (SceneManager.GetActiveScene().buildIndex == 0)
            {
                FindFirstObjectByType<HomeGameManager>().RemoveShowAdsPanel();
            }
            timerText.text = "Full";
        }
    }

}
