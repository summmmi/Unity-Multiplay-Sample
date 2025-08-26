using UnityEngine;
using System.Collections;

public class EnvController : MonoBehaviour
{
    [System.Serializable]
    public struct LevelSettings
    {
        [Header("Skybox")]
        public Material skybox;      // ������ �ϴ� ��Ƽ����

        [Header("Sun (Directional Light)")]
        public Color sunColor;     // �¾� ��
        public float sunIntensity; // �¾� ���
        public Vector3 sunEuler;     // �¾� ����(Euler)

        [Header("Fog (RenderSettings)")]
        public bool fogEnabled;   // ���� ���
        public Color fogColor;     // ���� ��
        public float fogDensity;   // ���� �е�(OFF�� �� 0���� ����)

        [Header("Lightning")]
        public bool lightningEnabled; // �� �������� ���� ������Ʈ Ȱ��ȭ ����
    }

    [Header("References")]
    public Light sun;                // ���� Directional Light(Sun)
    public GameObject lightning;     // ���� ��Ʈ�ѷ� ������Ʈ(Lightning)

    [Header("Levels (Size = 3)")]
    public LevelSettings[] levels = new LevelSettings[3]; // 0=Lv1, 1=Lv2, 2=Lv3

    [Header("Transition")]
    public float defaultDuration = 1.5f; // ��ȯ �ð�(��)

    void Start()
    {
        // Sun이 할당되지 않았으면 자동으로 찾기
        if (sun == null)
        {
            sun = FindObjectOfType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                Debug.Log("✅ Sun (Directional Light) auto-assigned");
            }
            else
            {
                Debug.LogError("❌ Directional Light not found! Please assign Sun in Inspector");
                return;
            }
        }
        
        ApplyInstant(0); // ������ ����1 ��� ����
    }

    /// <summary>�ܺο��� 1/2/3 ������ �ٲ� �� ȣ��</summary>
    public void ChangeEnvironmentLevel(int levelIndex01Based, float duration = -1f)
    {
        int idx = Mathf.Clamp(levelIndex01Based - 1, 0, levels.Length - 1);
        if (duration < 0f) duration = defaultDuration;
        StopAllCoroutines();
        StartCoroutine(DoTransition(idx, duration));
    }

    IEnumerator DoTransition(int idx, float dur)
    {
        var t = levels[idx];

        // Skybox�� ��Ÿ�� �ν��Ͻ��� ��ü(���� ���� ��ȣ)
        if (t.skybox) RenderSettings.skybox = new Material(t.skybox);

        // ---- ���۰� ������ ----
        // Sun
        Color sLightColor = sun.color;
        float sIntensity = sun.intensity;
        Vector3 sEuler = sun.transform.eulerAngles;

        // Fog
        RenderSettings.fogMode = FogMode.Exponential;
        bool sEnabled = RenderSettings.fog;
        Color sFogColor = RenderSettings.fogColor;
        float sFogDensity = RenderSettings.fogDensity;

        bool targetEnabled = t.fogEnabled;
        Color targetColor = t.fogColor;
        float targetDensity = targetEnabled ? t.fogDensity : 0f;

        // �����������̸� ���� Fog ON �� 0���� ���� ����
        if (!sEnabled && targetEnabled)
        {
            RenderSettings.fog = true;
            sFogDensity = 0f;
        }

        // ---- ���� ----
        float a = 0f;
        while (a < 1f)
        {
            a += (dur <= 0f ? 1f : Time.deltaTime / dur);

            // Sun
            sun.color = Color.Lerp(sLightColor, t.sunColor, a);
            sun.intensity = Mathf.Lerp(sIntensity, t.sunIntensity, a);
            sun.transform.eulerAngles = Vector3.Lerp(sEuler, t.sunEuler, a);

            // Fog
            RenderSettings.fogColor = Color.Lerp(sFogColor, targetColor, a);
            RenderSettings.fogDensity = Mathf.Lerp(sFogDensity, targetDensity, a);

            yield return null;
        }

        // �����沨���̸� �������� Fog OFF
        if (!targetEnabled)
        {
            RenderSettings.fogDensity = 0f;
            RenderSettings.fog = false;
        }

        // ���� ������Ʈ Ȱ��/��Ȱ��
        if (lightning != null) lightning.SetActive(t.lightningEnabled);
    }

    void ApplyInstant(int idx)
    {
        var t = levels[idx];

        if (t.skybox) RenderSettings.skybox = new Material(t.skybox);

        // Sun ��� ����
        sun.color = t.sunColor;
        sun.intensity = t.sunIntensity;
        sun.transform.eulerAngles = t.sunEuler;

        // Fog ��� ����
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fog = t.fogEnabled;
        RenderSettings.fogColor = t.fogColor;
        RenderSettings.fogDensity = t.fogEnabled ? t.fogDensity : 0f;

        // ���� ������Ʈ Ȱ��/��Ȱ��
        if (lightning != null) lightning.SetActive(t.lightningEnabled);
    }

#if UNITY_EDITOR
    // �����Ϳ����� 1/2/3 Ű�� �׽�Ʈ
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeEnvironmentLevel(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeEnvironmentLevel(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeEnvironmentLevel(3);
    }
#endif
}
