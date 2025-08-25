using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class SimpleRainController : MonoBehaviour
{
    [Header("Realistic Rain FX Prefabs (그대로 사용)")]
    public GameObject rainPrefab; // HQRainDistortDetailedTorrential
    public GameObject splashPrefab; // RainDistortFlatRipples  
    public GameObject fogPrefab; // RainWhiteSmoky
    
    [Header("Settings")]
    public bool followPlayer = true;
    public float heightOffset = 15f;
    
    [Header("Fog")]
    public bool enableFog = true;
    public Color fogColor = new Color(0.5f, 0.5f, 0.6f, 1f);
    [Range(0f, 0.05f)]
    public float fogDensity = 0.02f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    [Range(0f, 1f)]
    public float volume = 0.4f;
    
    public GameObject rainInstance; // public으로 변경 (ChangeEnvironment에서 접근용)
    private GameObject splashInstance;
    private GameObject fogInstance;
    private Transform playerTarget;
    
    // 스플래시 풀링
    private Queue<GameObject> splashPool = new Queue<GameObject>();
    private List<GameObject> activeSplashes = new List<GameObject>();
    
    void Start()
    {
        CreateRainEffects();
        SetupFog();
        SetupAudio();
        FindPlayer();
    }
    
    void CreateRainEffects()
    {
        // 비 프리팹 그대로 생성 (설정 건드리지 않음)
        if (rainPrefab != null)
        {
            rainInstance = Instantiate(rainPrefab);
            rainInstance.name = "Rain_Instance";
            
            // 메인 비에 충돌 감지 활성화 (바닥 스플래시용)
            ParticleSystem[] rainParticles = rainInstance.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in rainParticles)
            {
                var collision = ps.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D;
                collision.dampen = 0.8f;
                collision.bounce = 0.1f;
                collision.lifetimeLoss = 0.9f;
                collision.radiusScale = 0.1f;
                
                // 충돌 이벤트 리스너 추가
                ParticleCollisionDetector detector = ps.gameObject.GetComponent<ParticleCollisionDetector>();
                if (detector == null)
                {
                    detector = ps.gameObject.AddComponent<ParticleCollisionDetector>();
                }
                detector.rainController = this;
            }
            
            // 스플래시 풀 생성
            CreateSplashPool();
            
            Debug.Log("[SimpleRainController] 비 프리팹 생성 완료 - 충돌 감지 활성화");
        }
        
        // 스플래시는 비 충돌로 자동 생성되므로 별도 생성 안 함
        // if (splashPrefab != null) - 제거
        
        // 안개 프리팹 생성 (원래대로 복구, 하지만 설정은 건드리지 않음)
        if (fogPrefab != null)
        {
            fogInstance = Instantiate(fogPrefab);
            fogInstance.name = "Fog_Instance";
            Debug.Log("[SimpleRainController] 안개 프리팹 생성 완료 - 원본 설정 유지");
        }
    }
    
    void SetupFog()
    {
        if (enableFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }
    }
    
    void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 사운드가 Inspector에서 할당되지 않았으면 자동 로드 시도 (에디터에서만)
        if (audioSource.clip == null)
        {
#if UNITY_EDITOR
            // 직접 에셋 경로로 로드 시도 (에디터에서만)
            AudioClip rainSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Realistic Rain FX/Sound/rain_loop.wav");
            if (rainSound != null)
            {
                audioSource.clip = rainSound;
                Debug.Log("[SimpleRainController] rain_loop.wav 자동 로드됨");
            }
#else
            // 빌드에서는 Resources 폴더에서 로드하거나 Inspector에서 직접 할당 필요
            AudioClip rainSound = Resources.Load<AudioClip>("rain_loop");
            if (rainSound != null)
            {
                audioSource.clip = rainSound;
                Debug.Log("[SimpleRainController] rain_loop.wav Resources에서 로드됨");
            }
#endif
        }
        
        if (audioSource.clip != null)
        {
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.spatialBlend = 0f;
            audioSource.Play();
            Debug.Log("[SimpleRainController] 비 사운드 재생");
        }
        else
        {
            Debug.LogWarning("[SimpleRainController] Inspector에서 Audio Source에 rain_loop.wav를 직접 할당해주세요");
        }
    }
    
    void FindPlayer()
    {
        // LocalPlayer 찾기
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player player in players)
        {
            if (player.isLocalPlayer)
            {
                playerTarget = player.transform;
                Debug.Log("[SimpleRainController] LocalPlayer 발견");
                return;
            }
        }
        
        // MainCamera 사용
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerTarget = mainCam.transform;
            Debug.Log("[SimpleRainController] MainCamera 사용");
        }
    }
    
    void Update()
    {
        // 플레이어 없으면 찾기 시도
        if (playerTarget == null)
        {
            FindPlayer();
        }
        
        // 비와 스플래시를 플레이어 위치로 이동
        if (followPlayer && playerTarget != null)
        {
            Vector3 targetPos = playerTarget.position;
            
            if (rainInstance != null)
            {
                rainInstance.transform.position = targetPos + Vector3.up * heightOffset;
            }
            
            // 스플래시는 비 충돌로 자동 생성됨
            
            if (fogInstance != null)
            {
                // 안개는 좀 더 낮은 높이에서 퍼지도록
                fogInstance.transform.position = targetPos + Vector3.up * 2f;
            }
        }
        
        // 안개 유지
        if (enableFog && !RenderSettings.fog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogDensity = fogDensity;
        }
    }
    
    // Public Methods
    public void ToggleRain()
    {
        if (rainInstance != null)
        {
            rainInstance.SetActive(!rainInstance.activeInHierarchy);
        }
        
        // 스플래시는 비 충돌로 자동 생성됨
        
        if (fogInstance != null)
        {
            fogInstance.SetActive(!fogInstance.activeInHierarchy);
        }
    }
    
    void CreateSplashPool()
    {
        if (splashPrefab == null) return;
        
        // 스플래시 풀 생성 (10개)
        for (int i = 0; i < 10; i++)
        {
            GameObject splash = Instantiate(splashPrefab);
            splash.name = $"Splash_Pool_{i}";
            splash.SetActive(false);
            splash.transform.SetParent(transform);
            splashPool.Enqueue(splash);
        }
        
        Debug.Log("[SimpleRainController] 스플래시 풀 생성 완료 (10개)");
    }
    
    public void OnParticleCollision(Vector3 collisionPoint)
    {
        // 충돌 지점에 스플래시 생성
        if (splashPool.Count > 0)
        {
            GameObject splash = splashPool.Dequeue();
            splash.transform.position = collisionPoint + Vector3.up * 0.05f;
            splash.SetActive(true);
            activeSplashes.Add(splash);
            
            // 3초 후 풀로 반환 (더 오래 유지)
            StartCoroutine(ReturnSplashToPool(splash, 3f));
        }
    }
    
    System.Collections.IEnumerator ReturnSplashToPool(GameObject splash, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (splash != null)
        {
            splash.SetActive(false);
            activeSplashes.Remove(splash);
            splashPool.Enqueue(splash);
        }
    }
    
    public void SetVolume(float newVolume)
    {
        volume = newVolume;
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }
}