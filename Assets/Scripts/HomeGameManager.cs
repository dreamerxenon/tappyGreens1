using System.Collections;
using System.Dynamic;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeGameManager : MonoBehaviour
{
    public GameObject playBtn, setingsBtn, rankingBtn, showAdsPanel, levelLoadingImg, characterBtn;
    public Animator settingsAnim, errorTextAnim, errorAnim;
    public AudioSource[] effectsAudioSource;
    public AudioSource[] musicAudioSource;
    public Slider effectsSlider, musicSlider;
    public AudioSource audioSource;
    public TextMeshProUGUI starText, errorText, characterText;
    public GameObject[] uiElements;
    public GameObject lvlPanel, resetPanel, normalSettings;
    public bool levelButton;
    public RawImage levelImage;
    public Texture[] levelImageTexture;
    public GameObject noInternetUI, loadingIndicator;
    public Button retryButton;
    public float checkInterval = 10f;
    private bool isConnected;
    private const string SelectedKey = "SelectedCharacter";
    public GameObject CharacterChoosePanel, continueBtn;

    [Header("Camera Movement")]
    public Camera mainCamera;
    public Vector3 levelPanelPosition;
    public Vector3 levelPanelRotation;
    public float cameraTweenDuration = 0.5f;

    private Vector3 _originalCameraPosition;
    private Vector3 _originalCameraRotation;

    void Start()
    {
        AdsManager.instance.ShowBanner();
        starText.text = PlayerPrefs.GetInt("NumberOfStars").ToString();
        HandleMusicAndEffectsVolumeForStart();
        CheckIfSelectedCharacter();
        AttachButtonAnimators();
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            noInternetUI.SetActive(true);
        }
        else
        {
            isConnected = true;
        }

        if (retryButton != null)
            retryButton.onClick.AddListener(RetryCheck);

        StartCoroutine(CheckInternetLoop());

        // Cache the camera's starting transform
        if (mainCamera != null)
        {
            _originalCameraPosition = mainCamera.transform.position;
            _originalCameraRotation = mainCamera.transform.eulerAngles;
        }
    }

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
            characterBtn.SetActive(false);
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

    private void AttachButtonAnimators()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button btn in allButtons)
        {
            if (btn.GetComponent<ButtonAnimator>() == null)
            {
                btn.gameObject.AddComponent<ButtonAnimator>();
            }
        }
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

            // Tween camera to level panel position/rotation
            if (mainCamera != null)
            {
                mainCamera.transform.DOMove(levelPanelPosition, cameraTweenDuration).SetEase(Ease.InOutSine);
                mainCamera.transform.DORotateQuaternion(Quaternion.Euler(levelPanelRotation), cameraTweenDuration).SetEase(Ease.InOutSine);
            }
        }
    }

    public void RemovePlayGame()
    {
        if (isConnected)
        {
            lvlPanel.SetActive(false);
            audioSource.Play();
            uiElements[0].SetActive(true);

            // Tween camera back to original position/rotation
            if (mainCamera != null)
            {
                mainCamera.transform.DOMove(_originalCameraPosition, cameraTweenDuration).SetEase(Ease.InOutSine);
                mainCamera.transform.DORotateQuaternion(Quaternion.Euler(_originalCameraRotation), cameraTweenDuration).SetEase(Ease.InOutSine);
            }
        }
    }

    public void ShowLevelPlayBtn(int currrentLevel, int levelThreshold)
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
        int currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        if (isConnected && currentHeart > 0)
        {
            levelLoadingImg.SetActive(true);
            int currLvl = PlayerPrefs.GetInt("CurrentLevel");
            audioSource.Play();
            SceneManager.LoadScene(currLvl);
        }
        else if (isConnected && currentHeart <= 0)
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

    // Level names exactly as used in GameManager's switch statement
    // (CheckForNumberOfLevelXStars / case 1..30) - each level has three
    // PlayerPrefs keys built from this name: StartedLevel{name},
    // CompletedLevel{name}, Level{name}Stars. Generating from this list
    // instead of hardcoding 90 strings keeps it in sync if levels are
    // ever added/renamed - just keep this list matching GameManager's.
    private static readonly string[] LevelNames = new[]
    {
        "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
        "Eighteen", "Nineteen", "Twenty", "TwentyOne", "TwentyTwo", "TwentyThree",
        "TwentyFour", "TwentyFive", "TwentySix", "TwentySeven", "TwentyEight",
        "TwentyNine", "Thirty",
    };

    // Every PlayerPrefs key this game itself writes anywhere in the
    // project. ResetGame() below deletes ONLY these - never call
    // PlayerPrefs.DeleteAll(), since that also wipes Unity Authentication's
    // cached session token, which silently creates a brand-new anonymous
    // player on the next sign-in and orphans all of this player's
    // currency, Free World progress, and reward-claim history.
    //
    // IMPORTANT: if any other script in the project calls PlayerPrefs.Set*
    // with a key not listed here, add it - otherwise "reset" will leave
    // that piece of state behind instead of actually resetting it.
    private static readonly string[] StaticGameOwnedPlayerPrefsKeys = new[]
    {
        "NumberOfStars",
        "MusicVolume",
        "effectsVolume",
        "CurrentHeart",
        "CurrentLevel",
        SelectedKey, // "SelectedCharacter"
        "Money",
        "CloudMigration",
        "FreeWorld_Date",
        "FreeWorld_CourseIndex",
        "FreeWorld_Completed",
        "FreeWorld_TotalTime",
        "FreeWorld_TotalStrokes",
        "HasPlayedBefore",
        "SelectedCharacter",
        "Unlocked_elite",
        "Unlocked_power",
        "Unlocked_rookie",
        "Unlocked_strategist",
        "Unlocked_TimeCrusher",
        // Read-only in GameManager.cs (GameOver/GameWon) - written
        // elsewhere (AdsManager?). Included since it looks like session
        // state rather than a persistent settings toggle; move it out
        // if that's wrong.
        "CanShowAd",
    };

    private static string[] BuildGameOwnedPlayerPrefsKeys()
    {
        var keys = new System.Collections.Generic.List<string>(StaticGameOwnedPlayerPrefsKeys);

        foreach (string levelName in LevelNames)
        {
            keys.Add("StartedLevel" + levelName);
            keys.Add("CompletedLevel" + levelName);
            keys.Add("Level" + levelName + "Stars");
        }

        return keys.ToArray();
    }

    private static readonly string[] GameOwnedPlayerPrefsKeys = BuildGameOwnedPlayerPrefsKeys();

    public void ResetGame()
    {
        if (isConnected)
        {
            foreach (string key in GameOwnedPlayerPrefsKeys)
            {
                PlayerPrefs.DeleteKey(key);
            }

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
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(true);
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