


using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool ballClicked,ballMoving,isGameStarted,isGameWon =  false,isTimer = false,isGameLost = false;
    public TextMeshProUGUI strokesNumText,victoryTxt,timerTxt,errorText;
    public int strokes;
    private int holeInOne;
    public float timer;
    public Animator holeInOneAnim, loseTxtAnim, startPanelAnim;
    public Animator winScreenAnim, loseScreenAnim;
    public Animator winTxtAnim;
    public AudioSource audiosouce;
    public AudioClip[] audioClips;
    private CameraController cameraController;
    private string[] victoryTexts;
    public GameObject pausePanel;
    public bool isGamePaused,hitPitTrigger,hitmiddleWall,outOfBounds,useBankShot,hitSupports,hitWood,useRamp,hitMushroom,secondBall,hitWindMill,hitMovingWall;
    public bool hitBlackWall;
    public Vector3 startPosition;
    private int initialStrokes;
    public AudioSource[] effectsAudioSource;
    public AudioSource[] musicAudioSource;
    public Slider effectsSlider, musicSlider;
    public GameObject player,errorMessage,timerPanel;
    public GameObject[] stars;
    private int numberOfLvl1Stars, numberOfLvl2Stars,numberOfLvl3Stars, numberOfLvl4Stars, numberOfLvl5Stars;
    private int numberOfLvl6Stars, numberOfLvl7Stars, numberOfLvl8Stars, numberOfLvl9Stars, numberOfLvl10Stars;
    private int numberOfLvl11Stars, numberOfLvl12Stars, numberOfLvl13Stars, numberOfLvl14Stars, numberOfLvl15Stars;
    private int numberOfLvl16Stars, numberOfLvl17Stars, numberOfLvl18Stars, numberOfLvl19Stars, numberOfLvl20Stars;
    private int numberOfLvl21Stars, numberOfLvl22Stars, numberOfLvl23Stars, numberOfLvl24Stars, numberOfLvl25Stars;
    private int numberOfLvl26Stars, numberOfLvl27Stars, numberOfLvl28Stars, numberOfLvl29Stars, numberOfLvl30Stars;
    private int numberOfLvl31Stars, numberOfLvl32Stars, numberOfLvl33Stars, numberOfLvl34Stars, numberOfLvl35Stars;
    private int numberOfLvl36Stars, numberOfLvl37Stars, numberOfLvl38Stars, numberOfLvl39Stars, numberOfLvl40Stars;
    private int numberOfLvl41Stars, numberOfLvl42Stars, numberOfLvl43Stars, numberOfLvl44Stars, numberOfLvl45Stars;
    private int numberOfLvl46Stars, numberOfLvl47Stars, numberOfLvl48Stars, numberOfLvl49Stars, numberOfLvl50Stars;
    private CameraTurnTrigger cameraTurnTrigger;
    private Camera mainCamera;
    public int outOfBoundsInt;
    public GameObject noInternetUI, loadingIndicator;
    public Button retryButton,adBtn;
    public float checkInterval = 10f;
    public bool isConnected;
    public int nextLevelThreshold;
    private Button nextLvl;
    public GameObject levelLoadingImg;
    public Vector3 targetPosition;
    public bool canShowTutorial,testApp;
    private Transform currentHole;
    public Vector3 currentHolePosition;

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

        if(mainCamera == null)
            mainCamera = Camera.main;
        targetPosition = mainCamera.transform.position;
      

       GameObject nextLvlBtn = GameObject.FindWithTag("nextlevel");
        if (nextLvlBtn != null)
        {
            nextLvl = nextLvlBtn.GetComponent<Button>();
        }
        else
        {
            Debug.LogError("Button with tag 'UI_Button' not found!");
        }

        //assign currentHole gameObject
        currentHole = GameObject.FindWithTag("Hole").transform;
        if (currentHole == null) Debug.Log("currenthole not found");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AdsManager.instance.HideBanner();
        if (nextLvl != null)
        {
            nextLvl.onClick.AddListener(() => NextLevel());
        }
        cameraTurnTrigger = FindFirstObjectByType<CameraTurnTrigger>();
        currentHolePosition = currentHole.transform.position;



  
        isGameStarted = false;

        int extraStrokes = CharacterManager.Instance.GetBufferStrokes();

        strokes += extraStrokes;


        initialStrokes = strokes;

        strokesNumText.text = strokes.ToString();

        holeInOne = strokes - 1;
       

        cameraController = FindFirstObjectByType<CameraController>();
        victoryTexts = new string[] { "NICE SHOT!,YOU CRUSHED IT", "TIGER WOODS IS CALLING..HE WANTS TIPS FROM YOU", "NICE, NEXT LEVEL?", "WELL PLAYED! A TRUE MINI GOLF CHAMPION`"};






        StartHandleAndMusicVolume();

       if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Time.timeScale = 0;
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
    }

    private void LateUpdate()
    {
        
       
        

    }

    public void ApplySliderInstructions()
    {
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, 0.1f);
    }

    // Update is called once per frame
    void Update()
    {
        int currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        System.TimeSpan t = HeartSystem.Instance.GetNextHeartTime();
       
        if (currentHeart < 1 && isGameLost == true)
        {
            errorText.text = "YOU DON't HAVE ENOUGH HEARTS" + "\n REFILL:" + string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }
        if (isTimer && isConnected)
        {
            timerPanel.gameObject.SetActive(true);
            if (timer > 0)
            {
                timer -= Time.deltaTime *
         CharacterManager.Instance.GetTimerMultiplier(); ;
                Debug.Log(timer);
                int minutes = Mathf.FloorToInt(timer / 60f);
                int seconds = Mathf.FloorToInt(timer % 60f);
                timerTxt.text = "0" + minutes.ToString() + ":" + seconds.ToString();
            }
        }

        HandleMusicAndEffectsVolume();
        
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
    private void OnEnable()
    {
        AdsManager.OnRewardedAdCompleted += HandleRewardedAdCompleted;
    }

    private void OnDisable()
    {
        AdsManager.OnRewardedAdCompleted -= HandleRewardedAdCompleted;
    }

    public void ShowRewardedAd()
    {
        AdsManager.instance.ShowRewarded();
    }

    private void HandleRewardedAdCompleted()
    {
        AddHeart();

        // Preload the next ad
        AdsManager.instance.RequestRewarded();
    }
    private void AddHeart()
    {
       int currentHeart  =   PlayerPrefs.GetInt("CurrentHeart");
        if(currentHeart <= 5)
        {
            PlayerPrefs.SetInt("CurrentHeart", currentHeart + 1);
        }

    }
    public void DisplayStars(int numberOfStars)
    {
        if (numberOfStars == 1)
        {
            stars[0].SetActive(true);
        }
        else if (numberOfStars == 2)
        {
            stars[0].SetActive(true);
            stars[2].SetActive(true);
        }
        else if (numberOfStars == 3)
        {
            stars[0].SetActive(true);
            stars[1].SetActive(true);
            stars[2].SetActive(true);
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
    public void MakeStroke()
    {
        strokes = strokes - 1;
        strokesNumText.text = strokes.ToString();
            if (strokes == 0)
            {
                isGameStarted = false;
            Invoke("GameOver", 4f);
               }
   
    }

    public void GameOver()
    {
        HeartSystem.Instance.UseHeart();
        int currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        string canShowAd = PlayerPrefs.GetString("CanShowAd");
        if(canShowAd == "true")
        {
            AdsManager.instance.ShowInterstitialforLevels();
        }
        if (isGameWon == false)
        {
            audiosouce.clip = audioClips[2];
            Debug.Log("game over");
            loseScreenAnim.SetTrigger("showlosescreen");
            loseTxtAnim.SetTrigger("showlosetxt");
            audiosouce.Play();
            isGameStarted = false;
            isGameLost = true;
            
            if (currentHeart < 1)
            {
                adBtn.gameObject.SetActive(true);
                
                errorMessage.SetActive(true);
            }
        }
    }

    public void GameWon()
    {
        numberOfLvl1Stars++;numberOfLvl21Stars++;
        numberOfLvl2Stars++;numberOfLvl22Stars++;
        numberOfLvl3Stars++;numberOfLvl23Stars++; 
        numberOfLvl4Stars++;numberOfLvl24Stars++;
        numberOfLvl5Stars++;numberOfLvl25Stars++;
        numberOfLvl6Stars++;numberOfLvl26Stars++;
        numberOfLvl7Stars++;numberOfLvl27Stars++;
        numberOfLvl8Stars++;numberOfLvl28Stars++;
        numberOfLvl9Stars++;numberOfLvl29Stars++;
        numberOfLvl10Stars++;numberOfLvl30Stars++;
        numberOfLvl11Stars++;
        numberOfLvl12Stars++;
        numberOfLvl13Stars++;
        numberOfLvl14Stars++;
        numberOfLvl15Stars++;
        numberOfLvl16Stars++;
        numberOfLvl17Stars++;
        numberOfLvl18Stars++;
        numberOfLvl19Stars++;
        numberOfLvl20Stars++;
        string canShowAd = PlayerPrefs.GetString("CanShowAd");
        if (canShowAd == "true")
        {
            AdsManager.instance.ShowInterstitialforLevels();
        }
        if (strokes == holeInOne && isGameLost == false)
        {
            audiosouce.clip = audioClips[0];
            Debug.Log("Hole In one!!!");
            winScreenAnim.SetTrigger("showwinscreen");
            holeInOneAnim.SetTrigger("showholeinone");
            audiosouce.Play();
            cameraController.ShowBall();
            isGameWon = true;
            isGameStarted = false;
            numberOfLvl1Stars++;
            numberOfLvl16Stars++;
            numberOfLvl21Stars++;
            numberOfLvl25Stars++;
            numberOfLvl30Stars++;

        }
        else if(strokes != holeInOne && isGameLost == false)
        {
            audiosouce.clip = audioClips[1];
            int index = Random.Range(0, 4);
            victoryTxt.text = victoryTexts[index];
            Debug.Log("game won");
            isGameWon = true;
            winScreenAnim.SetTrigger("showwinscreen");
            winTxtAnim.SetTrigger("showwintxt");
            audiosouce.Play();
            cameraController.ShowBall();
            isGameStarted = false;

        }
       

         
         switch(SceneManager.GetActiveScene().buildIndex)
        {
            case 1:
            CheckForNumberOfLevelOneStars(); 
            break;

           case 2:
                CheckForNumberOfLevelTwoStars();
                break;
           case 3:
                CheckForNumberOfLevelThreeStars();
                break;
            case 4:
                CheckForNumberOfLevelFourStars();
                break;
           case 5:
                CheckForNumberOfLevelFiveStars();
                break;
            case 6:
                CheckForNumberOfLevelSixStars();
                break;
            case 7:
                CheckForNumberOfLevelSevenStars();
                break;
            case 8:
                CheckForNumberOfLevelEightStars();
                break;
            case 9:
                CheckForNumberOfLevelNineStars();
                break;
            case 10:
                CheckForNumberOfLevelTenStars();
                break;
            case 11:
                CheckForNumberOfLevelElevenStars();
                break;
            case 12:
                CheckForNumberOfLevelTwelveStars();
                break;
            case 13:
                CheckForNumberOfLevelThirteenStars();
                break;
            case 14:
                CheckForNumberOfLevelFourteenStars();
                break;
            case 15:
                CheckForNumberOfLevelFifteenStars();
                break;
            case 16:
                CheckForNumberOfLevelSixteenStars();
                break;
            case 17:
                CheckForNumberOfLevelSeventeenStars();
                break;
            case 18:
                CheckForNumberOfLevelEighteenStars();
                break;
            case 19:
                CheckForNumberOfLevelNineteenStars();
                break;
            case 20:
                CheckForNumberOfLevelTwentyStars();
                break;
            case 21:
                CheckForNumberOfLevelTwentyOneStars();
                break;
            case 22:
                CheckForNumberOfLevelTwentyTwoStars();
                break;
            case 23:
                CheckForNumberOfLevelTwentyThreeStars();
                break;
            case 24:
                CheckForNumberOfLevelTwentyFourStars();
                break;
            case 25:
                CheckForNumberOfLevelTwentyFiveStars();
                break;
            case 26:
                CheckForNumberOfLevelTwentySixStars();
                break;
            case 27:
                CheckForNumberOfLevelSevenStars();
                break;
            case 28:
                CheckForNumberOfLevelTwentyEightStars();
                break;
            case 29:
                CheckForNumberOfLevelTwentyNineStars();
                break;
            case 30:
                CheckForNumberOfLevelThirtyStars();
                break;
        }



    }

    public void SetPlayerPrefs(int numbOfStars, string number)
    {
        
        PlayerPrefs.SetString("StartedLevel" + number, "true");
        if (!PlayerPrefs.HasKey("CompletedLevel" + number))
        {
           // PlayerPrefs.SetInt("NumberOfStars", PlayerPrefs.GetInt("NumberOfStars") + numbOfStars);
            PlayerPrefs.SetString("CompletedLevel" + number, "false");
            //PlayerPrefs.SetInt("Level" + number + "Stars", numbOfStars);
          
        }

        if (numbOfStars == 1 && PlayerPrefs.GetString("CompletedLevel" + number) == "false")
        {
            numbOfStars = numbOfStars - PlayerPrefs.GetInt("Level" + number + "Stars");
            PlayerPrefs.SetInt("Level" + number + "Stars", (PlayerPrefs.GetInt("Level" + number + "Stars") + numbOfStars));
            PlayerPrefs.SetInt("NumberOfStars", PlayerPrefs.GetInt("NumberOfStars") + numbOfStars);
        }
        else if (numbOfStars == 2 && PlayerPrefs.GetString("CompletedLevel" + number) == "false")
        {
            numbOfStars = numbOfStars - PlayerPrefs.GetInt("Level" + number + "Stars");
            PlayerPrefs.SetInt("Level" + number + "Stars", (PlayerPrefs.GetInt("Level" + number + "Stars") + numbOfStars));
            PlayerPrefs.SetInt("NumberOfStars", PlayerPrefs.GetInt("NumberOfStars") + numbOfStars);
        }
        else if (numbOfStars == 3 && PlayerPrefs.GetString("CompletedLevel" + number) == "false")
        {
            numbOfStars = numbOfStars - PlayerPrefs.GetInt("Level" + number + "Stars");
            PlayerPrefs.SetInt("Level" + number + "Stars", (PlayerPrefs.GetInt("Level" + number + "Stars") + numbOfStars));
            PlayerPrefs.SetInt("NumberOfStars", PlayerPrefs.GetInt("NumberOfStars") + numbOfStars);
            PlayerPrefs.SetString("CompletedLevel" + number, "true");
        }
        PlayerPrefs.Save();
    }
    public void ShowPausePanel()
    {
        pausePanel.SetActive(true);
        isGamePaused  = true;
    }

    public void HidePausePanel()
    {
        pausePanel.SetActive(false);
        isGamePaused = false;
    }

    public void RetryGame()
    {
        /* isGameStarted = true;
         loseScreenAnim.SetTrigger("hidelosescreen");
         loseTxtAnim.SetTrigger("hidelosetext");
         player.GetComponent<BallController>().ResetPlayerPosition();
         strokes = initialStrokes;
         strokesNumText.text = strokes.ToString();
         mainCamera.fieldOfView = 53f;
         cameraTurnTrigger.EnableCollider();*/
        int currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        if (currentHeart > 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {

            adBtn.gameObject.SetActive(true);
          
            errorMessage.SetActive(true);
            
        }
    }


    public void GoHome()
    {
        SceneManager.LoadScene(0);
    }

    public void NextLevel()
    {
        int numberOfStars = PlayerPrefs.GetInt("NumberOfStars");
        int currBuildIndex = SceneManager.GetActiveScene().buildIndex;

        if(numberOfStars >= nextLevelThreshold && testApp == false)
        {
            levelLoadingImg.SetActive(true);
            SceneManager.LoadScene(currBuildIndex + 1);
        }
        else
        {
            errorText.text = "YOU DO NOT HAVE ENOUGH STARS TO CONTINUE";
            errorMessage.SetActive(true);

        }
    }
    public void ButtonTap()
    { 
        audiosouce.clip = audioClips[3]; 
        audiosouce.Play();
    }

    public void HideStartPanel()
    {
        startPanelAnim.SetTrigger("hidestartpanel");
        if(!PlayerPrefs.HasKey("HasPlayedBefore"))
        {
            canShowTutorial = true;
            PlayerPrefs.SetInt("HasPlayedBefore", 1);
            PlayerPrefs.Save();
        }
        else
        {
            canShowTutorial = false;
        }
       
        isGameStarted = true;
        switch (SceneManager.GetActiveScene().buildIndex)
        { 
            case 11:
                timer = 120f;
                StartTimer();
                break;
            case 13:
                timer = 90f;
                StartTimer();
                break;
            case 15:
                timer = 120f;
                StartTimer();
                break;
            case 20:
                timer = 90f;
                StartTimer();
                break;
            case 23:
                timer = 100f;
                StartTimer();
                break;
            case 24:
                timer = 90f;
                StartTimer();
                break;
            case 25:
                timer = 60f;
                StartTimer();
                break;
            case 29:
                timer = 30f;
                StartTimer();
                break;
            case 30:
                timer = 30f;
                StartTimer();
                break;
        }

      
    }
    public void StartTimer()
    {
        isTimer = true;
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
            Time.timeScale = 0;
        }
        else
        {
            noInternetUI?.SetActive(false);
            Time.timeScale = 1;
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

    public void CheckForNumberOfLevelOneStars()
    {

        if (strokes > 1)
        {
            numberOfLvl1Stars++;
        }
        int numStars = numberOfLvl1Stars;
        Debug.Log(numStars);
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "One");
       


    }

    public void CheckForNumberOfLevelTwoStars()
    {
        if (strokes > 1)
        {
            numberOfLvl2Stars++;
        }
        if(strokes >= 5)
        {
            numberOfLvl2Stars++;
        }
        int numStars = numberOfLvl2Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Two");
       
       


    }

    public void CheckForNumberOfLevelThreeStars()
    { 
     

      if (strokes >= 2)
   {
    numberOfLvl3Stars++;
    }
    if(hitPitTrigger == false)
        {
            numberOfLvl3Stars++;
        }
    int numStars = numberOfLvl3Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Three");
       


    }
    public void CheckForNumberOfLevelFourStars()
    {


        if (outOfBounds == false)
        {
            numberOfLvl4Stars++;
        }
        if (hitmiddleWall == true)
        {
            numberOfLvl4Stars++;
        }
        int numStars = numberOfLvl4Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Four");
       

    }

    public void CheckForNumberOfLevelFiveStars()
    {


        if (outOfBounds == false)
        {
            numberOfLvl5Stars++;
        }
        if (strokes >= 5)
        {
            numberOfLvl5Stars++;
        }
        int numStars = numberOfLvl5Stars;
        DisplayStars(numStars);

        SetPlayerPrefs(numStars, "Five");


    }
    public void CheckForNumberOfLevelSixStars()
    {


        if (outOfBounds == false)
        {
            numberOfLvl6Stars++;
        }
        if (strokes >= 2)
        {
            numberOfLvl6Stars++;
        }
        int numStars = numberOfLvl6Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Six");
        
    }
    public void CheckForNumberOfLevelSevenStars()
    {


        if (strokes == 3)
        {
            numberOfLvl7Stars++;
        }
        if (strokes >= 1)
        {
            numberOfLvl7Stars++;
        }
        int numStars = numberOfLvl7Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Seven");

    }
    public void CheckForNumberOfLevelEightStars()
    {


        if (useBankShot)
        {
            numberOfLvl8Stars++;
        }
        if (hitSupports == false)
        {
            numberOfLvl8Stars++;
        }
        int numStars = numberOfLvl8Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Eight");

    }
    public void CheckForNumberOfLevelNineStars()
    {


        if (hitPitTrigger)
        {
            numberOfLvl9Stars++;
        }
        if (hitWood)
        {
            numberOfLvl9Stars++;
        }
        int numStars = numberOfLvl9Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Nine");

    }
    public void CheckForNumberOfLevelTenStars()
    {


        if (useRamp)
        {
            numberOfLvl10Stars++;
        }
        if (strokes >= 2)
        {
            numberOfLvl10Stars++;
        }
        int numStars = numberOfLvl10Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Ten");

    }
    public void CheckForNumberOfLevelElevenStars()
    {

        isTimer = false;
        if (hitSupports == false)
        {
            numberOfLvl11Stars++;
        }
        Debug.Log(timer);
        if (timer > 0)
        {
            numberOfLvl11Stars++;
        }
        int numStars = numberOfLvl11Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Eleven");

    }
    public void CheckForNumberOfLevelTwelveStars()
    {
    
        if (outOfBounds == false)
        {
            numberOfLvl12Stars++;
        }
        
        if (hitMushroom == false)
        {
            numberOfLvl12Stars++;
        }
        int numStars = numberOfLvl12Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Twelve");

    }
    public void CheckForNumberOfLevelThirteenStars()
    {
        isTimer = false;
        if (outOfBounds == false)
        {
            numberOfLvl13Stars++;
        }

        if (timer > 0)
        {
            numberOfLvl13Stars++;
        }
        int numStars = numberOfLvl13Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Thirteen");

    }
    public void CheckForNumberOfLevelFourteenStars()
    {
        
        if (outOfBounds == false)
        {
            numberOfLvl14Stars++;
        }

        if (secondBall)
        {
            numberOfLvl14Stars++;
        }
        int numStars = numberOfLvl14Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Fourteen");

    }
    public void CheckForNumberOfLevelFifteenStars()
    {

        if (strokes >= 5)
        {
            numberOfLvl15Stars++;
        }

        if (timer > 0)
        {
            numberOfLvl15Stars++;
        }
        int numStars = numberOfLvl15Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Fifteen");

    }
    public void CheckForNumberOfLevelSixteenStars()
    {



        if (hitWindMill)
        {
            numberOfLvl16Stars++;
        }
        int numStars = numberOfLvl16Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Sixteen");

    }
    public void CheckForNumberOfLevelSeventeenStars()
    {

        if(outOfBounds == false)
        {
            numberOfLvl17Stars++;
        }

        if (hitWood)
        {
            numberOfLvl17Stars++;
        }
        int numStars = numberOfLvl17Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Seventeen");

    }
    public void CheckForNumberOfLevelEighteenStars()
    {

        if (useBankShot)
        {
            numberOfLvl18Stars++;
        }

        if (strokes >= 10)
        {
            numberOfLvl18Stars++;
        }
        int numStars = numberOfLvl18Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Eighteen");

    }
    public void CheckForNumberOfLevelNineteenStars()
    {

        if (outOfBoundsInt <= 2)
        {
            numberOfLvl19Stars++;
        }

        if (strokes >= 5)
        {
            numberOfLvl19Stars++;
        }
        int numStars = numberOfLvl19Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Nineteen");

    }
    public void CheckForNumberOfLevelTwentyStars()
    {
        
        if (timer > 0)
        {
            numberOfLvl20Stars++;
        }

        if (hitPitTrigger)
        {
            numberOfLvl20Stars++;
        }
        int numStars = numberOfLvl20Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Twenty");

    }
    public void CheckForNumberOfLevelTwentyOneStars()
    {

        

        if (hitMovingWall == false)
        {
            numberOfLvl21Stars++;
        }
        int numStars = numberOfLvl21Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentyOne");

    }
    public void CheckForNumberOfLevelTwentyTwoStars()
    {

        if(outOfBounds == false)
        {
            numberOfLvl22Stars++;
        }

        if (hitBlackWall == false)
        {
            numberOfLvl22Stars++;
        }
        int numStars = numberOfLvl22Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentyTwo");

    }
    public void CheckForNumberOfLevelTwentyThreeStars()
    {

        if (strokes >= 10)
        {
            numberOfLvl23Stars++;
        }

        if (timer > 0)
        {
            numberOfLvl23Stars++;
        }
        int numStars = numberOfLvl23Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentyThree");

    }
    public void CheckForNumberOfLevelTwentyFourStars()
    {

        if (outOfBounds == false)
        {
            numberOfLvl24Stars++;
        }

        if (timer > 0)
        {
            numberOfLvl24Stars++;
        }
        int numStars = numberOfLvl24Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentyFour");

    }
    public void CheckForNumberOfLevelTwentyFiveStars()
    {

        


        if (timer > 0)
        {
            numberOfLvl25Stars++;
        }
        int numStars = numberOfLvl25Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentyFive");

    }
    public void CheckForNumberOfLevelTwentySixStars()
    {

        if(strokes >= 6)
        {
            numberOfLvl26Stars++;
        }


        if (outOfBounds == false)
        {
            numberOfLvl26Stars++;
        }
        int numStars = numberOfLvl26Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentySix");

    }

    public void CheckForNumberOfLevelTwentySevenStars()
    {

        if (strokes >= 9)
        {
            numberOfLvl27Stars++;
        }

        if (strokes >= 3)
        {
            numberOfLvl27Stars++;
        }

        int numStars = numberOfLvl27Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentySeven");

    }
    public void CheckForNumberOfLevelTwentyEightStars()
    {

        if (outOfBounds == false)
        {
            numberOfLvl28Stars++;
        }

        if (secondBall)
        {
            numberOfLvl28Stars++;
        }

        int numStars = numberOfLvl28Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentyEight");

    }
    public void CheckForNumberOfLevelTwentyNineStars()
    {

        if (timer > 0)
        {
            numberOfLvl29Stars++;
        }

        if (strokes >= 3)
        {
            numberOfLvl29Stars++;
        }

        int numStars = numberOfLvl29Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "TwentyNine");

    }
    public void CheckForNumberOfLevelThirtyStars()
    {

        if (timer > 0)
        {
            numberOfLvl30Stars++;
        }

        


        int numStars = numberOfLvl30Stars;
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, "Thirty");

    }
}




