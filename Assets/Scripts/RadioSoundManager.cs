using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manages ambient "radio style" music playback for two tracks.
/// - Shuffles between the two clips (no immediate repeats)
/// - Leaves a silent gap between plays so music isn't constant
/// - Fakes a radio tune-in effect using a low-pass filter sweep + optional static burst
/// - Locks music volume to HALF of the player's MusicVolume setting, and stays in sync
///   even if the volume slider is dragged mid-song.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RadioSoundManager : MonoBehaviour
{
    public static RadioSoundManager Instance { get; private set; }

    [Header("Tracks")]
    [SerializeField] private AudioClip trackA;
    [SerializeField] private AudioClip trackB;

    [Header("Timing")]
    [Tooltip("Minimum silent gap (seconds) between songs.")]
    [SerializeField] private float minGap = 20f;
    [Tooltip("Maximum silent gap (seconds) between songs.")]
    [SerializeField] private float maxGap = 50f;

    [Header("Volume")]
    [Tooltip("Music is played at this fraction of PlayerPrefs 'MusicVolume', so it never overtakes SFX.")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolumeMultiplier = 0.5f;
    [Tooltip("How often (seconds) to re-check PlayerPrefs in case something changed it without calling SetMusicVolume.")]
    [SerializeField] private float volumePollInterval = 0.25f;

    [Header("Radio Effect")]
    [SerializeField] private AudioClip radioStaticBurst; // optional, short crackle/static sfx
    [Range(0f, 1f)]
    [SerializeField] private float staticVolume = 0.4f;
    [Tooltip("How long the 'tuning in' filter sweep + fade-in takes.")]
    [SerializeField] private float tuneInDuration = 1.2f;
    [Tooltip("How long the fade-out at the end of a track takes.")]
    [SerializeField] private float fadeOutDuration = 1.0f;
    [Tooltip("Low-pass cutoff while 'untuned' (muffled radio start).")]
    [SerializeField] private float mufflerCutoff = 800f;
    [Tooltip("Low-pass cutoff once fully tuned in (normal clarity). 22000 = effectively off.")]
    [SerializeField] private float clearCutoff = 22000f;

    private AudioSource musicSource;
    private AudioSource staticSource;
    private AudioLowPassFilter lowPassFilter;

    private float cachedMusicVolume01 = 1f; // raw slider value (0-1) from PlayerPrefs
    private AudioClip lastPlayed;
    private Coroutine radioLoop;

    private const string MUSIC_VOLUME_KEY = "MusicVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Main music source
        musicSource = GetComponent<AudioSource>();
        musicSource.loop = false;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D

        // Low-pass filter for the "tuning in" muffled effect
        lowPassFilter = GetComponent<AudioLowPassFilter>();
        if (lowPassFilter == null)
            lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
        lowPassFilter.cutoffFrequency = clearCutoff;

        // Separate source for the static burst so it can overlap the fade-in
        GameObject staticObj = new GameObject("RadioStaticSource");
        staticObj.transform.SetParent(transform);
        staticSource = staticObj.AddComponent<AudioSource>();
        staticSource.loop = false;
        staticSource.playOnAwake = false;
        staticSource.spatialBlend = 0f;
    }

    private void Start()
    {
        RefreshVolumeFromPlayerPrefs();

        // Randomize which track "lastPlayed" pretends to be, so PickNextClip()
        // doesn't always default to trackA on the very first pick.
        if (trackA != null && trackB != null)
            lastPlayed = (Random.value < 0.5f) ? trackA : trackB;

        radioLoop = StartCoroutine(RadioLoop());
        StartCoroutine(VolumePollLoop());
    }

    /// <summary>
    /// Call this directly from your volume slider's OnValueChanged for instant response.
    /// (PlayerPrefs.SetFloat should still be called by whoever owns the slider.)
    /// </summary>
    public void SetMusicVolume(float sliderValue01)
    {
        cachedMusicVolume01 = Mathf.Clamp01(sliderValue01);
        ApplyVolumeToSources();
    }

    private void RefreshVolumeFromPlayerPrefs()
    {
        cachedMusicVolume01 = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
        ApplyVolumeToSources();
    }

    private void ApplyVolumeToSources()
    {
        float target = cachedMusicVolume01 * musicVolumeMultiplier;
        musicSource.volume = target;
        staticSource.volume = target * staticVolume / Mathf.Max(musicVolumeMultiplier, 0.0001f);
        // ^ static burst scales relative to overall music volume too, not a fixed value
    }

    /// <summary>
    /// Safety net: in case something else calls PlayerPrefs.SetFloat directly
    /// (e.g. a slider bound only to PlayerPrefs with no direct reference to this manager),
    /// this catches the change within volumePollInterval seconds.
    /// </summary>
    private IEnumerator VolumePollLoop()
    {
        var wait = new WaitForSeconds(volumePollInterval);
        while (true)
        {
            float current = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
            if (!Mathf.Approximately(current, cachedMusicVolume01))
            {
                cachedMusicVolume01 = current;
                ApplyVolumeToSources();
            }
            yield return wait;
        }
    }

    private IEnumerator RadioLoop()
    {
        while (true)
        {
            // Silent gap before the next "broadcast"
            float gap = Random.Range(minGap, maxGap);
            yield return new WaitForSeconds(gap);

            AudioClip next = PickNextClip();
            if (next == null)
                continue;

            yield return PlayWithRadioEffect(next);
        }
    }

    private AudioClip PickNextClip()
    {
        if (trackA == null && trackB == null) return null;
        if (trackA == null) return trackB;
        if (trackB == null) return trackA;

        // Avoid playing the same track twice in a row
        return (lastPlayed == trackA) ? trackB : trackA;
    }

    private IEnumerator PlayWithRadioEffect(AudioClip clip)
    {
        lastPlayed = clip;

        // 1. Optional static burst as the "station" comes in
        if (radioStaticBurst != null)
            staticSource.PlayOneShot(radioStaticBurst, staticVolume);

        // 2. Start the track muffled and silent, then sweep filter + fade in ("tuning in")
        musicSource.clip = clip;
        musicSource.time = 0f;
        lowPassFilter.cutoffFrequency = mufflerCutoff;
        musicSource.volume = 0f;
        musicSource.Play();

        float targetVolume = cachedMusicVolume01 * musicVolumeMultiplier;
        float t = 0f;
        while (t < tuneInDuration)
        {
            t += Time.deltaTime;
            float lerp = t / tuneInDuration;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, lerp);
            lowPassFilter.cutoffFrequency = Mathf.Lerp(mufflerCutoff, clearCutoff, lerp);
            yield return null;
        }
        musicSource.volume = targetVolume;
        lowPassFilter.cutoffFrequency = clearCutoff;

        // 3. Let it play until fade-out should start
        float playDuration = clip.length - fadeOutDuration;
        if (playDuration > 0f)
            yield return new WaitForSeconds(playDuration);

        // 4. Fade out ("losing signal")
        t = 0f;
        float startVolume = musicSource.volume;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            float lerp = t / fadeOutDuration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, lerp);
            lowPassFilter.cutoffFrequency = Mathf.Lerp(clearCutoff, mufflerCutoff, lerp);
            yield return null;
        }

        musicSource.Stop();
        lowPassFilter.cutoffFrequency = clearCutoff;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}