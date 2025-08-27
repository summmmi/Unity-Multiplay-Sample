using UnityEngine;
using System.Collections;

public class LightningFlash : MonoBehaviour
{
    [Header("References")]
    public Light sun;             // Sun(Directional Light)
    public Light flashLight;      // Flash light for lightning (default OFF)
    public AudioSource thunder;   // Thunder sound Audio Source

    [Header("Timing")]
    public float minInterval = 8f;
    public float maxInterval = 14f;

    [Header("Burst Settings")]
    public int minFlashesPerBurst = 1;   // 1-2 flashes
    public int maxFlashesPerBurst = 2;
    public float interFlashGap = 0.12f; // Gap between flashes

    [Header("Flash Light Settings")]
    public float flashDuration = 0.2f;    // Light flash duration (increased)
    public float flashLightIntensity = 50f; // Flash light intensity (very bright)
    public float sunFlashIntensity = 3.0f; // Temporary sun intensity (increased)
    public float sunRecoverIntensity = 0.35f; // Stage 3 sun default

    [Header("Flash Skybox Settings")]
    public bool flashSkybox = true;           // Skybox flash ON/OFF
    public float skyboxExposureBoost = 2.5f;  // Exposure boost (increased for visibility)
    public float skyboxFlashDuration = 0.25f;  // Duration (increased)

    [Header("Audio")]
    [Range(0f, 1.5f)] public float thunderVolume = 1.0f;  // Volume control
    [Range(1f, 5f)] public float thunderMinDelay = 2f;     // 천둥소리 최소 딜레이
    [Range(3f, 8f)] public float thunderMaxDelay = 5f;     // 천둥소리 최대 딜레이

    [Header("Control")]
    public bool isActive = false; // External control

    void Start()
    {
        // Start에서는 절대 MainLoop 시작하지 않음
        // 오직 StartLightning()에서만 시작
        Debug.Log($"⚡ LightningFlash Start() - isActive: {isActive}");
    }

    void OnEnable()
    {
        // OnEnable에서는 아무것도 하지 않음 - Start에서 처리
    }
    void OnDisable() { StopAllCoroutines(); }

    IEnumerator MainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            yield return StartCoroutine(DoBurst());   // Flash burst

            if (thunder != null && thunder.clip != null)
            {
                thunder.volume = thunderVolume;
                float randomDelay = Random.Range(thunderMinDelay, thunderMaxDelay);
                thunder.PlayDelayed(randomDelay);
                Debug.Log($"⚡ 번개 플래시 완료, 천둥소리 {randomDelay:F1}초 후 재생");
            }
        }
    }

    IEnumerator DoBurst()
    {
        int count = Random.Range(minFlashesPerBurst, maxFlashesPerBurst + 1);
        for (int i = 0; i < count; i++)
        {
            // Single flash (light + skybox)
            Coroutine lc = StartCoroutine(FlashLightOnce());
            Coroutine sc = flashSkybox ? StartCoroutine(FlashSkyboxOnce()) : null;
            if (lc != null) yield return lc;
            if (sc != null) yield return sc;

            // Gap between multiple flashes
            if (i < count - 1)
                yield return new WaitForSeconds(interFlashGap);
        }
    }

    IEnumerator FlashLightOnce()
    {
        if (flashLight != null)
        {
            // Random position for lightning
            Vector3 originalPos = flashLight.transform.position;
            float randomX = Random.Range(-20f, 20f);
            float randomY = Random.Range(2f, 15f);  // Height variation
            float randomZ = Random.Range(-20f, 20f);
            flashLight.transform.position = new Vector3(randomX, randomY, randomZ);

            // Flash light on and off with high intensity
            float originalIntensity = flashLight.intensity;
            flashLight.intensity = flashLightIntensity;
            flashLight.enabled = true;
            yield return new WaitForSeconds(flashDuration);
            flashLight.enabled = false;
            flashLight.intensity = originalIntensity;

            // Restore original position
            flashLight.transform.position = originalPos;
            yield break;
        }

        // If no flash light, use Sun intensity
        if (sun != null)
        {
            float org = sun.intensity;
            sun.intensity = sunFlashIntensity;
            yield return new WaitForSeconds(flashDuration);
            sun.intensity = sunRecoverIntensity;
        }
    }

    IEnumerator FlashSkyboxOnce()
    {
        var sky = RenderSettings.skybox;
        if (sky == null) yield break;

        bool hasExposure = sky.HasProperty("_Exposure");
        bool hasTint = sky.HasProperty("_Tint");

        float baseExp = hasExposure ? sky.GetFloat("_Exposure") : 1f;
        Color baseTint = hasTint ? sky.GetColor("_Tint") : Color.white;

        float targetExp = hasExposure ? baseExp * skyboxExposureBoost : baseExp;
        Color targetTint = hasTint ? baseTint * skyboxExposureBoost : baseTint;

        float half = Mathf.Max(0.01f, skyboxFlashDuration * 0.5f);

        // In
        float a = 0f;
        while (a < 1f)
        {
            a += Time.deltaTime / half;
            float k = Mathf.SmoothStep(0f, 1f, a);
            if (hasExposure) sky.SetFloat("_Exposure", Mathf.Lerp(baseExp, targetExp, k));
            if (hasTint) sky.SetColor("_Tint", Color.Lerp(baseTint, targetTint, k));
            yield return null;
        }
        // Out
        a = 0f;
        while (a < 1f)
        {
            a += Time.deltaTime / half;
            float k = Mathf.SmoothStep(0f, 1f, a);
            if (hasExposure) sky.SetFloat("_Exposure", Mathf.Lerp(targetExp, baseExp, k));
            if (hasTint) sky.SetColor("_Tint", Color.Lerp(targetTint, baseTint, k));
            yield return null;
        }
    }

    // Inspector test function
    public void StartLightning()
    {
        isActive = true;
        StopAllCoroutines();
        StartCoroutine(MainLoop());
    }

    public void StopLightning()
    {
        isActive = false;
        StopAllCoroutines();
    }

    [ContextMenu("Test: Flash now")]
    void _TestFlashNow()
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(TestOnce());
    }
    IEnumerator TestOnce()
    {
        yield return StartCoroutine(DoBurst());
        if (thunder != null && thunder.clip != null)
        {
            thunder.volume = thunderVolume;
            thunder.Play();
        }
        // MainLoop 다시 시작하지 않음 - 테스트 한번만
    }
}
