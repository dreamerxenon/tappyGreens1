using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HeartUi : MonoBehaviour
{
    public TextMeshProUGUI fuelText;
    public TextMeshProUGUI timerText;
    public GameObject timerPanel, alarmButton;

    private HomeGameManager _homeGameManager;
    private bool _isHomeScene;
    private int _lastCurrentHeart = -1;
    private int _lastMaxHeart = -1;
    private float _timerUpdateInterval = 0.5f;
    private float _timerUpdateTimer;
    private enum UiState { None, Counting, Empty, Full }
    private UiState _lastState = UiState.None;

    void Start()
    {
        _isHomeScene = SceneManager.GetActiveScene().buildIndex == 0;
        if (_isHomeScene)
            _homeGameManager = FindFirstObjectByType<HomeGameManager>();
    }

    void Update()
    {
        if (HeartSystem.Instance == null) return;

        int currentHeart = HeartSystem.Instance.currentHeart;
        int maxHeart = HeartSystem.Instance.maxHeart;

        // Only touch fuel text when the value actually changes
        if (currentHeart != _lastCurrentHeart || maxHeart != _lastMaxHeart)
        {
            fuelText.text = $" {currentHeart}/{maxHeart}";
            _lastCurrentHeart = currentHeart;
            _lastMaxHeart = maxHeart;
        }

        UiState state = currentHeart == 0 ? UiState.Empty
                       : currentHeart < maxHeart && _isHomeScene ? UiState.Counting
                       : currentHeart < maxHeart ? UiState.Counting
                       : UiState.Full;

        bool needsCountdown = state == UiState.Counting || state == UiState.Empty;

        // Only flip panel/button active state on actual state transitions
        if (state != _lastState)
        {
            switch (state)
            {
                case UiState.Counting:
                    timerPanel.SetActive(true);
                    timerText.gameObject.SetActive(true);
                    alarmButton.SetActive(_isHomeScene);
                    break;
                case UiState.Empty:
                    timerPanel.SetActive(true);
                    timerText.gameObject.SetActive(true);
                    break;
                case UiState.Full:
                    timerPanel.SetActive(false);
                    alarmButton.SetActive(false);
                    timerText.gameObject.SetActive(false);
                    timerText.text = "Full";
                    if (_isHomeScene && _homeGameManager != null)
                        _homeGameManager.RemoveShowAdsPanel();
                    break;
            }
            _lastState = state;
        }

        // Countdown text updates at most twice a second, not every frame
        if (needsCountdown)
        {
            _timerUpdateTimer -= Time.deltaTime;
            if (_timerUpdateTimer <= 0f)
            {
                _timerUpdateTimer = _timerUpdateInterval;
                TimeSpan t = HeartSystem.Instance.GetNextHeartTime();
                timerText.text = $"{t.Minutes:D2}:{t.Seconds:D2}";
            }
        }
    }
}