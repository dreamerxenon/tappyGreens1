using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class WorldGameManager : MonoBehaviour
{
    public static WorldGameManager Instance;
   
    public bool isGameStarted, isGameWon, isGamePaused;
    public bool ballMoving,ballClicked,tapOnSlider;
    public Vector3 targetPosition;
    private Camera mainCamera;
    public GameObject pausePanel;
    public AudioSource audiosouce;
    public AudioClip[] audioClips;
    public AudioSource[] effectsAudioSource;
    public AudioSource[] musicAudioSource;
    public Slider effectsSlider, musicSlider;
    public FreeWorldRunManager freeWorldRunManager;
    public GameObject displayHud,defaultCourse;
    public GameObject[] nonDisplayHud;
    public GameObject strokesPanel, timerPanel;
    public bool fastestTime, lowestStrokes;
    private Vector3 initialCameraPosition;
    private Quaternion initialCameraRotation;
    public GameObject player;
    private Vector3 initialPlayerPosition;
    public GameObject leaderBoardPanel;
    public FreeWorldLeaderboardUI freeWorldLeaderboardUI;
    public GameObject noInternetUI, loadingIndicator;
    public Button retryButton;
    public float checkInterval = 10f;
    private bool isConnected;
    public GameObject endingPanel,errorPanel;
    public Animator errorText;
    public int starsThreshold;
    public TextMeshProUGUI numbOfStarsText;
    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;


        }
        else
        {
            Destroy(gameObject);
        }
        isGameStarted = false;

        if (mainCamera == null)
            mainCamera = Camera.main;


        int numberOfStars = PlayerPrefs.GetInt("NumberOfStars");
        numbOfStarsText.text = numberOfStars.ToString();
        CheckIfNumberOfStarsExceedThreshold(numberOfStars);

    }

    private void CheckIfNumberOfStarsExceedThreshold(int stars)
    {
        if(stars < starsThreshold)
        {
            errorPanel.SetActive(true);
            errorText.SetTrigger("show");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

        // Start background internet checking
        StartCoroutine(CheckInternetLoop());


        initialCameraPosition  = mainCamera.transform.position;
        initialCameraRotation = mainCamera.transform.rotation;  
        initialPlayerPosition = player.transform.position;
        StartHandleAndMusicVolume();
    }

    // Update is called once per frame
    void Update()
    {
        HandleMusicAndEffectsVolume();
        if(isGameStarted)
        {
            CheckForInternetConnectiontoContinueGameTimer();
        }

    }

    private void CheckForInternetConnectiontoContinueGameTimer()
    {
        if(!isConnected)
        {
            freeWorldRunManager.PauseGame();
        }else if(isConnected)
        {
            freeWorldRunManager.ResumeGame();
        }
    }
    public void ApplySliderInstructions()
    {
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, 0.1f);
    }
    public void StartGame()
    {
        if (isConnected)
        {
            isGameStarted = true;
            isGamePaused = false;
            defaultCourse.SetActive(false);
            freeWorldRunManager.StartGame();
            displayHud.SetActive(false);

            for (int i = 0; i < nonDisplayHud.Length; i++)
            {
                nonDisplayHud[i].SetActive(true);
            }
            CheckForChallengeType();
        }
    }

    private void CheckForChallengeType()
    {
        if (lowestStrokes)
        {
            strokesPanel.SetActive(true);
            timerPanel.SetActive(false);
        }
        else if (fastestTime)
        {
            strokesPanel.SetActive(false);
            timerPanel.SetActive(true);
        }
    }
    public void ShowPausePanel()
    {
        pausePanel.SetActive(true);
        isGamePaused = true;
        freeWorldRunManager.PauseGame();
    }
    public void GameWon()
    {
        freeWorldRunManager.OnCourseCompleted();
    }
    public void HidePausePanel()
    {
        pausePanel.SetActive(false);
        isGamePaused = false;
        freeWorldRunManager.ResumeGame();
            
    }

    public void ButtonTap()
    {
        audiosouce.clip = audioClips[3];
        audiosouce.Play();
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
    public void GoBack()
    {
        isGameStarted = false;

        for (int i = 0; i < nonDisplayHud.Length; i++)
        {
            nonDisplayHud[i].SetActive(false);
        }
       displayHud.SetActive(true);
        strokesPanel.SetActive(false);
        timerPanel.SetActive(false);
        pausePanel.SetActive(false);
        freeWorldRunManager.ExitFreeWorld();
        mainCamera.transform.position = initialCameraPosition;
       // player.transform.position = initialPlayerPosition;
       player.GetComponent<Rigidbody>().position = initialPlayerPosition;
        mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, initialCameraRotation, 1f);
        endingPanel.SetActive(false);
        defaultCourse.SetActive(true);
        
    }

    public void GoHome()
    {
        SceneManager.LoadScene(0);
    }
    public void ChangeEffectsVolume(float volume)
    {
        PlayerPrefs.SetFloat("effectsVolume", volume);
    }

    public void ChangeMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
    private void HandleMusicAndEffectsVolume()
    {

        if (PlayerPrefs.HasKey("MusicVolume"))
        {
            for (int i = 0; i < musicAudioSource.Length; i++)
            {
                if (musicAudioSource[i] != null)
                {
                    musicAudioSource[i].volume = PlayerPrefs.GetFloat("MusicVolume");
                }
                if (musicSlider != null)
                {
                    musicSlider.value = PlayerPrefs.GetFloat("MusicVolume");
                }
            }
        }

        if (PlayerPrefs.HasKey("effectsVolume"))
        {
            for (int i = 0; i < effectsAudioSource.Length; i++)
            {
                if (effectsAudioSource[i] != null)
                {
                    effectsAudioSource[i].volume = PlayerPrefs.GetFloat("effectsVolume");
                }
                if (effectsSlider != null)
                {
                    effectsSlider.value = PlayerPrefs.GetFloat("effectsVolume");
                }
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
    private void StartHandleAndMusicVolume()
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

    public void RegisterStroke()
    {
        
        freeWorldRunManager.UpdateStrokes(1);
    }

    public void OpenLeaderboard()
    {
        leaderBoardPanel.SetActive(true);
        freeWorldLeaderboardUI.OpenLeaderboard();
    }

    public void CloseLeaderboard()
    {
        leaderBoardPanel.SetActive(false);
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
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(true);
        }
        StartCoroutine(CheckInternet());
    }

}
