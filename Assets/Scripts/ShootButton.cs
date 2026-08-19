using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShootButton : MonoBehaviour
{
    [Header("Player Lookup")]
    public string playerTag = "Golf";
    private BallController ballController;
    private WorldBallController worldBallController;

    [Header("Click Animation")]
    public float punchScale = 1.15f;
    public float punchDuration = 0.12f;

    private Button button;
    private Vector3 originalScale;
    private Coroutine punchRoutine;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            ballController = playerObj.GetComponent<BallController>();
            worldBallController = playerObj.GetComponent<WorldBallController>();

            if (ballController == null && worldBallController == null)
                Debug.LogWarning($"ShootButton: GameObject with tag '{playerTag}' has neither BallController nor WorldBallController.");
        }
        else
        {
            Debug.LogWarning($"ShootButton: no GameObject found with tag '{playerTag}'.");
        }

        if (button != null)
            button.onClick.AddListener(OnShootButtonClicked);
    }

    private void OnShootButtonClicked()
    {
        PlayClickAnimation();

        if (GameManager.Instance != null)
            GameManager.Instance.ButtonTap();

        if (ballController != null)
            ballController.ConfirmShot();
        else if (worldBallController != null)
            worldBallController.ConfirmShot();
        else
            Debug.LogWarning("ShootButton: no ball controller reference found, cannot confirm shot.");
    }

    private void PlayClickAnimation()
    {
        if (punchRoutine != null)
            StopCoroutine(punchRoutine);

        punchRoutine = StartCoroutine(PunchScaleRoutine());
    }

    private IEnumerator PunchScaleRoutine()
    {
        float halfDuration = punchDuration * 0.5f;
        Vector3 targetScale = originalScale * punchScale;

        float t = 0f;
        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t / halfDuration);
            yield return null;
        }

        t = 0f;
        while (t < halfDuration)
        {
            t += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t / halfDuration);
            yield return null;
        }

        transform.localScale = originalScale;
        punchRoutine = null;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnShootButtonClicked);
    }
}