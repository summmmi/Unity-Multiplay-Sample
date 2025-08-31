using UnityEngine;
using System.Collections;

public class SkyboxController : MonoBehaviour
{
    [Header("Skybox Materials (2 materials only)")]
    public Material skyboxSunny; // Stage 0-2용
    public Material skyboxDark;  // Stage 3용

    [System.Serializable]
    public class StageSettings
    {
        [Header("Skybox Settings")]
        [Range(0.1f, 3f)]
        public float exposure = 1.0f;
        
        [Header("Light Settings")]
        public Color lightColor = Color.white;
        [Range(0.1f, 2f)]
        public float lightIntensity = 1.0f;
        [Range(0.1f, 2f)]
        public float ambientIntensity = 1.0f;
    }

    [Header("Stage Settings (4 stages: 0-3)")]
    public StageSettings[] stageSettings = new StageSettings[4]
    {
        new StageSettings { exposure = 1.3f, lightColor = Color.white, lightIntensity = 1.0f, ambientIntensity = 1.2f }, // Stage 0 - 맑음
        new StageSettings { exposure = 1.0f, lightColor = new Color(0.9f, 0.9f, 0.95f), lightIntensity = 0.8f, ambientIntensity = 1.0f }, // Stage 1 - 약간 흐림
        new StageSettings { exposure = 0.7f, lightColor = new Color(0.7f, 0.8f, 0.9f), lightIntensity = 0.6f, ambientIntensity = 0.8f }, // Stage 2 - 흐림
        new StageSettings { exposure = 0.4f, lightColor = new Color(0.5f, 0.6f, 0.8f), lightIntensity = 0.4f, ambientIntensity = 0.6f }  // Stage 3 - 어둠
    };

    [Header("Lighting Settings")]
    public Light directionalLight;

    [Header("Transition Settings")]
    [Range(0.5f, 5f)]
    public float transitionDuration = 2f;
    
    [Header("Overlay Transition")]
    public bool useOverlayTransition = true;
    public Material overlayMaterial; // Overlay용 임시 머테리얼

    private Material originalSkybox;
    private Color originalLightColor;
    private float originalLightIntensity;
    private Coroutine currentTransition = null;

    void Start()
    {
        InitializeSkybox();
        InitializeLighting();
        StoreOriginalValues();
    }

    void InitializeSkybox()
    {
        // Load Skybox Materials from Resources if not assigned
        if (skyboxSunny == null)
        {
            skyboxSunny = Resources.Load<Material>("Materials/SkyBox_Sunny");
            Debug.Log($"🌤️ Loading Sunny sky material: {(skyboxSunny != null ? "SUCCESS" : "FAILED")}");
        }
        if (skyboxDark == null)
        {
            skyboxDark = Resources.Load<Material>("Materials/SkyBox_Dark");
            Debug.Log($"🌑 Loading Dark material: {(skyboxDark != null ? "SUCCESS" : "FAILED")}");
        }

        Debug.Log($"✅ Skybox Materials loaded - Sunny: {skyboxSunny != null}, Dark: {skyboxDark != null}");
        
        // Initialize default settings if not set in Inspector
        if (stageSettings == null || stageSettings.Length != 4)
        {
            stageSettings = new StageSettings[4];
            for (int i = 0; i < 4; i++)
            {
                stageSettings[i] = new StageSettings();
            }
        }
    }

    void InitializeLighting()
    {
        if (directionalLight == null)
        {
            directionalLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            if (directionalLight == null)
            {
                Light[] lights = FindObjectsOfType<Light>();
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {
                        directionalLight = light;
                        break;
                    }
                }
            }
        }

