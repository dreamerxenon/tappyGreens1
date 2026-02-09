using UnityEngine;

public class WetEffect : MonoBehaviour
{
    public Material mat;
    private  float wetness = 0.3f;

    void Update()
    {
        mat.SetFloat("_Smoothness", Mathf.Lerp(0.2f, 1f, wetness));
        mat.SetFloat("_Metallic", Mathf.Lerp(0f, 0.8f, wetness));
    }
}
