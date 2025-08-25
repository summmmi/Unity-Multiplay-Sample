using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mirror;
using System.Linq;

public class ChangeEnviroment : NetworkBehaviour
{
    [Header("Environment Settings")]
    public Volume globalVolume;

    [Header("Weather System")]
    public SimpleRainController rainController;

    private Bloom bloom;
    
    // 아두이노 버튼 누적 상태
    [SyncVar] private int buttonPressCount = 0;
    private float originalBloomIntensity = 0.0f; // 기본 블룸 강도
    private float originalRainIntensity = 1.0f; // 기본 비 강도 (더 높게)

    void Start()
    {
        Debug.Log("🚀 ChangeEnvironment Start() called");
        
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

        // Rain Controller 찾기 - 철저한 디버깅
        Debug.Log("🔍 Starting Rain Controller search...");
        
        if (rainController == null)
        {
            Debug.Log("🔍 rainController is null, searching for Weather System...");
            
            GameObject weatherSystem = GameObject.Find("Weather System");
            if (weatherSystem != null)
            {
                Debug.Log($"✅ WeatherSystem GameObject found: {weatherSystem.name}");
                Debug.Log($"🔍 WeatherSystem components: {string.Join(", ", weatherSystem.GetComponentsInChildren<Component>().Select(c => c.GetType().Name))}");
                
                rainController = weatherSystem.GetComponent<SimpleRainController>();
                if (rainController != null)
                {
                    Debug.Log("✅ SimpleRainController found in Weather System GameObject");
                    Debug.Log($"🔍 rainController.rainInstance: {(rainController.rainInstance != null ? rainController.rainInstance.name : "NULL")}");
                }
                else
                {
                    Debug.LogError("❌ SimpleRainController component NOT found in Weather System GameObject!");
                    Debug.LogError($"❌ Available components: {string.Join(", ", weatherSystem.GetComponents<Component>().Select(c => c.GetType().Name))}");
                }
            }
            else
            {
                Debug.LogError("❌ Weather System GameObject not found!");
                Debug.Log($"🔍 All GameObjects in scene: {string.Join(", ", FindObjectsOfType<GameObject>().Select(go => go.name))}");
            }
            
            if (rainController == null)
            {
                Debug.Log("🔍 Trying FindObjectOfType<SimpleRainController>()...");
                rainController = FindObjectOfType<SimpleRainController>();
                if (rainController != null)
                {
                    Debug.Log($"✅ SimpleRainController found via FindObjectOfType on GameObject: {rainController.gameObject.name}");
                    Debug.Log($"🔍 rainController.rainInstance: {(rainController.rainInstance != null ? rainController.rainInstance.name : "NULL")}");
                }
                else
                {
                    Debug.LogError("❌ SimpleRainController not found anywhere in scene!");
                }
            }
        }
        else
        {
            Debug.Log("✅ rainController was already assigned in Inspector");
            Debug.Log($"🔍 rainController.rainInstance: {(rainController.rainInstance != null ? rainController.rainInstance.name : "NULL")}");
        }

        // Bloom 컴포넌트 가져오기 또는 추가
        if (!globalVolume.profile.TryGet<Bloom>(out bloom))
        {
            bloom = globalVolume.profile.Add<Bloom>(false);
        }

        // 초기 설정
        bloom.intensity.overrideState = true;
        originalBloomIntensity = bloom.intensity.value;
        
        Debug.Log($"Original bloom intensity: {originalBloomIntensity}");
        
        // 초기 비 강도 설정 (약하게)
        SetRainIntensity(originalRainIntensity);
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

        // 버튼 누적 카운트 증가
        buttonPressCount++;
        
        // 환경 변화 적용
        ApplyEnvironmentChanges();
        
        // 모든 클라이언트에 동기화
        RpcSyncEnvironmentChanges(buttonPressCount);
    }

    // 환경 변화 적용 (서버에서만 호출)
    private void ApplyEnvironmentChanges()
    {
        // 1. Bloom 강도 증가 (누적)
        float newBloomIntensity = originalBloomIntensity + (buttonPressCount * 5.0f); // 버튼 누를 때마다 5씩 증가
        newBloomIntensity = Mathf.Clamp(newBloomIntensity, 0f, 50f); // 최대 50까지
        bloom.intensity.value = newBloomIntensity;
        
        // 2. 비 강도 증가 (누적) - 더 크게 변화
        float newRainIntensity = originalRainIntensity + (buttonPressCount * 1.0f); // 버튼 누를 때마다 100% 증가
        newRainIntensity = Mathf.Clamp(newRainIntensity, 0.5f, 50.0f); // 최대 5000%까지
        SetRainIntensity(newRainIntensity);
        
        Debug.Log($"✅ Environment changes applied - Press count: {buttonPressCount}, Bloom intensity: {newBloomIntensity}, Rain intensity: {newRainIntensity}");
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
            float baseRate = 500f; // 기본 emission rate (매우 높게)
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

    [ClientRpc]
    void RpcSyncEnvironmentChanges(int pressCount)
    {
        // 모든 클라이언트에서 환경 변화 동기화
        buttonPressCount = pressCount;
        
        // 1. Bloom 강도 동기화
        float newBloomIntensity = originalBloomIntensity + (buttonPressCount * 5.0f);
        newBloomIntensity = Mathf.Clamp(newBloomIntensity, 0f, 50f);
        bloom.intensity.value = newBloomIntensity;
        
        // 2. 비 강도 동기화
        float newRainIntensity = originalRainIntensity + (buttonPressCount * 1.0f);
        newRainIntensity = Mathf.Clamp(newRainIntensity, 0.5f, 50.0f);
        SetRainIntensity(newRainIntensity);
        
        Debug.Log($"✅ Client synced - Press count: {buttonPressCount}, Bloom intensity: {newBloomIntensity}, Rain intensity: {newRainIntensity}");
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
