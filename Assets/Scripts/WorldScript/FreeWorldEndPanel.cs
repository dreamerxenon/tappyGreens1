using UnityEngine;
using TMPro;
using System.Collections;

public class FreeWorldEndPanel : MonoBehaviour
{
    public static FreeWorldEndPanel Instance;

    [Header("UI")]
    public GameObject panelRoot;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI rankText;
    public Animator text1anim, text2anim;
    [Header("Animation")]
    public float scoreAnimDuration = 1.5f;
    public float rankAnimDuration = 1.0f;

    [Header("Audio Source")]
    [SerializeField] private AudioSource scoreTextAudio, rankTextAudio;


    public FreeWorldRunManager runManager;
    void Awake()
    {
        Instance = this;
        panelRoot.SetActive(false);
    }

    public void Show(LeaderboardEntryData result)
    {
        panelRoot.SetActive(true);
        text1anim.SetTrigger("come");
        text2anim.SetTrigger("come");
        scoreText.text = "0";
        rankText.text = "--";

        StopAllCoroutines();
        StartCoroutine(AnimateResult(result));
    }

    public void ShowOfflineResult(float time,int strokes)
    {
        panelRoot.SetActive(true);
        text1anim.SetTrigger("come");
        text2anim.SetTrigger("come");
        if (runManager.ChallengeType == FreeWorldChallengeType.FastestTime)
        {
            scoreText.text = time.ToString("0.00") + "s";
        }
        else
        {
            scoreText.text = strokes.ToString();
        }
    }
    IEnumerator AnimateResult(LeaderboardEntryData result)
    {
        if (runManager.ChallengeType == FreeWorldChallengeType.FastestTime)
        {
           
            float seconds = result.Score / 1000f;
           scoreText.text = seconds.ToString("0.00") + "s";
        }
        else
        {
          
            yield return AnimateNumber(scoreText, 0, result.Score, scoreAnimDuration);
        }
        
      
        yield return new WaitForSeconds(0.2f);
        yield return AnimateNumber(rankText, 0, result.Rank, rankAnimDuration, prefix: "#");
    }

    IEnumerator AnimateNumber(
        TextMeshProUGUI text,
        int from,
        int to,
        float duration,
        string prefix = ""
    )
    {
        float elapsed = 0f;
        scoreTextAudio.Play();
        rankTextAudio.Play();
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease out (feels premium)
            t = 1f - Mathf.Pow(1f - t, 3f);

            int value = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            text.text = prefix + value;

            yield return null;
        }

        text.text = prefix + to;
    }
}
