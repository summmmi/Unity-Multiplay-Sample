using UnityEngine;
using TMPro;
using Mirror;

public class StatusUIManager : MonoBehaviour
{
    [Header("Dynamic UI Elements")]
    [SerializeField] private TextMeshProUGUI meValueText;
    [SerializeField] private TextMeshProUGUI levelValueText;
    [SerializeField] private TextMeshProUGUI totalValueText;
    
    [Header("Static UI Elements (Optional)")]
    [SerializeField] private TextMeshProUGUI meLabelText;
    [SerializeField] private TextMeshProUGUI levelLabelText;
    [SerializeField] private TextMeshProUGUI totalLabelText;
    
    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f; // UI 업데이트 주기
    [SerializeField] private bool showInMeters = true; // true: m, false: km
    
    // 추적 데이터
    private float myDistance = 0f; // 내 이동 거리 (미터)
    private float otherPlayersDistance = 0f; // 다른 플레이어들 거리 (미터)
    private float totalDistance = 0f; // 전체 이동 거리 (미터)
    private int currentWeatherLevel = 0; // 현재 날씨 단계
    
    // 참조
    private Player localPlayer;
    private ChangeEnviroment changeEnvironment;
    private float lastUpdateTime;
    
    // 싱글톤
    public static StatusUIManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeUI();
        FindReferences();
        
        // WebGL에서만 표시
        if (Application.platform != RuntimePlatform.WebGLPlayer && !Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }
    
    void InitializeUI()
    {
        // 정적 라벨 설정 (옵션)
        if (meLabelText != null)
            meLabelText.text = "Me";
        if (levelLabelText != null)
            levelLabelText.text = "Level";
        if (totalLabelText != null)
            totalLabelText.text = "Total";
            
        // 초기 값 표시
        UpdateUI();
        
        Debug.Log("[StatusUIManager] UI 초기화 완료");
    }
    
    void FindReferences()
    {
        // ChangeEnviroment 찾기 (최대 3초간 재시도)
        StartCoroutine(FindChangeEnvironmentWithRetry());
    }
    
    System.Collections.IEnumerator FindChangeEnvironmentWithRetry()
    {
        float timeout = 3f;
        while (changeEnvironment == null && timeout > 0)
        {
            changeEnvironment = FindObjectOfType<ChangeEnviroment>();
            if (changeEnvironment != null)
            {
                Debug.Log("[StatusUIManager] ChangeEnviroment 찾음");
                break;
            }
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }
        
        if (changeEnvironment == null)
        {
            Debug.LogWarning("[StatusUIManager] ChangeEnviroment를 찾을 수 없습니다");
        }
    }
    
    void Update()
    {
        // 업데이트 주기 체크
        if (Time.time - lastUpdateTime < updateInterval)
            return;
            
        lastUpdateTime = Time.time;
        
        // Local Player 찾기 (처음에 못 찾을 수 있으므로 계속 시도)
        if (localPlayer == null)
        {
            FindLocalPlayer();
        }
        
        // 거리 계산 (Player.cs에서 처리하므로 여기서는 총합만 계산)
        CalculateTotalDistance();
        
        // 날씨 레벨 가져오기
        UpdateWeatherLevel();
        
        // UI 업데이트
        UpdateUI();
    }
    
