using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LevelButtonScript : MonoBehaviour
{
    public GameObject[] stars;
    public int level;
    public string levelText;
    private bool canStarsBeOn;
    public int levelThreshold;
    private HomeGameManager homeGameManager;
    private Button thisButton;
    public RawImage customBackgroundRawImage;
    public float fadeDuration = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void Start()
    {
        homeGameManager = FindAnyObjectByType<HomeGameManager>();
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(() => homeGameManager.ShowLevelPlayBtn(level,levelThreshold));
       
        string hasStartedLevel = PlayerPrefs.GetString("StartedLevel" + levelText);
       
        if (hasStartedLevel == "true")
        {
            canStarsBeOn = true;

        }
        int numberOfStars = PlayerPrefs.GetInt("NumberOfStars");
        if (numberOfStars < levelThreshold && levelThreshold != 0 && canStarsBeOn == false)
        {

            FadeButtonOpacity(0.3f);
    

            }
        else
        {

            FadeButtonOpacity(1f);

        }
    }

    // Update is called once per frame
    void Update()
    {
       
        int numberOfLevelStars = PlayerPrefs.GetInt("Level" + levelText + "Stars");
        int numberOfStars = PlayerPrefs.GetInt("NumberOfStars");
      
         if(canStarsBeOn)
        {
           

            switch (numberOfLevelStars)
            {
                case 1:
                    stars[0].SetActive(true);
                    break;
                case 2:
                    stars[0].SetActive(true);
                    stars[1].SetActive(true);
                    break;
                case 3:
                    stars[0].SetActive(true);
                    stars[1].SetActive(true);
                    stars[2].SetActive(true);
                    break;
            }


        }

        

    }

    public void FadeButtonOpacity(float targetOpacity)
    {
        if (customBackgroundRawImage != null)
            StartCoroutine(FadeImage(customBackgroundRawImage, targetOpacity));

        Text btnText = thisButton.GetComponentInChildren<Text>();
        if (btnText != null)
            StartCoroutine(FadeText(btnText, targetOpacity));
    }

    private IEnumerator FadeImage(RawImage image, float targetAlpha)
    {
        Color color = image.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            image.color = color;
            time += Time.deltaTime;
            yield return null;
        }

        // Ensure exact final alpha
        color.a = targetAlpha;
        image.color = color;
    }

    private IEnumerator FadeText(Text text, float targetAlpha)
    {
        Color color = text.color;
        float startAlpha = color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            text.color = color;
            time += Time.deltaTime;
            yield return null;
        }

        color.a = targetAlpha;
        text.color = color;
    }
}
