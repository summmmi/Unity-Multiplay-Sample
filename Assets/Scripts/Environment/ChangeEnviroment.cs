using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mirror;
using System.Linq;

public class ChangeEnviroment : NetworkBehaviour
{
    [Header("=== Environment Controllers ===")]
    public SkyboxController skyboxController;
    public PostProcessController postProcessController;
    
    [Header("Weather")]
    public SimpleRainController rainController;
    public LightningFlash lightningFlash;
    
    [Header("Wind")]
    public WindZone windZone;

    [Header("=== Button Count & Stage System ===")]
    [SyncVar(hook = nameof(OnButtonCountChanged))]
    private int buttonPressCount = 0;

    [Header("Stage Settings")]
    [SerializeField] private int maxButtonCount = 9;

    // 3단계 시스템 (비, 천둥)
    private int GetWeatherStage()
    {
        if (buttonPressCount <= 0) return 0;
        if (buttonPressCount <= 3) return 1;  // 1-3: Stage 1
        if (buttonPressCount <= 6) return 2;  // 4-6: Stage 2
        return 3;  // 7-9: Stage 3
    }

    // 9단계 점진적 변화 (나머지 효과들)
    private float GetGradualProgress()
    {
        return Mathf.Clamp01((float)buttonPressCount / (float)maxButtonCount);
    }

    // Original Values
    private float originalFogDensity = 0.02f;
    private float originalWindStrength = 0.0f;

    void Start()
    {
        Debug.Log("🚀 ChangeEnvironment Manager initialized");

        InitializeControllers();
        InitializeWeatherSystem();
        InitializeWind();
        InitializeFog();

        // Store original values
        StoreOriginalValues();

        Debug.Log("✅ All environment systems initialized");
    }

    void InitializeControllers()
    {
        // Initialize SkyboxController
        if (skyboxController == null)
        {
            Debug.Log("🔍 Searching for SkyboxController...");
            skyboxController = FindObjectOfType<SkyboxController>();
            
            if (skyboxController == null)
            {
                Debug.LogError("❌ SkyboxController NOT FOUND in scene! Make sure SkyboxController script is attached to a GameObject.");
            }
            else
            {
                Debug.Log($"✅ SkyboxController found on GameObject: {skyboxController.gameObject.name}");
            }
        }
        
        // Initialize PostProcessController
        if (postProcessController == null)
        {
            Debug.Log("🔍 Searching for PostProcessController...");
            postProcessController = FindObjectOfType<PostProcessController>();
            
            if (postProcessController == null)
            {
                Debug.LogError("❌ PostProcessController NOT FOUND in scene! Make sure PostProcessController script is attached to a GameObject.");
            }
            else
            {
                Debug.Log($"✅ PostProcessController found on GameObject: {postProcessController.gameObject.name}");
            }
        }
        
        Debug.Log($"📊 Controllers initialization result - Skybox: {skyboxController != null}, PostProcess: {postProcessController != null}");
    }

    void InitializeWeatherSystem()
    {
        if (rainController == null)
        {
            GameObject weatherSystem = GameObject.Find("Weather System");
            if (weatherSystem != null)
            {
                rainController = weatherSystem.GetComponent<SimpleRainController>();
            }

            if (rainController == null)
            {
                rainController = FindObjectOfType<SimpleRainController>();
            }
        }

        if (rainController != null)
        {
            Debug.Log("✅ Rain Controller initialized");
        }
        else
        {
            Debug.LogWarning("⚠️ Rain Controller not found");
        }
    }


    void InitializeWind()
    {
        if (windZone == null)
        {
            windZone = FindObjectOfType<WindZone>();

            // Create WindZone if it doesn't exist
            if (windZone == null)
            {
                GameObject windObject = new GameObject("Wind Zone");
                windZone = windObject.AddComponent<WindZone>();
                windZone.mode = WindZoneMode.Directional;
                windZone.windMain = 0;
                windZone.windTurbulence = 0;
                windZone.windPulseMagnitude = 0.5f;
                windZone.windPulseFrequency = 0.25f;
                Debug.Log("✅ Wind Zone created");
            }
        }

        if (windZone != null)
        {
            Debug.Log("✅ Wind Zone initialized");
        }
    }


    void InitializeFog()
    {
        // Fog is controlled through RenderSettings
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        Debug.Log("✅ Fog settings initialized");
    }
    

    void StoreOriginalValues()
    {
        // Fog
        originalFogDensity = RenderSettings.fogDensity;

        // Wind
        if (windZone != null)
        {
            originalWindStrength = windZone.windMain;
        }

        Debug.Log($"📊 Original values stored - Fog: {originalFogDensity}, Wind: {originalWindStrength}");
    }

