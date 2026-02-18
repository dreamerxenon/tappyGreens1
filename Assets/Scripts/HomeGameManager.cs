using System.Collections;
using System.Dynamic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeGameManager : MonoBehaviour
{
    public GameObject playBtn, setingsBtn,rankingBtn, showAdsPanel,levelLoadingImg,characterBtn;
    public Animator settingsAnim,errorTextAnim,errorAnim;
    public AudioSource[] effectsAudioSource;
    public AudioSource[] musicAudioSource;
    public Slider effectsSlider, musicSlider;
    public AudioSource audioSource;
    public TextMeshProUGUI starText,errorText,characterText;
    public GameObject[] uiElements;
    public GameObject lvlPanel,resetPanel,normalSettings;
    public bool  levelButton;
    public RawImage levelImage;
    public Texture[] levelImageTexture;
    public GameObject noInternetUI,loadingIndicator;
    public Button retryButton;      
    public float checkInterval = 10f;
    private bool isConnected;
    private const string SelectedKey = "SelectedCharacter";
    public GameObject CharacterChoosePanel, continueBtn;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
        AdsManager.instance.ShowBanner();                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
        starText.text = PlayerPrefs.GetInt("NumberOfStars").ToString();
        HandleMusicAndEffectsVolumeForStart();
        CheckIfSelectedCharacter();
         if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            noInternetUI.SetActive(true);
        }else
        {
            isConnected = true;
        }

        if (retryButton != null)
            retryButton.onClick.AddListener(RetryCheck);

        // Start background internet checking
        StartCoroutine(CheckInternetLoop());

       


    }

    // Update is called once per frame
    void Update()
    {
        starText.text = PlayerPrefs.GetInt("NumberOfStars").ToString();
        HandleMusicAndEffectsVolumeForUpdate();
        DisplayCharacterText();
        CheckIfContinueButtonShouldBeActive();
    }

    private void HandleMusicAndEffectsVolumeForUpdate()
    {
        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            for (int i = 0; i < musicAudioSource.Length; i++)
            {
                musicAudioSource[i].volume = PlayerPrefs.GetFloat("MusicVolume");
                musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
            }
        }

        if (PlayerPrefs.HasKey("effectsVolume"))
        {
            for (int i = 0; i < effectsAudioSource.Length; i++)
            {
                effectsAudioSource[i].volume = PlayerPrefs.GetFloat("effectsVolume");
                effectsSlider.value = PlayerPrefs.GetFloat("effectsVolume");
            }
        }

        if (effectsSlider != null)
        {
            effectsSlider.onValueChanged.AddListener(ChangeEffectsVolume);
        }
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(ChangeMusicVolume);
        }
    }

    private void HandleMusicAndEffectsVolumeForStart()
    {
        effectsSlider.minValue = musicSlider.minValue = 0f;
        musicSlider.maxValue = effectsSlider.maxValue = 1f;
        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            for (int i = 0; i < musicAudioSource.Length; i++)
            {
                musicAudioSource[i].volume = PlayerPrefs.GetFloat("MusicVolume");
                musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
            }
        }
        else
        {
            for (int i = 0; i < musicAudioSource.Length; i++)
            {
                musicSlider.value = musicAudioSource[i].volume;
            }
        }

        if (PlayerPrefs.HasKey("effectsVolume"))
        {
            for (int i = 0; i < effectsAudioSource.Length; i++)
            {
                effectsAudioSource[i].volume = PlayerPrefs.GetFloat("effectsVolume");
                effectsSlider.value = PlayerPrefs.GetFloat("effectsVolume");

            }
        }
        else
        {
            for (int i = 0; i < effectsAudioSource.Length; i++)
            {
                effectsSlider.value = effectsAudioSource[i].volume;
            }
        }
    }

    private void DisplayCharacterText()
    {
        CharacterData characterData = CharacterManager.Instance.GetSelectedCharacter();
        string savedID = PlayerPrefs.GetString(SelectedKey, "");
        if (!string.IsNullOrEmpty(savedID))
        {
            characterText.text = characterData.displayName;
        }
    }

    private void CheckIfContinueButtonShouldBeActive()
    {
        string savedID = PlayerPrefs.GetString(SelectedKey, "");
        if (!string.IsNullOrEmpty(savedID))
        {
            if (continueBtn != null)
                continueBtn.SetActive(true);
        }
    }
    private void CheckIfSelectedCharacter()
    {
        string savedID = PlayerPrefs.GetString(SelectedKey, "");
        if (string.IsNullOrEmpty(savedID))
        {
            if (CharacterChoosePanel != null)
                CharacterChoosePanel.SetActive(true);
        }
    }
    private void OnEnable()
    {
        AdsManager.OnRewardedAdCompleted += HandleRewardedAdCompleted;
    }

    private void OnDisable()
    {
        AdsManager.OnRewardedAdCompleted -= HandleRewardedAdCompleted;
    }
    private void HandleRewardedAdCompleted()
    {
        AddHeart();

        // Preload the next ad
        AdsManager.instance.RequestRewarded();
    }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   
    public void ShowRewardedAd()
    {
        AdsManager.instance.ShowRewarded();
    }
    private void AddHeart()
    {
        int currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        if (currentHeart <= 5)
        {
            PlayerPrefs.SetInt("CurrentHeart", currentHeart + 1);
            Debug.Log(PlayerPrefs.GetInt("CurrentHeart"));         
        }

    }
    public void ChangeEffectsVolume(float volume)
    {
        PlayerPrefs.SetFloat("effectsVolume", volume);
    }

    public void ChangeMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }


    public void ShowSettings()
    {
        if (isConnected)
        {
            settingsAnim.SetTrigger("Show");
            playBtn.SetActive(false);
            setingsBtn.SetActive(false);
            rankingBtn.SetActive(false);
            characterBtn.SetActive(false) ;
        }
    }

    public void HideSettings()
    {
        if (isConnected)
        {
            settingsAnim.SetTrigger("Hide");
            playBtn.SetActive(true);
            setingsBtn.SetActive(true);
            rankingBtn.SetActive(true);
            characterBtn.SetActive(true);
        }
    }

    public void ButtonTap()
    {
        
        audioSource.Play();
    }

    
    public void PlayGame()
    {
        if (isConnected)
        {
            for (int i = 0; i < uiElements.Length; i++)
            {
                uiElements[i].SetActive(false);
            }
            audioSource.Play();
            lvlPanel.SetActive(true);
        }
    }

    public void RemovePlayGame()
    {
        if (isConnected)
        {
            lvlPanel.SetActive(false);
           
                audioSource.Play();
                uiElements[0].SetActive(true);
            
        }
       
    }

   public void ShowLevelPlayBtn(int currrentLevel,int levelThreshold)
    {
        if (isConnected)
        {
            audioSource.Play();
            Debug.Log(levelButton);
            int numberOfStars = PlayerPrefs.GetInt("NumberOfStars");
            if (numberOfStars < levelThreshold && levelThreshold != 0)
            {
                errorTextAnim.SetTrigger("show");
                errorText.text = "YOU NEED " + levelThreshold + " STARS \n TO UNLOCK THIS LEVEL";

            }
            else
            {
                
                PlayerPrefs.SetInt("CurrentLevel", currrentLevel);
                PlayLevel();

            }
          
        }
       

    }

   

    public void PlayLevel()
    {
       int  currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        if (isConnected && currentHeart > 0)
        {
            levelLoadingImg.SetActive(true);
            int currLvl = PlayerPrefs.GetInt("CurrentLevel");
            audioSource.Play();
            SceneManager.LoadScene(currLvl);
        }
        else if(isConnected && currentHeart <= 0)
        {
            errorAnim.SetTrigger("show");
            Invoke("hideErrorAnim", 2f);
        }
    }

   private void hideErrorAnim()
    {
        errorAnim.SetTrigger("hide");
    }


    public void HideErrorTextAnim()
    {
        if (isConnected)
        {
            audioSource.Play();
            errorTextAnim.SetTrigger("hide");
        }
    }

    public void ControlResetResetPanel(int indicator)
    {
        if (isConnected)
        {
            switch (indicator)
            {
                case 0:
                    resetPanel.SetActive(false);
                    normalSettings.SetActive(true);
                    break;
                case 1:
                    normalSettings.SetActive(false);
                    resetPanel.SetActive(true);
                    break;
            }
        }
    }

    public void ResetGame()
    {
        if (isConnected)
        {
           PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            PlayerPrefs.SetInt("CurrentHeart", 6);
            SceneManager.LoadScene(0);
        }
    }

    IEnumerator CheckInternetLoop()
    {
        while (true)
        {
            yield return CheckInternet();
            yield return new WaitForSeconds(checkInterval);
        }
    }

    IEnumerator CheckInternet()
    {
        // Show loading indicator (during auto-check or retry)
       // if 

        using (UnityWebRequest request = UnityWebRequest.Head("https://www.google.com"))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();

            bool newState = request.result == UnityWebRequest.Result.Success;

            if (newState != isConnected)
            {
                isConnected = newState;
                HandleConnectionChange(isConnected);
            }
        }

        // Hide loading indicator after check
        if (loadingIndicator != null) loadingIndicator.SetActive(false);
    }

    void HandleConnectionChange(bool connected)
    {
        if (!connected)
        {
            noInternetUI?.SetActive(true);
            
        }
        else
        {
            noInternetUI?.SetActive(false);
            
        }
    }

    public void RetryCheck()
    {
        Debug.Log("Retry pressed.");
        if(loadingIndicator != null)
            { loadingIndicator.SetActive(true);
        }
        StartCoroutine(CheckInternet());
    }

    public void ShowShowAdsPanel()
    {
        if (isConnected)
        {
            showAdsPanel.SetActive(true);
        }
    }
    public void RemoveShowAdsPanel()
    {
        if (isConnected)
        {
            showAdsPanel.SetActive(false);
        }
    }

    public void RemoveCharacterChoosePanel()
    {
        if (CharacterChoosePanel != null)
            CharacterChoosePanel.SetActive(false);
    }

    public void ShowCharacterChoosePanel()
    {
        if (CharacterChoosePanel != null)
            CharacterChoosePanel.SetActive(true);
    }

}
