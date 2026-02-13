using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class VHSEffectWorking : MonoBehaviour
{
    [Header("VHS Settings")]
    [Range(0f, 1f)]
    public float noise = 0.5f;
    [Range(0f, 1f)]
    public float scanlines = 0.5f;
    [Range(0f, 0.1f)]
    public float distortion = 0.03f;

    [Header("Material")]
    public Material vhsMaterial;

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (vhsMaterial != null)
        {
            vhsMaterial.SetFloat("_Noise", noise);
            vhsMaterial.SetFloat("_Scanlines", scanlines);
            vhsMaterial.SetFloat("_Distortion", distortion);

            Graphics.Blit(src, dest, vhsMaterial);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}