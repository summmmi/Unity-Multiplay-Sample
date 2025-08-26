using UnityEngine;
using System.Collections;

public class LightningFlash : MonoBehaviour
{
    [Header("References")]
    public Light sun;             // Sun(Directional Light)
    public Light flashLight;      // 번개 전용 라이트(= LightningLight, 기본 OFF)
    public AudioSource thunder;   // 번개 소리 Audio Source(클립 할당)

    [Header("Timing (레벨3 권장: 8~14초)")]
    public float minInterval = 8f;
    public float maxInterval = 14f;

    [Header("Burst (연속 번쩍)")]
    public int minFlashesPerBurst = 1;   // 1~2 권장
    public int maxFlashesPerBurst = 2;
    public float interFlashGap = 0.12f; // 연속 번쩍 사이 간격(초)

    [Header("Flash (Light) - 바닥/오브젝트 번쩍")]
    public float flashDuration = 0.12f;    // 라이트 깜빡 시간(0.10~0.16)
    public float sunFlashIntensity = 2.0f; // flashLight 없을 때 Sun 임시 세기
    public float sunRecoverIntensity = 0.35f; // 레벨3 Sun 기본 세기와 맞춤

    [Header("Flash (Skybox) - 하늘 번쩍")]
    public bool flashSkybox = true;           // 하늘 번쩍 ON/OFF
    public float skyboxExposureBoost = 1.50f;  // 세기(1.35~1.55)
    public float skyboxFlashDuration = 0.18f;  // 지속(0.12~0.20)

    [Header("Audio")]
    [Range(0f, 1.5f)] public float thunderVolume = 1.0f;  // 인스펙터에서 볼륨 조절

    void OnEnable() { StartCoroutine(MainLoop()); }
    void OnDisable() { StopAllCoroutines(); }

    IEnumerator MainLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            yield return StartCoroutine(DoBurst());   // 연속 번쩍

            if (thunder != null)
            {
                thunder.volume = thunderVolume;
                thunder.PlayDelayed(Random.Range(0.2f, 0.5f)); // 살짝 지연
            }
        }
    }

    IEnumerator DoBurst()
    {
        int count = Random.Range(minFlashesPerBurst, maxFlashesPerBurst + 1);
        for (int i = 0; i < count; i++)
        {
            // 한 번 번쩍(라이트 + 스카이박스)
            Coroutine lc = StartCoroutine(FlashLightOnce());
            Coroutine sc = flashSkybox ? StartCoroutine(FlashSkyboxOnce()) : null;
            if (lc != null) yield return lc;
            if (sc != null) yield return sc;

            // 다음 번쩍까지 잠깐 쉼 (마지막은 쉼 없음)
            if (i < count - 1)
                yield return new WaitForSeconds(interFlashGap);
        }
    }

    IEnumerator FlashLightOnce()
    {
        if (flashLight != null)
        {
            // 전용 라이트를 잠깐 켰다가 끔
            flashLight.enabled = true;
            yield return new WaitForSeconds(flashDuration);
            flashLight.enabled = false;
            yield break;
        }

        // 전용 라이트 없으면 Sun 강도 깜빡(폴백)
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

    // 에디터에서 즉시 테스트
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
        StartCoroutine(MainLoop());
    }
}