    // 아두이노에서 호출되는 메서드
    public void OnButtonPressed(string buttonData)
    {
        Debug.Log($"OnButtonPressed called with data: '{buttonData}', NetworkServer.active: {NetworkServer.active}");

        // 서버에서만 실행
        if (!NetworkServer.active)
        {
            Debug.Log("Not server, ignoring button press");
            return;
        }

        Debug.Log($"Processing button press: {buttonData}");

        // 버튼 누적 카운트 증가 (최대 9까지)
        buttonPressCount = Mathf.Min(buttonPressCount + 1, maxButtonCount);

        // 환경 변화 적용
        ApplyEnvironmentChanges();

        // 모든 클라이언트에 동기화
        RpcSyncEnvironmentChanges(buttonPressCount);
    }

    // 환경 변화 적용 (서버에서만 호출)
    private void ApplyEnvironmentChanges()
    {
        ApplyControllersChanges();
        ApplyWeatherChanges();
        ApplyWindChanges();

        Debug.Log($"✅ All environment changes applied - Button count: {buttonPressCount}");
    }
    
    private void ApplyControllersChanges()
    {
        int weatherStage = GetWeatherStage();
        float gradualProgress = GetGradualProgress();
        
        // Apply skybox and lighting changes
        if (skyboxController != null)
        {
            Debug.Log($"🌤️ SkyboxController found, applying weather stage: {weatherStage}");
            skyboxController.ApplySkyboxAndLighting(weatherStage);
        }
        else
        {
            Debug.LogError("❌ SkyboxController is NULL! Cannot apply skybox changes!");
        }
        
        // Apply post processing changes
        if (postProcessController != null)
        {
            postProcessController.ApplyPostProcessing(gradualProgress);
        }
        else
        {
            Debug.LogError("❌ PostProcessController is NULL!");
        }
        
        Debug.Log($"✅ Controllers updated - Weather stage: {weatherStage}, Gradual progress: {gradualProgress:F2}");
    }


    private void ApplyWeatherChanges()
    {
        int weatherStage = GetWeatherStage(); // 0-3 stages

        // 3단계 시스템으로 비 강도 설정 - 값 대폭 증가
        float newRainIntensity = 0f;
        switch (weatherStage)
        {
            case 0:
                newRainIntensity = 0f; // 비 없음
                break;
            case 1:
                newRainIntensity = 5f; // 약한 비 (2 → 5)
                break;
            case 2:
                newRainIntensity = 15f; // 보통 비 (5 → 15)
                break;
            case 3:
                newRainIntensity = 30f; // 강한 비/폭우 (10 → 30)
                break;
        }

        SetRainIntensity(newRainIntensity);

        Debug.Log($"🌧️ Weather [Stage {weatherStage}/3] - Rain intensity: {newRainIntensity}");
    }

    // private void ApplyFogChanges()
    // {
    //     float progress = GetGradualProgress(); // 0 to 1 (9 stages)

    //     // 안개 밀도 증가 (9단계) - 값 대폭 감소
    //     float newFogDensity = Mathf.Lerp(originalFogDensity, originalFogDensity + 0.03f, progress);
    //     RenderSettings.fogDensity = newFogDensity;

    //     // 안개 색상도 9단계로 어둡게 - 변화량 감소
    //     float fogColorBrightness = Mathf.Lerp(1f, 0.7f, progress);
    //     Color newFogColor = new Color(
    //         fogColorBrightness * 0.5f,
    //         fogColorBrightness * 0.5f,
    //         fogColorBrightness * 0.6f
    //     );
    //     RenderSettings.fogColor = newFogColor;

    //     Debug.Log($"🌫️ Fog [Stage {buttonPressCount}/9] - Density: {newFogDensity:F3}, Brightness: {fogColorBrightness:F2}");
    // }


    private void ApplyWindChanges()
    {
        if (windZone == null) return;

        float progress = GetGradualProgress(); // 0 to 1 (9 stages)

        // 바람 강도 증가 (9단계)
        float newWindStrength = Mathf.Lerp(0f, 20f, progress);
        windZone.windMain = newWindStrength;

        // 바람 난류도 증가 (9단계)
        float turbulence = Mathf.Lerp(0f, 5f, progress);
        windZone.windTurbulence = turbulence;

        // 바람 펄스도 점진적으로 증가
        windZone.windPulseMagnitude = Mathf.Lerp(0.5f, 2f, progress);
        windZone.windPulseFrequency = Mathf.Lerp(0.25f, 1f, progress);

        Debug.Log($"💨 Wind [Stage {buttonPressCount}/9] - Strength: {newWindStrength:F2}, Turbulence: {turbulence:F2}");
    }
    


