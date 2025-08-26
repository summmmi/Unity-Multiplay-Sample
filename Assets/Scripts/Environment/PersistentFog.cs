using UnityEngine;

public class PersistentFog : MonoBehaviour
{
    [Header("Fog Settings")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.5f, 0.5f, 0.5f, 1.0f); // 회색 안개
    public float fogDensity = 0.015f;
    public FogMode fogMode = FogMode.Exponential;
    
    [Header("Update Settings")]
    public bool continuousUpdate = true;
    public float updateInterval = 0.1f;
    
    private float lastUpdateTime;
    
    void Start()
    {
        ApplyFogSettings();
        lastUpdateTime = Time.time;
    }
    
    void Update()
    {
        if (continuousUpdate && Time.time - lastUpdateTime >= updateInterval)
        {
            ApplyFogSettings();
            lastUpdateTime = Time.time;
        }
    }
    
    void ApplyFogSettings()
    {
        RenderSettings.fog = enableFog;
        
        if (enableFog)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
        }
    }
    
    // 외부에서 안개 설정 변경할 때 사용
    public void UpdateFogSettings(bool enable, Color color, float density)
    {
        enableFog = enable;
        fogColor = color;
        fogDensity = density;
        ApplyFogSettings();
    }
    
    // 안개 켜기/끄기
    public void ToggleFog()
    {
        enableFog = !enableFog;
        ApplyFogSettings();
        Debug.Log($"[PersistentFog] 안개 {(enableFog ? "활성화" : "비활성화")}");
    }
    
    // 안개 농도만 변경
    public void SetFogDensity(float density)
    {
        fogDensity = density;
        ApplyFogSettings();
    }
    
    void OnValidate()
    {
        // Inspector에서 값 변경 시 즉시 적용
        if (Application.isPlaying)
        {
            ApplyFogSettings();
        }
    }
}