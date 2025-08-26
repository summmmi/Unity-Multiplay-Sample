using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessController : MonoBehaviour
{
    [Header("Post Processing")]
    public Volume globalVolume;
    
    // Post Processing Components
    private Bloom bloom;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    
    // Original Values
    private float originalBloomIntensity = 0.0f;
    
    void Start()
    {
        InitializePostProcessing();
        StoreOriginalValues();
    }
    
    void InitializePostProcessing()
    {
        // Global Volume 찾기
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
        }

        if (globalVolume == null)
        {
            Debug.LogError("Global Volume not found!");
            return;
        }

        // Bloom 컴포넌트
        if (!globalVolume.profile.TryGet<Bloom>(out bloom))
        {
            bloom = globalVolume.profile.Add<Bloom>(false);
        }
        bloom.intensity.overrideState = true;
        
        // Vignette 컴포넌트
        if (!globalVolume.profile.TryGet<Vignette>(out vignette))
        {
            vignette = globalVolume.profile.Add<Vignette>(false);
        }
        vignette.intensity.overrideState = true;
        
        // Color Adjustments 컴포넌트
        if (!globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments = globalVolume.profile.Add<ColorAdjustments>(false);
        }
        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.colorFilter.overrideState = true;
        
        Debug.Log("✅ Post Processing initialized");
    }
    
    void StoreOriginalValues()
    {
        // Post Processing
        if (bloom != null)
        {
            originalBloomIntensity = bloom.intensity.value;
        }
        
        Debug.Log($"📊 Post Process Controller - Original bloom: {originalBloomIntensity}");
    }
    
    public void ApplyPostProcessing(float progress)
    {
        if (bloom == null || vignette == null || colorAdjustments == null) return;
        
        // Bloom 강도 증가 (9단계) - 값 최소화
        float newBloomIntensity = Mathf.Lerp(originalBloomIntensity, originalBloomIntensity + 1f, progress);
        bloom.intensity.value = newBloomIntensity;

        // Vignette 강도 증가 (9단계) - 값 더 감소
        float vignetteIntensity = Mathf.Lerp(0f, 0.15f, progress);
        vignette.intensity.value = vignetteIntensity;

        // Color Filter (9단계로 차가운 톤으로) - 변화량 더 감소
        Color newColorFilter = Color.Lerp(Color.white, new Color(0.92f, 0.95f, 1.0f), progress);
        colorAdjustments.colorFilter.value = newColorFilter;

        // Exposure (9단계로 어둡게) - 값 더 감소
        float exposureShift = Mathf.Lerp(0f, -0.5f, progress);
        colorAdjustments.postExposure.value = exposureShift;
        
        Debug.Log($"📊 Post Processing applied - Bloom: {newBloomIntensity:F2}, Vignette: {vignetteIntensity:F2}");
    }
}