    // 비 강도 설정
    private void SetRainIntensity(float intensity)
    {
        Debug.Log($"🚀 SetRainIntensity called with intensity: {intensity}");

        if (rainController == null)
        {
            Debug.LogError("❌ RainController is null - cannot set rain intensity");
            Debug.LogError("❌ This means the SimpleRainController component was not found!");
            return;
        }

        Debug.Log($"✅ RainController found: {rainController.gameObject.name}");

        if (rainController.rainInstance == null)
        {
            Debug.LogError("❌ RainController.rainInstance is null - cannot set rain intensity");
            Debug.LogError("❌ This means the rain prefab was not instantiated in SimpleRainController!");
            return;
        }

        Debug.Log($"✅ RainInstance found: {rainController.rainInstance.name}");

        ParticleSystem[] rainParticles = rainController.rainInstance.GetComponentsInChildren<ParticleSystem>();
        Debug.Log($"🔍 Found {rainParticles.Length} particle systems in rain instance");

        foreach (ParticleSystem ps in rainParticles)
        {
            Debug.Log($"🔍 Processing Particle System: '{ps.name}'");

            // ImpactDrops는 건드리지 않고 메인 비 파티클만 처리
            if (ps.name.Contains("Impact") || ps.name.Contains("Splash") || ps.name.Contains("Drop"))
            {
                Debug.Log($"⏭️ Skipping '{ps.name}' - this is a splash/impact effect");
                continue;
            }

            // 현재 emission rate 확인
            var emission = ps.emission;
            float currentRate = emission.rateOverTime.constant;
            Debug.Log($"🔍 Particle System '{ps.name}' current rate: {currentRate}");

            // 새로운 rate 설정 - 메인 비 파티클을 극적으로 증가
            float baseRate = 200f; // 기본 emission rate (매우 높게)
            float newRate = baseRate * intensity;

            var rateOverTime = emission.rateOverTime;
            rateOverTime.constant = newRate;
            emission.rateOverTime = rateOverTime;

            // 메인 파티클 시스템 속성들을 모두 조정
            var main = ps.main;
            main.maxParticles = Mathf.RoundToInt(newRate * 15); // emission rate의 15배

            // 속도 증가 - 비가 더 빠르게 떨어지게
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-20f * intensity, -10f * intensity); // 강도에 따라 속도 증가

            // 크기는 원래대로 유지 (키우지 않음)
            // main.startSize는 건드리지 않음

            // 생존시간 조정 - 적당히
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);

            Debug.Log($"🌧️ Enhanced rain properties - Velocity Y: -{20f * intensity} to -{10f * intensity}, Lifetime: 1-2 seconds");

            // 변경 후 실제 값 확인
            float actualNewRate = ps.emission.rateOverTime.constant;
            Debug.Log($"✅ MAIN RAIN Particle System '{ps.name}' rate: {currentRate} → {actualNewRate} (target: {newRate}, maxParticles: {main.maxParticles})");

            // 파티클 시스템이 활성화되어 있는지 확인
            if (!ps.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"⚠️ Particle System '{ps.name}' is not active in hierarchy!");
            }

            if (!ps.isPlaying)
            {
                Debug.LogWarning($"⚠️ Particle System '{ps.name}' is not playing!");
                ps.Play(); // 강제로 재생
            }
        }

        // 비 사운드 볼륨도 강도에 맞게 조절
        SetRainSoundVolume(intensity);

        Debug.Log($"✅ Rain intensity set to: {intensity} (target emission rate: {200f * intensity})");
    }

    // 비 사운드 볼륨 조절
    private void SetRainSoundVolume(float intensity)
    {
        if (rainController != null && rainController.audioSource != null)
        {
            // 기본 볼륨 0.4에서 강도에 따라 조절 (최대 1.0까지)
            float baseVolume = 0.4f;
            float maxVolume = 1.0f;
            float volumeMultiplier = Mathf.Clamp(intensity / 10f, 0.1f, 2.5f); // intensity 10일 때 최대
            float newVolume = Mathf.Clamp(baseVolume * volumeMultiplier, 0.1f, maxVolume);

            rainController.audioSource.volume = newVolume;
            Debug.Log($"🔊 Rain sound volume: {rainController.audioSource.volume} (intensity: {intensity}, multiplier: {volumeMultiplier})");
        }
        else
        {
            Debug.LogWarning("⚠️ Rain audio source not found - cannot adjust volume");
        }
    }

    // SyncVar Hook - 클라이언트에서 자동 호출
    void OnButtonCountChanged(int oldCount, int newCount)
    {
        if (!NetworkServer.active) // 클라이언트에서만 실행
        {
            buttonPressCount = newCount;
            ApplyEnvironmentChanges();
            Debug.Log($"✅ Client environment synced via SyncVar - Button count: {newCount}");
        }
    }

    [ClientRpc]
    void RpcSyncEnvironmentChanges(int pressCount)
    {
        // 모든 클라이언트에서 환경 변화 동기화
        buttonPressCount = pressCount;
        ApplyEnvironmentChanges();
        Debug.Log($"✅ Client environment synced via RPC - Button count: {pressCount}");
    }


    // 테스트용 메서드 (키보드로 테스트 가능)
    void Update()
    {
        // 서버에서만 실행
        if (!NetworkServer.active) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space key pressed, testing environment change");
            OnButtonPressed("test");
        }
    }
}