    void FindLocalPlayer()
    {
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player player in players)
        {
            if (player.isLocalPlayer)
            {
                localPlayer = player;
                Debug.Log("[StatusUIManager] Local Player 찾음");
                break;
            }
        }
    }
    
    
    void UpdateWeatherLevel()
    {
        // ChangeEnvironment에서 현재 날씨 단계 가져오기
        if (changeEnvironment != null)
        {
            // GetWeatherStage() 메서드가 public이어야 함
            // 또는 ChangeEnvironment 스크립트에 public 프로퍼티 추가 필요
            currentWeatherLevel = GetCurrentWeatherStage();
        }
    }
    
    int GetCurrentWeatherStage()
    {
        // ChangeEnvironment의 GetWeatherStage() 메서드 호출
        if (changeEnvironment != null)
        {
            return changeEnvironment.GetWeatherStage();
        }
        return 0; // 기본값
    }
    
    void CalculateTotalDistance()
    {
        // 모든 플레이어의 거리 합산
        CalculateAllPlayersDistance();
        totalDistance = myDistance + otherPlayersDistance;
    }
    
    void CalculateAllPlayersDistance()
    {
        float allOthersDistance = 0f;
        
        // 모든 Player 인스턴스 찾기
        Player[] allPlayers = FindObjectsOfType<Player>();
        
        foreach (Player player in allPlayers)
        {
            // 로컬 플레이어가 아닌 다른 플레이어들의 거리 합산
            if (!player.isLocalPlayer)
            {
                allOthersDistance += player.GetTotalDistance();
            }
            else
            {
                // 로컬 플레이어의 경우 myDistance 업데이트
                myDistance = player.GetTotalDistance();
            }
        }
        
        otherPlayersDistance = allOthersDistance;
    }
    
    void UpdateUI()
    {
        // Me (내 이동 거리)
        if (meValueText != null)
        {
            string distanceStr = FormatDistance(myDistance);
            meValueText.text = distanceStr;
        }
        
        // Level (날씨 단계)
        if (levelValueText != null)
        {
            levelValueText.text = currentWeatherLevel.ToString();
        }
        
        // Total (전체 거리)
        if (totalValueText != null)
        {
            string totalStr = FormatDistance(totalDistance);
            totalValueText.text = totalStr;
        }
    }
    
    string FormatDistance(float meters)
    {
        if (showInMeters)
        {
            if (meters < 1000)
            {
                return $"{meters:F0}m";
            }
            else
            {
                float km = meters / 1000f;
                return $"{km:F1}km";
            }
        }
        else
        {
            float km = meters / 1000f;
            return $"{km:F1}km";
        }
    }
    
    // 외부에서 호출 가능한 메서드들
    
    /// <summary>
    /// 내 이동 거리 설정
    /// </summary>
    public void SetMyDistance(float meters)
    {
        myDistance = meters;
        UpdateUI();
    }
    
    /// <summary>
    /// 다른 플레이어들 총 거리 설정
    /// </summary>
    public void SetOtherPlayersDistance(float meters)
    {
        otherPlayersDistance = meters;
        CalculateTotalDistance();
        UpdateUI();
    }
    
    /// <summary>
    /// 다른 플레이어 거리 추가
    /// </summary>
    public void AddOtherPlayerDistance(float meters)
    {
        otherPlayersDistance += meters;
        CalculateTotalDistance();
        UpdateUI();
    }
    
    /// <summary>
    /// 전체 거리 직접 설정 (네트워크 동기화용)
    /// </summary>
    public void SetTotalDistance(float meters)
    {
        totalDistance = meters;
        UpdateUI();
    }
    
    /// <summary>
    /// 날씨 레벨 수동 설정
    /// </summary>
    public void SetWeatherLevel(int level)
    {
        currentWeatherLevel = Mathf.Clamp(level, 0, 3);
        UpdateUI();
    }
    
    /// <summary>
    /// 거리 리셋
    /// </summary>
    public void ResetDistances()
    {
        myDistance = 0f;
        otherPlayersDistance = 0f;
        totalDistance = 0f;
        UpdateUI();
    }
    
    /// <summary>
    /// 네트워크에서 전체 거리 동기화 받기
    /// </summary>
    public void OnNetworkTotalDistanceUpdate(float networkTotalDistance)
    {
        totalDistance = networkTotalDistance;
        UpdateUI();
    }
    
    /// <summary>
    /// 플레이어 거리 업데이트 시 호출 (네트워크 동기화)
    /// </summary>
    public void OnPlayerDistanceUpdated()
    {
        CalculateTotalDistance();
        UpdateUI();
    }
}