        if (directionalLight != null)
        {
            Debug.Log("✅ Directional Light initialized for Skybox Controller");
        }
        else
        {
            Debug.LogWarning("⚠️ Directional Light not found for Skybox Controller");
        }
    }

    void StoreOriginalValues()
    {
        // Skybox
        originalSkybox = RenderSettings.skybox;

        // Lighting
        if (directionalLight != null)
        {
            originalLightIntensity = directionalLight.intensity;
            originalLightColor = directionalLight.color;
        }

        // HDRI 환경광 초기 설정
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;

        // 초기 스카이박스 적용 (Stage 0 = 맑은 날씨)
        ApplySkyboxAndLighting(0);

        Debug.Log($"📊 Skybox Controller - Original values stored, initial skybox applied");
    }

    void OnDestroy()
    {
        ResetAllSkyboxesToDefault();
    }

    void OnApplicationQuit()
    {
        ResetAllSkyboxesToDefault();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            ResetAllSkyboxesToDefault();
    }

    public void ResetAllSkyboxesToDefault()
    {
        // 스카이박스를 기본값으로 리셋
        if (skyboxSunny != null && stageSettings.Length > 0)
        {
            skyboxSunny.SetFloat("_Exposure", stageSettings[0].exposure);
        }
        if (skyboxDark != null && stageSettings.Length > 3)
        {
            skyboxDark.SetFloat("_Exposure", stageSettings[3].exposure);
        }
        Debug.Log($"🔄 Reset Skyboxes to default values");
    }

    public void ApplySkyboxAndLighting(int weatherStage)
    {
        // 기존 전환이 진행 중이면 중단
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        // 부드러운 전환 시작
        currentTransition = StartCoroutine(TransitionSkyboxSmooth(weatherStage));
    }

    private IEnumerator TransitionSkyboxSmooth(int weatherStage)
    {
        // Stage 범위 체크
        weatherStage = Mathf.Clamp(weatherStage, 0, 3);
        
        // 현재 상태 저장
        Material startSkybox = RenderSettings.skybox;
        Color startLightColor = directionalLight != null ? directionalLight.color : Color.white;
        float startLightIntensity = directionalLight != null ? directionalLight.intensity : 1f;
        float startAmbientIntensity = RenderSettings.ambientIntensity;
        float startExposure = startSkybox != null ? startSkybox.GetFloat("_Exposure") : 1.0f;

        // 목표 스카이박스 결정 (0-2단계는 Sunny, 3단계만 Dark)
        Material targetSkybox = (weatherStage <= 2) ? skyboxSunny : skyboxDark;
        
        // 목표 설정값 (Inspector에서 설정한 값 사용)
        StageSettings targetSettings = stageSettings[weatherStage];
        
        if (targetSkybox == null || targetSettings == null)
        {
            Debug.LogWarning($"Target skybox or settings not found for stage {weatherStage}");
            yield break;
        }

        // Inspector에서 설정한 값 사용
        float targetExposure = targetSettings.exposure;
        Color targetLightColor = targetSettings.lightColor;
        float targetLightIntensity = targetSettings.lightIntensity;
        float targetAmbientIntensity = targetSettings.ambientIntensity;

        // 스카이박스가 바뀔 경우 교체
        if (startSkybox != targetSkybox && targetSkybox != null)
        {
            // Stage 2->3 전환이고 overlay 옵션이 켜져있으면 overlay 전환 사용
            if (useOverlayTransition && weatherStage == 3 && startSkybox == skyboxSunny && targetSkybox == skyboxDark)
            {
                yield return StartCoroutine(OverlayTransition(startSkybox, targetSkybox, targetExposure, targetLightColor, targetLightIntensity, targetAmbientIntensity));
                currentTransition = null;
                yield break;
            }
            else
            {
                // 일반 전환
                RenderSettings.skybox = targetSkybox;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
                
                // 새 스카이박스의 현재 Exposure 값으로 시작 (검정색 방지)
                float skyboxOriginalExposure = targetSkybox.GetFloat("_Exposure");
                startExposure = skyboxOriginalExposure;
                
                Debug.Log($"🌤️ Skybox switched to: {targetSkybox.name} for stage {weatherStage}, Starting with original exposure: {skyboxOriginalExposure}");
            }
        }

        // 부드러운 전환
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / transitionDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Exposure 점진적 변경
            if (RenderSettings.skybox != null)
            {
                float currentExposure = Mathf.Lerp(startExposure, targetExposure, smoothProgress);
                RenderSettings.skybox.SetFloat("_Exposure", currentExposure);
            }

            // 조명도 점진적 변경
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startLightColor, targetLightColor, smoothProgress);
                directionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, smoothProgress);
            }

            // 환경광도 점진적 변경
            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbientIntensity, targetAmbientIntensity, smoothProgress);
            RenderSettings.reflectionIntensity = RenderSettings.ambientIntensity;

            yield return null;
        }

        // 최종 값 확실히 설정
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetFloat("_Exposure", targetExposure);
        }

        if (directionalLight != null)
        {
            directionalLight.color = targetLightColor;
            directionalLight.intensity = targetLightIntensity;
        }

        RenderSettings.ambientIntensity = targetAmbientIntensity;
        RenderSettings.reflectionIntensity = targetAmbientIntensity;

        // DynamicGI.UpdateEnvironment()를 비동기로 처리
        yield return null; // 한 프레임 대기
        DynamicGI.UpdateEnvironment();
        
        Debug.Log($"✅ Transition completed - Stage {weatherStage}, Exposure: {targetExposure}");
        currentTransition = null;
    }

    private IEnumerator OverlayTransition(Material startSkybox, Material targetSkybox, float targetExposure, Color targetLightColor, float targetLightIntensity, float targetAmbientIntensity)
    {
        Debug.Log($"🌈 Starting overlay transition from {startSkybox.name} to {targetSkybox.name}");

        // 현재 스카이박스의 exposure 값 저장
        float startExposure = startSkybox.GetFloat("_Exposure");
        
        // 현재 조명 상태 저장
        Color startLightColor = directionalLight != null ? directionalLight.color : Color.white;
        float startLightIntensity = directionalLight != null ? directionalLight.intensity : 1f;
        float startAmbientIntensity = RenderSettings.ambientIntensity;

        // 먼저 target skybox로 교체하되, 시작 exposure로 설정하여 부드럽게 시작
        RenderSettings.skybox = targetSkybox;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
        
        // Target skybox를 시작 exposure로 설정하여 갑작스러운 변화 방지
        if (targetSkybox.HasProperty("_Exposure"))
        {
            targetSkybox.SetFloat("_Exposure", startExposure);
        }

        Debug.Log($"🌈 Skybox switched to {targetSkybox.name}, transitioning exposure from {startExposure} to {targetExposure}");

        // Exposure와 조명을 점진적으로 변경
        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / transitionDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Exposure 점진적 변경 (시작 exposure에서 목표 exposure로)
            if (targetSkybox.HasProperty("_Exposure"))
            {
                float currentExposure = Mathf.Lerp(startExposure, targetExposure, smoothProgress);
                targetSkybox.SetFloat("_Exposure", currentExposure);
            }

            // 조명도 점진적 변경
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(startLightColor, targetLightColor, smoothProgress);
                directionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, smoothProgress);
            }

            // 환경광도 점진적 변경
            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbientIntensity, targetAmbientIntensity, smoothProgress);
            RenderSettings.reflectionIntensity = RenderSettings.ambientIntensity;

            yield return null;
        }

        // 최종 값 확실히 설정
        if (targetSkybox.HasProperty("_Exposure"))
        {
            targetSkybox.SetFloat("_Exposure", targetExposure);
        }

        if (directionalLight != null)
        {
            directionalLight.color = targetLightColor;
            directionalLight.intensity = targetLightIntensity;
        }

        RenderSettings.ambientIntensity = targetAmbientIntensity;
        RenderSettings.reflectionIntensity = targetAmbientIntensity;

        // 환경 업데이트
        yield return null;
        DynamicGI.UpdateEnvironment();

        Debug.Log($"✅ Overlay transition completed - Final skybox: {targetSkybox.name}, Exposure: {targetExposure}");
    }
}