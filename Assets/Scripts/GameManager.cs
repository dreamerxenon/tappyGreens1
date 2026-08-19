using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    // Game state
    public bool ballClicked, ballMoving, isGameStarted, isGameWon = false, isTimer = false, isGameLost = false, tapOnSlider;
    public bool isGamePaused, hitPitTrigger, hitmiddleWall, outOfBounds, useBankShot, hitSupports, hitWood, useRamp, hitMushroom, secondBall, hitWindMill, hitMovingWall;
    public bool hitBlackWall;
    public bool canShowTutorial, testApp;
    private bool isTimerPaused = false;

    // UI
    public TextMeshProUGUI strokesNumText, victoryTxt, timerTxt, errorText;
    public GameObject pausePanel, player, errorMessage, timerPanel;
    public GameObject[] stars;
    public GameObject noInternetUI, loadingIndicator, levelLoadingImg;
    public Slider effectsSlider, musicSlider;
    public Button retryButton, adBtn;

    // Audio
    public AudioSource audiosouce;
    public AudioClip[] audioClips;
    public AudioSource[] effectsAudioSource;
    public AudioSource[] musicAudioSource;

    // Animators
    public Animator holeInOneAnim, loseTxtAnim, startPanelAnim;
    public Animator winScreenAnim, loseScreenAnim, winTxtAnim;

    // Game data
    public int strokes;
    public float timer;
    public Vector3 startPosition, targetPosition, currentHolePosition;
    public int outOfBoundsInt;
    public int nextLevelThreshold;
    public float checkInterval = 10f;
    public bool isConnected;

    // Private
    private int holeInOne;
    private int initialStrokes;
    private CameraController cameraController;
    private CameraTurnTrigger cameraTurnTrigger;
    private Camera mainCamera;
    private Transform currentHole;
    private Vector3 previousPlayerPosition;
    private Button nextLvl;
    private string[] victoryTexts;

    // Per-level star counts
    private int[] levelStars = new int[51]; // index 1–50 maps to levels 1–50

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Application.targetFrameRate = 60;
        // QualitySettings.vSyncCount = 0;

        mainCamera = Camera.main;
        targetPosition = mainCamera.transform.position;

        GameObject nextLvlBtn = GameObject.FindWithTag("nextlevel");
        if (nextLvlBtn != null)
            nextLvl = nextLvlBtn.GetComponent<Button>();
        else
            Debug.LogError("Button with tag 'nextlevel' not found!");

        currentHole = GameObject.FindWithTag("Hole")?.transform;
        if (currentHole == null) Debug.LogError("Hole not found!");
    }

    void Start()
    {
        AdsManager.instance.HideBanner();

        if (nextLvl != null)
            nextLvl.onClick.AddListener(NextLevel);

        if (retryButton != null)
            retryButton.onClick.AddListener(RetryCheck);

        cameraTurnTrigger = FindFirstObjectByType<CameraTurnTrigger>();
        cameraController = FindFirstObjectByType<CameraController>();

        currentHolePosition = currentHole.position;
        previousPlayerPosition = player.transform.position;

        isGameStarted = false;

        int extraStrokes = CharacterManager.Instance.GetBufferStrokes();
        strokes += extraStrokes;
        initialStrokes = strokes;
        strokesNumText.text = strokes.ToString();
        holeInOne = strokes - 1;

        victoryTexts = new string[]
        {
            "NICE SHOT! YOU CRUSHED IT",
            "TIGER WOODS IS CALLING.. HE WANTS TIPS FROM YOU",
            "NICE, NEXT LEVEL?",
            "WELL PLAYED! A TRUE MINI GOLF CHAMPION"
        };

        InitAudioSliders();

        // Register slider listeners once here, not every frame
        if (effectsSlider != null)
            effectsSlider.onValueChanged.AddListener(ChangeEffectsVolume);
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(ChangeMusicVolume);

        AttachButtonAnimators();

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Time.timeScale = 0;
            noInternetUI.SetActive(true);
        }
        else
        {
            isConnected = true;
        }

        StartCoroutine(CheckInternetLoop());
    }

    void Update()
    {
        // Timer
        if (isTimer && isConnected && timer > 0 && !isTimerPaused)
        {
            timer -= Time.deltaTime * CharacterManager.Instance.GetTimerMultiplier();
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            timerTxt.text = "0" + minutes + ":" + seconds.ToString("D2");
        }

        // Heart refill countdown — only shown when game is lost and hearts are empty
        if (isGameLost)
        {
            int currentHeart = PlayerPrefs.GetInt("CurrentHeart");
            if (currentHeart < 1)
            {
                System.TimeSpan t = HeartSystem.Instance.GetNextHeartTime();
                errorText.text = "YOU DON'T HAVE ENOUGH HEARTS\nREFILL: " + string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
            }
        }
    }

    private void LateUpdate() { }

    // ─── Audio ───────────────────────────────────────────────────────────────

    private void InitAudioSliders()
    {
        effectsSlider.minValue = musicSlider.minValue = 0f;
        effectsSlider.maxValue = musicSlider.maxValue = 1f;

        float musicVol = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : musicAudioSource[0].volume;
        float effectsVol = PlayerPrefs.HasKey("effectsVolume") ? PlayerPrefs.GetFloat("effectsVolume") : effectsAudioSource[0].volume;

        for (int i = 0; i < musicAudioSource.Length; i++)
            musicAudioSource[i].volume = musicVol;
        musicSlider.value = musicVol;

        for (int i = 0; i < effectsAudioSource.Length; i++)
            effectsAudioSource[i].volume = effectsVol;
        effectsSlider.value = effectsVol;
    }

    public void ChangeEffectsVolume(float volume)
    {
        PlayerPrefs.SetFloat("effectsVolume", volume);
        for (int i = 0; i < effectsAudioSource.Length; i++)
            if (effectsAudioSource[i] != null)
                effectsAudioSource[i].volume = volume;
    }

    public void ChangeMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
        for (int i = 0; i < musicAudioSource.Length; i++)
            if (musicAudioSource[i] != null)
                musicAudioSource[i].volume = volume;
    }

    public void ButtonTap()
    {
        audiosouce.clip = audioClips[3];
        audiosouce.Play();
    }

    // ─── Ads & Hearts ────────────────────────────────────────────────────────

    private void OnEnable() => AdsManager.OnRewardedAdCompleted += HandleRewardedAdCompleted;
    private void OnDisable() => AdsManager.OnRewardedAdCompleted -= HandleRewardedAdCompleted;

    public void ShowRewardedAd() => AdsManager.instance.ShowRewarded();

    private void HandleRewardedAdCompleted()
    {
        AddHeart();
        AdsManager.instance.RequestRewarded();
    }

    private void AddHeart()
    {
        int currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        if (currentHeart <= 5)
            PlayerPrefs.SetInt("CurrentHeart", currentHeart + 1);
    }

    // ─── Timer ────────────────────────────────────────────────────────────────

    public void StartTimer() => isTimer = true;

    public void PauseTimerForFiveSeconds() => StartCoroutine(PauseTimerCoroutine());

    private IEnumerator PauseTimerCoroutine()
    {
        isTimerPaused = true;
        yield return new WaitForSeconds(5f);
        isTimerPaused = false;
    }

    // ─── Camera ───────────────────────────────────────────────────────────────

    public void ApplySliderInstructions()
    {
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, 0.1f);
    }

    // ─── Stars ────────────────────────────────────────────────────────────────

    public void DisplayStars(int numberOfStars)
    {
        if (numberOfStars >= 1) stars[0].SetActive(true);
        if (numberOfStars >= 2) stars[2].SetActive(true);
        if (numberOfStars >= 3) stars[1].SetActive(true);
    }

    // ─── Stroke & Game State ─────────────────────────────────────────────────

    public void MakeStroke()
    {
        strokes--;
        strokesNumText.text = strokes.ToString();
        previousPlayerPosition = player.transform.position;

        if (strokes == 0)
        {
            isGameStarted = false;
            Invoke("GameOver", 4f);
        }
    }

    public void RedoLastShot()
    {
        player.transform.position = Vector3.Lerp(player.transform.position, previousPlayerPosition, Time.deltaTime);
    }

    public void GameOver()
    {
        HeartSystem.Instance.UseHeart();
        int currentHeart = PlayerPrefs.GetInt("CurrentHeart");

        if (PlayerPrefs.GetString("CanShowAd") == "true")
            AdsManager.instance.ShowInterstitialforLevels();

        if (!isGameWon)
        {
            audiosouce.clip = audioClips[2];
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
        if (PlayerPrefs.GetString("CanShowAd") == "true")
            AdsManager.instance.ShowInterstitialforLevels();

        if (strokes == holeInOne && !isGameLost)
        {
            audiosouce.clip = audioClips[0];
            winScreenAnim.SetTrigger("showwinscreen");
            holeInOneAnim.SetTrigger("showholeinone");
            audiosouce.Play();
            cameraController.ShowBall();
            isGameWon = true;
            isGameStarted = false;
        }
        else if (strokes != holeInOne && !isGameLost)
        {
            audiosouce.clip = audioClips[1];
            victoryTxt.text = victoryTexts[Random.Range(0, victoryTexts.Length)];
            isGameWon = true;
            winScreenAnim.SetTrigger("showwinscreen");
            winTxtAnim.SetTrigger("showwintxt");
            audiosouce.Play();
            cameraController.ShowBall();
            isGameStarted = false;
        }

        // Add base completion star for this level only, then check for bonus stars
        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        if (buildIndex >= 1 && buildIndex <= 50)
            levelStars[buildIndex]++;

        switch (buildIndex)
        {
            case 1: CheckForNumberOfLevelOneStars(); break;
            case 2: CheckForNumberOfLevelTwoStars(); break;
            case 3: CheckForNumberOfLevelThreeStars(); break;
            case 4: CheckForNumberOfLevelFourStars(); break;
            case 5: CheckForNumberOfLevelFiveStars(); break;
            case 6: CheckForNumberOfLevelSixStars(); break;
            case 7: CheckForNumberOfLevelSevenStars(); break;
            case 8: CheckForNumberOfLevelEightStars(); break;
            case 9: CheckForNumberOfLevelNineStars(); break;
            case 10: CheckForNumberOfLevelTenStars(); break;
            case 11: CheckForNumberOfLevelElevenStars(); break;
            case 12: CheckForNumberOfLevelTwelveStars(); break;
            case 13: CheckForNumberOfLevelThirteenStars(); break;
            case 14: CheckForNumberOfLevelFourteenStars(); break;
            case 15: CheckForNumberOfLevelFifteenStars(); break;
            case 16: CheckForNumberOfLevelSixteenStars(); break;
            case 17: CheckForNumberOfLevelSeventeenStars(); break;
            case 18: CheckForNumberOfLevelEighteenStars(); break;
            case 19: CheckForNumberOfLevelNineteenStars(); break;
            case 20: CheckForNumberOfLevelTwentyStars(); break;
            case 21: CheckForNumberOfLevelTwentyOneStars(); break;
            case 22: CheckForNumberOfLevelTwentyTwoStars(); break;
            case 23: CheckForNumberOfLevelTwentyThreeStars(); break;
            case 24: CheckForNumberOfLevelTwentyFourStars(); break;
            case 25: CheckForNumberOfLevelTwentyFiveStars(); break;
            case 26: CheckForNumberOfLevelTwentySixStars(); break;
            case 27: CheckForNumberOfLevelTwentySevenStars(); break;
            case 28: CheckForNumberOfLevelTwentyEightStars(); break;
            case 29: CheckForNumberOfLevelTwentyNineStars(); break;
            case 30: CheckForNumberOfLevelThirtyStars(); break;
        }
    }

    // ─── PlayerPrefs Star Saving ─────────────────────────────────────────────

    public void SetPlayerPrefs(int numbOfStars, string number)
    {
        PlayerPrefs.SetString("StartedLevel" + number, "true");

        // Was this the level's first-ever completion? Check this BEFORE any
        // network call - the money reward must never gate star/progression saving.
        bool firstCompletion = !PlayerPrefs.HasKey("CompletedLevel" + number);
        if (firstCompletion)
            PlayerPrefs.SetString("CompletedLevel" + number, "false");

        if (numbOfStars >= 1 && PlayerPrefs.GetString("CompletedLevel" + number) == "false")
        {
            int existing = PlayerPrefs.GetInt("Level" + number + "Stars");
            int diff = numbOfStars - existing;

            if (diff > 0)
            {
                PlayerPrefs.SetInt("Level" + number + "Stars", existing + diff);
                PlayerPrefs.SetInt("NumberOfStars", PlayerPrefs.GetInt("NumberOfStars") + diff);
            }

            if (numbOfStars >= 3)
                PlayerPrefs.SetString("CompletedLevel" + number, "true");
        }

        // Stars/completion are written to disk immediately - progression can
        // never be blocked or delayed by a slow/failed network call anymore.
        PlayerPrefs.Save();

        // Money reward runs independently, fire-and-forget. If the network is
        // slow or the call fails on Android, it no longer delays star saving
        // or blocks NextLevel() from seeing the correct star count.
        if (firstCompletion)
            GrantFirstCompletionReward();
    }

    private async void GrantFirstCompletionReward()
    {
        await CurrencyManager.Instance.AddMoney(5);
    }

    // ─── UI Controls ─────────────────────────────────────────────────────────

    public void ShowPausePanel()
    {
        pausePanel.SetActive(true);
        isGamePaused = true;
    }

    public void HidePausePanel()
    {
        pausePanel.SetActive(false);
        isGamePaused = false;
    }

    public void RetryGame()
    {
        int currentHeart = PlayerPrefs.GetInt("CurrentHeart");
        if (currentHeart > 0)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        else
        {
            adBtn.gameObject.SetActive(true);
            errorMessage.SetActive(true);
        }
    }

    public void GoHome() => SceneManager.LoadScene(0);

    public void NextLevel()
    {
        int numberOfStars = PlayerPrefs.GetInt("NumberOfStars");
        int currBuildIndex = SceneManager.GetActiveScene().buildIndex;

        if (numberOfStars >= nextLevelThreshold && !testApp)
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

    public void HideStartPanel()
    {
        startPanelAnim.SetTrigger("hidestartpanel");

        if (!PlayerPrefs.HasKey("HasPlayedBefore"))
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

        int buildIndex = SceneManager.GetActiveScene().buildIndex;
        timerPanel.SetActive(false);

        switch (buildIndex)
        {
            case 11: timer = 120f; timerPanel.SetActive(true); StartTimer(); break;
            case 13: timer = 90f; timerPanel.SetActive(true); StartTimer(); break;
            case 15: timer = 120f; timerPanel.SetActive(true); StartTimer(); break;
            case 20: timer = 90f; timerPanel.SetActive(true); StartTimer(); break;
            case 23: timer = 100f; timerPanel.SetActive(true); StartTimer(); break;
            case 24: timer = 90f; timerPanel.SetActive(true); StartTimer(); break;
            case 25: timer = 60f; timerPanel.SetActive(true); StartTimer(); break;
            case 29: timer = 30f; timerPanel.SetActive(true); StartTimer(); break;
            case 30: timer = 30f; timerPanel.SetActive(true); StartTimer(); break;
        }
    }

    // ─── Button Animators ────────────────────────────────────────────────────

    private void AttachButtonAnimators()
    {
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button btn in allButtons)
        {
            ButtonAnimator animator = btn.GetComponent<ButtonAnimator>();
            if (animator == null)
                animator = btn.gameObject.AddComponent<ButtonAnimator>();
            animator.Initialize();
        }
    }

    // ─── Internet ────────────────────────────────────────────────────────────

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
        noInternetUI?.SetActive(!connected);
        Time.timeScale = connected ? 1 : 0;
    }

    public void RetryCheck()
    {
        if (loadingIndicator != null) loadingIndicator.SetActive(true);
        StartCoroutine(CheckInternet());
    }

    // ─── Level Star Checks ───────────────────────────────────────────────────

    // Helper to get & display final star count for a level
    private void FinalizeStars(int levelIndex, string levelName)
    {
        int numStars = levelStars[levelIndex];
        DisplayStars(numStars);
        SetPlayerPrefs(numStars, levelName);
    }

    public void CheckForNumberOfLevelOneStars()
    {
        if (strokes > 1) levelStars[1]++;
        if (strokes == 3) levelStars[1]++;
        FinalizeStars(1, "One");
    }

    public void CheckForNumberOfLevelTwoStars()
    {
        if (strokes > 1) levelStars[2]++;
        if (strokes >= 5) levelStars[2]++;
        FinalizeStars(2, "Two");
    }

    public void CheckForNumberOfLevelThreeStars()
    {
        if (strokes >= 2) levelStars[3]++;
        if (!hitPitTrigger) levelStars[3]++;
        FinalizeStars(3, "Three");
    }

    public void CheckForNumberOfLevelFourStars()
    {
        if (!outOfBounds) levelStars[4]++;
        if (hitmiddleWall) levelStars[4]++;
        FinalizeStars(4, "Four");
    }

    public void CheckForNumberOfLevelFiveStars()
    {
        if (!outOfBounds) levelStars[5]++;
        if (strokes >= 5) levelStars[5]++;
        FinalizeStars(5, "Five");
    }

    public void CheckForNumberOfLevelSixStars()
    {
        if (!outOfBounds) levelStars[6]++;
        if (strokes >= 2) levelStars[6]++;
        FinalizeStars(6, "Six");
    }

    public void CheckForNumberOfLevelSevenStars()
    {
        if (strokes == 3) levelStars[7]++;
        if (strokes >= 1) levelStars[7]++;
        FinalizeStars(7, "Seven");
    }

    public void CheckForNumberOfLevelEightStars()
    {
        if (useBankShot) levelStars[8]++;
        if (!hitSupports) levelStars[8]++;
        FinalizeStars(8, "Eight");
    }

    public void CheckForNumberOfLevelNineStars()
    {
        if (!hitPitTrigger) levelStars[9]++;
        if (!hitWood) levelStars[9]++;
        FinalizeStars(9, "Nine");
    }

    public void CheckForNumberOfLevelTenStars()
    {
        if (useRamp) levelStars[10]++;
        if (strokes >= 2) levelStars[10]++;
        FinalizeStars(10, "Ten");
    }

    public void CheckForNumberOfLevelElevenStars()
    {
        isTimer = false;
        if (!hitSupports) levelStars[11]++;
        if (timer > 0) levelStars[11]++;
        FinalizeStars(11, "Eleven");
    }

    public void CheckForNumberOfLevelTwelveStars()
    {
        if (!outOfBounds) levelStars[12]++;
        if (!hitMushroom) levelStars[12]++;
        FinalizeStars(12, "Twelve");
    }

    public void CheckForNumberOfLevelThirteenStars()
    {
        isTimer = false;
        if (!outOfBounds) levelStars[13]++;
        if (timer > 0) levelStars[13]++;
        FinalizeStars(13, "Thirteen");
    }

    public void CheckForNumberOfLevelFourteenStars()
    {
        if (!outOfBounds) levelStars[14]++;
        if (secondBall) levelStars[14]++;
        FinalizeStars(14, "Fourteen");
    }

    public void CheckForNumberOfLevelFifteenStars()
    {
        isTimer = false;
        if (strokes >= 5) levelStars[15]++;
        if (timer > 0) levelStars[15]++;
        FinalizeStars(15, "Fifteen");
    }

    public void CheckForNumberOfLevelSixteenStars()
    {
        if (!hitWindMill) levelStars[16]++;
        if (strokes == 3) levelStars[16]++;
        FinalizeStars(16, "Sixteen");
    }

    public void CheckForNumberOfLevelSeventeenStars()
    {
        if (!outOfBounds) levelStars[17]++;
        if (!hitWood) levelStars[17]++;
        FinalizeStars(17, "Seventeen");
    }

    public void CheckForNumberOfLevelEighteenStars()
    {
        if (useBankShot) levelStars[18]++;
        if (strokes >= 10) levelStars[18]++;
        FinalizeStars(18, "Eighteen");
    }

    public void CheckForNumberOfLevelNineteenStars()
    {
        if (outOfBoundsInt <= 2) levelStars[19]++;
        if (strokes >= 5) levelStars[19]++;
        FinalizeStars(19, "Nineteen");
    }

    public void CheckForNumberOfLevelTwentyStars()
    {
        isTimer = false;
        if (timer > 0) levelStars[20]++;
        if (hitPitTrigger) levelStars[20]++;
        FinalizeStars(20, "Twenty");
    }

    public void CheckForNumberOfLevelTwentyOneStars()
    {
        if (!hitMovingWall) levelStars[21]++;
        if (strokes == 3) levelStars[21]++;
        FinalizeStars(21, "TwentyOne");
    }

    public void CheckForNumberOfLevelTwentyTwoStars()
    {
        if (!outOfBounds) levelStars[22]++;
        if (!hitBlackWall) levelStars[22]++;
        FinalizeStars(22, "TwentyTwo");
    }

    public void CheckForNumberOfLevelTwentyThreeStars()
    {
        isTimer = false;
        if (strokes >= 10) levelStars[23]++;
        if (timer > 0) levelStars[23]++;
        FinalizeStars(23, "TwentyThree");
    }

    public void CheckForNumberOfLevelTwentyFourStars()
    {
        isTimer = false;
        if (!outOfBounds) levelStars[24]++;
        if (timer > 0) levelStars[24]++;
        FinalizeStars(24, "TwentyFour");
    }

    public void CheckForNumberOfLevelTwentyFiveStars()
    {
        isTimer = false;
        if (timer > 0) levelStars[25]++;
        if (strokes == 3) levelStars[25]++;
        FinalizeStars(25, "TwentyFive");
    }

    public void CheckForNumberOfLevelTwentySixStars()
    {
        if (strokes >= 6) levelStars[26]++;
        if (!outOfBounds) levelStars[26]++;
        FinalizeStars(26, "TwentySix");
    }

    public void CheckForNumberOfLevelTwentySevenStars()
    {
        if (strokes >= 9) levelStars[27]++;
        if (strokes >= 3) levelStars[27]++;
        FinalizeStars(27, "TwentySeven");
    }

    public void CheckForNumberOfLevelTwentyEightStars()
    {
        if (!outOfBounds) levelStars[28]++;
        if (secondBall) levelStars[28]++;
        FinalizeStars(28, "TwentyEight");
    }

    public void CheckForNumberOfLevelTwentyNineStars()
    {
        isTimer = false;
        if (timer > 0) levelStars[29]++;
        if (strokes >= 3) levelStars[29]++;
        FinalizeStars(29, "TwentyNine");
    }

    public void CheckForNumberOfLevelThirtyStars()
    {
        isTimer = false;
        if (timer > 0) levelStars[30]++;
        if (strokes == 3) levelStars[30]++;
        FinalizeStars(30, "Thirty");
    }
}