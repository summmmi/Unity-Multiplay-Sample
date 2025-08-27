using UnityEngine;
using System.Collections;

public class SkyboxController : MonoBehaviour
{
    [Header("Skybox Materials (2 materials)")]
    public Material[] skyboxMaterials = new Material[2]; // 0=Sunny, 1=Dark

    [Header("Lighting Settings")]
    public Light directionalLight;

    private Material originalSkybox;
    private Color originalLightColor;
    private float originalLightIntensity;

    // 각 스카이박스의 원래 Exposure 값 저장
    private float[] originalExposureValues = new float[2];

    void Start()
    {
        InitializeSkybox();
        InitializeLighting();
        StoreOriginalValues();
    }

    void InitializeSkybox()
    {
        // Load Skybox Materials from Resources
        if (skyboxMaterials[0] == null)
        {
            skyboxMaterials[0] = Resources.Load<Material>("Materials/SkyBox_Sunny");
            Debug.Log($"🌤️ Loading Sunny sky material: {(skyboxMaterials[0] != null ? "SUCCESS" : "FAILED")}");
        }
        if (skyboxMaterials[1] == null)
        {
            skyboxMaterials[1] = Resources.Load<Material>("Materials/SkyBox_Dark");
            Debug.Log($"🌑 Loading Dark material: {(skyboxMaterials[1] != null ? "SUCCESS" : "FAILED")}");
        }

        int loadedCount = 0;
        for (int i = 0; i < skyboxMaterials.Length; i++)
        {
            if (skyboxMaterials[i] != null) loadedCount++;
        }

        Debug.Log($"✅ Skybox Materials loaded: {loadedCount}/2");

        // 각 스카이박스의 원래 Exposure 값 저장 및 초기화
        for (int i = 0; i < skyboxMaterials.Length && i < originalExposureValues.Length; i++)
        {
            if (skyboxMaterials[i] != null)
            {
                // Inspector 기본값 설정
                if (i == 0) originalExposureValues[i] = 1.1f;  // Sunny
                else if (i == 1) originalExposureValues[i] = 0.6f;  // Dark

                // 시작시 원래 값으로 리셋
                skyboxMaterials[i].SetFloat("_Exposure", originalExposureValues[i]);
                Debug.Log($"💾 Reset Exposure[{i}] to default: {originalExposureValues[i]}");
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

    private Coroutine currentTransition = null;

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
        // 모든 스카이박스 원래 값으로 리셋
        for (int i = 0; i < skyboxMaterials.Length; i++)
        {
            if (skyboxMaterials[i] != null && i < originalExposureValues.Length)
            {
                skyboxMaterials[i].SetFloat("_Exposure", originalExposureValues[i]);
                Debug.Log($"🔄 Reset Skybox[{i}] Exposure to: {originalExposureValues[i]}");
            }
        }
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
        // 현재 상태 저장
        Material startSkybox = RenderSettings.skybox;
        Color startLightColor = directionalLight != null ? directionalLight.color : originalLightColor;
        float startLightIntensity = directionalLight != null ? directionalLight.intensity : originalLightIntensity;
        float startAmbientIntensity = RenderSettings.ambientIntensity;

        // 현재 스카이박스의 실제 Exposure 값 (현재 설정된 값)
        float startExposure = startSkybox != null ? startSkybox.GetFloat("_Exposure") : 1.0f;
        float startReflectionIntensity = RenderSettings.reflectionIntensity;

        // 목표 상태 계산
        Material targetSkybox = null;
        Color targetLightColor = originalLightColor;
        float targetLightIntensity = originalLightIntensity;
        float targetAmbientIntensity = 1.0f;

        switch (weatherStage)
        {
            case 0: // 비 없음 - 맑은 하늘
                targetSkybox = skyboxMaterials[0]; // Sunny
                targetLightColor = new Color(1.0f, 0.95f, 0.8f); // 따뜻한 노란색
                targetLightIntensity = 1.2f;
                targetAmbientIntensity = 1.2f;
                break;

            case 1: // 약한 비 - 약간 흐림
                targetSkybox = skyboxMaterials[0]; // 여전히 Sunny (약간 흐린 정도)
                targetLightColor = new Color(0.9f, 0.9f, 0.95f); // 약간 차가운 톤
                targetLightIntensity = 0.8f;
                targetAmbientIntensity = 0.8f;
                break;

            case 2: // 보통 비 - 어둠
                targetSkybox = skyboxMaterials[1]; // Dark
                targetLightColor = new Color(0.7f, 0.8f, 0.9f);
                targetLightIntensity = 0.65f;
                targetAmbientIntensity = 0.65f;
                break;

            case 3: // 강한 비 - 폭풍 (Dark 더 어둡게)
                targetSkybox = skyboxMaterials[1]; // Dark
                targetLightColor = new Color(0.5f, 0.6f, 0.8f);
                targetLightIntensity = 0.35f;
                targetAmbientIntensity = 0.35f;
                break;
        }

        // fade 효과로 부드럽게 전환
        float fadeTime = 0.5f;
        float elapsedTime = 0f;

        Debug.Log($"🌤️ Smooth transition to stage {weatherStage}");

        // 단계별 목표 Exposure 계산
        float targetExposure = 1.0f;
        switch (weatherStage)
        {
            case 0: // Sunny 원래값
                targetExposure = originalExposureValues[0];
                break;
            case 1: // Sunny 약간 어둡게
                targetExposure = originalExposureValues[0] * 0.8f;
                break;
            case 2: // Dark 0.5
                targetExposure = 0.5f;
                break;
            case 3: // Dark 0.3 (폭풍)
                targetExposure = 0.3f;
                break;
        }

        // 스카이박스가 바뀔 경우 바로 교체
        if (startSkybox != targetSkybox && targetSkybox != null)
        {
            RenderSettings.skybox = targetSkybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Skybox;
            targetSkybox.SetFloat("_Exposure", startExposure); // 현재 밝기로 시작
            Debug.Log($"🌤️ Skybox switched to: {targetSkybox.name}");
        }

        // 현재 밝기에서 목표 밝기로 부드럽게 전환
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeTime;
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

        DynamicGI.UpdateEnvironment();

        Debug.Log($"✅ Quick fade completed - Stage {weatherStage}");
        currentTransition = null;
    }
}