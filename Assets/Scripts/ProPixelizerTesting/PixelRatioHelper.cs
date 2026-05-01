using ProPixelizer;
using UnityEngine;

public class PixelRatioHelper : MonoBehaviour
{
    public enum CalculationMode
    {
        GetOrthoSizeForRatio,
        GetWorldSpacePixelSizeForRatio
    }

    [Tooltip("How many screen pixels should equal one low-res pixel. Use whole numbers like 1, 2, 3, 4.")]
    public float Ratio = 2f;

    [Tooltip("GetOrthoSizeForRatio: tells you what to set Orthographic Size to.\nGetWorldSpacePixelSizeForRatio: tells you what to set World Space Pixel Size to.")]
    public CalculationMode Mode = CalculationMode.GetOrthoSizeForRatio;

    private ProPixelizerCamera _proPixelizerCamera;

    private void Awake()
    {
        _proPixelizerCamera = GetComponent<ProPixelizerCamera>();
    }

    private void Update()
    {
        if (_proPixelizerCamera == null)
        {
            Debug.LogWarning("PixelRatioHelper: No ProPixelizerCamera found on this GameObject.");
            return;
        }

        if (Mode == CalculationMode.GetOrthoSizeForRatio)
        {
            float orthoSize = _proPixelizerCamera.GetOrthoSizeForScreenToTargetRatio(Ratio);
            Debug.Log($"[PixelRatioHelper] For a ratio of {Ratio}, set Orthographic Size to: {orthoSize}");
        }
        else
        {
            float pixelSize = _proPixelizerCamera.GetWorldSpacePixelSizeForScreenToTargetRatio(Ratio);
            Debug.Log($"[PixelRatioHelper] For a ratio of {Ratio}, set World Space Pixel Size to: {pixelSize}");
        }
    }
}