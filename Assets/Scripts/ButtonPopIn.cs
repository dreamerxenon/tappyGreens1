using System.Collections;
using UnityEngine;

public class ButtonPopIn : MonoBehaviour
{
    public GameObject[] buttons;
    public float delayBetween = 0.15f;
    public float popDuration = 0.3f;

    private Vector3[] originalScales;

    private void Awake()
    {
        originalScales = new Vector3[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            originalScales[i] = buttons[i].transform.localScale;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(PopButtonsIn());
    }

    private IEnumerator PopButtonsIn()
    {
        foreach (GameObject btn in buttons)
        {
            btn.transform.localScale = Vector3.zero;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            StartCoroutine(PopSingle(buttons[i], originalScales[i]));
            yield return new WaitForSeconds(delayBetween);
        }
    }

    private IEnumerator PopSingle(GameObject btn, Vector3 targetScale)
    {
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            float scale = OvershootCurve(t);
            btn.transform.localScale = targetScale * scale;
            yield return null;
        }

        btn.transform.localScale = targetScale;

        // Pass the correct original scale to ButtonAnimator now that pop is settled
        ButtonAnimator animator = btn.GetComponent<ButtonAnimator>();
        if (animator != null)
        {
            animator.SetOriginalScale(targetScale);
        }
    }

    private float OvershootCurve(float t)
    {
        float overshoot = 1.70158f;
        t -= 1;
        return t * t * ((overshoot + 1) * t + overshoot) + 1;
    }
}