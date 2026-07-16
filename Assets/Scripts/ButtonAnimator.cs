using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class ButtonAnimator : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private float punchScale = 0.85f;
    [SerializeField] private float duration = 0.08f;

    private Vector3 originalScale;
    private Coroutine currentAnim;
    private bool isInitialized = false;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (transform.localScale != Vector3.zero)
        {
            originalScale = transform.localScale;
            isInitialized = true;
        }
    }

    public void SetOriginalScale(Vector3 scale)
    {
        originalScale = scale;
        isInitialized = true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (!isInitialized) return;

        if (currentAnim != null)
        {
            StopCoroutine(currentAnim);
            transform.localScale = originalScale;
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        currentAnim = StartCoroutine(Punch());
    }

    private IEnumerator Punch()
    {
        Vector3 punched = originalScale * punchScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (transform == null) yield break;
            transform.localScale = Vector3.Lerp(originalScale, punched, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < duration)
        {
            if (transform == null) yield break;
            transform.localScale = Vector3.Lerp(punched, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
        currentAnim = null;
    }
}