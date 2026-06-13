using System.Collections;
using UnityEngine;

public class ButtonPopIn : MonoBehaviour
{
    public GameObject[] buttons;
    public float delayBetween = 0.15f;
    public float popDuration = 0.3f;

    private Vector3[] originalScales; // stores each button's original scale

    private void Awake()
    {
        // save the original scales ONCE when the game starts
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
        // hide all buttons first
        foreach (GameObject btn in buttons)
        {
            btn.transform.localScale = Vector3.zero;
        }

        // pop each one in using its original scale as the target
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
            btn.transform.localScale = targetScale * scale; // scale towards original
            yield return null;
        }

        btn.transform.localScale = targetScale; // snap to exact original at the end
    }

    private float OvershootCurve(float t)
    {
        float overshoot = 1.70158f;
        t -= 1;
        return t * t * ((overshoot + 1) * t + overshoot) + 1;
    }
}