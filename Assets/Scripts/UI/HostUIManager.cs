using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Linq;

public class HostUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject hostUICanvas;
    [SerializeField] private TextMeshProUGUI buttonCountText;
    [SerializeField] private TextMeshProUGUI environmentLevelText;
    [SerializeField] private Transform userListContainer;
    [SerializeField] private GameObject userListItemPrefab;
    [SerializeField] private TextMeshProUGUI totalDistanceText;

    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.5f;

    // 참조
    private ChangeEnviroment changeEnvironment;
    private float lastUpdateTime;
    private Dictionary<uint, GameObject> userListItems = new Dictionary<uint, GameObject>();

    // 싱글톤
    public static HostUIManager Instance { get; private set; }

    void Awake()
    {
        Debug.Log("[HostUIManager] Awake() called");
        
        // WebGL이면 Host UI Canvas 즉시 비활성화
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("[HostUIManager] WebGL에서 Host UI Canvas 비활성화");
            if (hostUICanvas != null)
                hostUICanvas.SetActive(false);
            return;
        }
        
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[HostUIManager] Instance set");
        }
        else
        {
            Debug.Log("[HostUIManager] Duplicate instance, destroying");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log($"[HostUIManager] Start() called, NetworkServer.active: {NetworkServer.active}");

        // WebGL이거나 호스트가 아니면 즉시 비활성화
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("[HostUIManager] WebGL 클라이언트 - Host UI 비활성화");
            gameObject.SetActive(false);
            return;
        }

        // NetworkServer가 아직 시작되지 않았을 수 있으므로 코루틴으로 확인
        StartCoroutine(CheckNetworkServerAndInitialize());
    }

    System.Collections.IEnumerator CheckNetworkServerAndInitialize()
    {
        // WebGL 클라이언트면 바로 비활성화
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("[HostUIManager] WebGL 클라이언트 - Host UI 비활성화");
            gameObject.SetActive(false);
            yield break;
        }
        
        // NetworkServer가 활성화될 때까지 대기 (최대 5초)
        float timeout = 5f;
        while (!NetworkServer.active && timeout > 0)
        {
            yield return new WaitForSeconds(0.1f);
            timeout -= 0.1f;
        }

        Debug.Log($"[HostUIManager] After waiting, NetworkServer.active: {NetworkServer.active}");

        // Host가 아니면 비활성화
        if (!NetworkServer.active)
        {
            Debug.Log("[HostUIManager] Host가 아니므로 Host UI Canvas 비활성화");
            if (hostUICanvas != null)
                hostUICanvas.SetActive(false);
            yield break;
        }

        FindReferences();
        InitializeUI();

        Debug.Log("[HostUIManager] Host UI 초기화 완료");
    }

    void FindReferences()
    {
        // ChangeEnviroment 찾기 (최대 5초간 재시도)
        StartCoroutine(FindChangeEnvironmentWithRetry());
    }
    
    System.Collections.IEnumerator FindChangeEnvironmentWithRetry()
    {
        float timeout = 10f;
        while (changeEnvironment == null && timeout > 0)
        {
            changeEnvironment = FindObjectOfType<ChangeEnviroment>();
            if (changeEnvironment != null)
            {
                Debug.Log("[HostUIManager] ChangeEnviroment 찾음");
                break;
            }
            yield return new WaitForSeconds(1f);
            timeout -= 1f;
            Debug.Log($"[HostUIManager] ChangeEnviroment 찾는 중... 남은 시간: {timeout}초");
        }
        
        if (changeEnvironment == null)
        {
            Debug.LogError("[HostUIManager] ChangeEnviroment를 찾을 수 없습니다!");
        }
    }

    void InitializeUI()
    {
        // 초기 UI 업데이트
        UpdateButtonCount();
        UpdateEnvironmentLevel();
        UpdateUserList();
        UpdateTotalDistance();
    }

    void Update()
    {
        // Host가 아니면 리턴
        if (!NetworkServer.active) return;

        // 업데이트 주기 체크
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        // UI 업데이트
        UpdateButtonCount();
        UpdateEnvironmentLevel();
        UpdateUserList();
        UpdateTotalDistance();
    }

    void UpdateButtonCount()
    {
        if (changeEnvironment != null && buttonCountText != null)
        {
            int buttonCount = changeEnvironment.GetButtonPressCount();
            buttonCountText.text = $"{buttonCount}";
            Debug.Log($"[HostUIManager] Button count updated: {buttonCount}");
        }
        else
        {
            if (changeEnvironment == null)
                Debug.LogWarning("[HostUIManager] changeEnvironment is null!");
            if (buttonCountText == null)
                Debug.LogWarning("[HostUIManager] buttonCountText is null!");
        }
    }

    void UpdateEnvironmentLevel()
    {
        if (changeEnvironment != null && environmentLevelText != null)
        {
            int level = changeEnvironment.GetWeatherStage();
            environmentLevelText.text = $"{level}";
        }
    }

    string GetLevelName(int level)
    {
        switch (level)
        {
            case 0: return "Sunny";
            case 1: return "Cloudy";
            case 2: return "Rainy";
            case 3: return "Stormy";
            default: return "Unknown";
        }
    }

    private GameObject waitingMessageItem;
    
    void UpdateUserList()
    {
        if (userListContainer == null || userListItemPrefab == null) return;

        // 모든 Player 찾기
        Player[] allPlayers = FindObjectsOfType<Player>();
        
        // 클라이언트 플레이어만 필터링 (Host 제외) - 모든 연결된 플레이어 포함
        Player[] clientPlayers = allPlayers.Where(p => p.isClient && (!p.isServer || !p.isLocalPlayer)).ToArray();
        
        Debug.Log($"[HostUIManager] 전체 플레이어: {allPlayers.Length}, 클라이언트: {clientPlayers.Length}");
        
        if (clientPlayers.Length == 0)
        {
            // 유저가 없으면 "Waiting for users..." 메시지 표시
            ShowWaitingMessage();
            // Waiting 메시지만 있을 때는 기존 유저 아이템들은 그대로 두고 return 안 함
        }
        else
        {
            // 유저가 있으면 Waiting 메시지 숨기기
            HideWaitingMessage();
        }

        // 현재 연결된 플레이어 NetId 목록
        HashSet<uint> currentPlayerIds = new HashSet<uint>();

        foreach (Player player in clientPlayers)
        {
            uint netId = player.netId;
            currentPlayerIds.Add(netId);

            // 새로운 플레이어면 리스트 아이템 생성
            if (!userListItems.ContainsKey(netId))
            {
                GameObject listItem = Instantiate(userListItemPrefab, userListContainer);
                userListItems[netId] = listItem;
            }

            // 리스트 아이템 업데이트
            if (userListItems.TryGetValue(netId, out GameObject item))
            {
                UpdateUserListItem(item, player);
            }
        }

        // 연결 해제된 플레이어 제거
        List<uint> toRemove = new List<uint>();
        foreach (var kvp in userListItems)
        {
            if (!currentPlayerIds.Contains(kvp.Key))
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (uint id in toRemove)
        {
            userListItems.Remove(id);
        }
    }
    
    void ShowWaitingMessage()
    {
        if (waitingMessageItem == null)
        {
            // Waiting 메시지 아이템 생성
            waitingMessageItem = Instantiate(userListItemPrefab, userListContainer);
            
            // 메시지 텍스트 설정
            TextMeshProUGUI[] texts = waitingMessageItem.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length >= 1)
            {
                texts[0].text = "Waiting for users...";
                texts[0].alignment = TextAlignmentOptions.Center;
                texts[0].color = Color.white;
                texts[0].enableWordWrapping = false;
                texts[0].overflowMode = TextOverflowModes.Overflow;
                texts[0].enableAutoSizing = false;
                
                // 두 번째 텍스트가 있으면 숨기기
                if (texts.Length >= 2)
                {
                    texts[1].text = "";
                }
            }
            
            // 아바타 이미지가 있으면 숨기기
            UnityEngine.UI.Image avatarImage = waitingMessageItem.GetComponentInChildren<UnityEngine.UI.Image>();
            if (avatarImage != null)
            {
                avatarImage.enabled = false;
            }
        }
    }
    
    void HideWaitingMessage()
    {
        if (waitingMessageItem != null)
        {
            Destroy(waitingMessageItem);
            waitingMessageItem = null;
        }
    }

    void UpdateUserListItem(GameObject item, Player player)
    {
        // 유저 이름과 거리를 분리된 텍스트로 표시
        TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
        
        string playerID = player.GetPlayerID();
        float distance = player.GetTotalDistance();

        // ID가 비어있으면 표시하지 않음
        if (string.IsNullOrEmpty(playerID))
        {
            return; // 아이템 자체를 업데이트하지 않음
        }

        if (texts.Length >= 2)
        {
            // 첫 번째 텍스트: 플레이어 이름 (왼쪽 정렬)
            texts[0].text = playerID;
            texts[0].alignment = TextAlignmentOptions.Left;
            
            // 두 번째 텍스트: 거리 (오른쪽 정렬, km 단위)
            texts[1].text = $"{(distance / 1000f):N0} km";
            texts[1].alignment = TextAlignmentOptions.Right;
        }
        else if (texts.Length >= 1)
        {
            // 하나의 텍스트만 있으면 기존 방식
            texts[0].text = $"{playerID} | {(distance / 1000f):F3} km";
        }
    }
    
    void UpdateTotalDistance()
    {
        if (totalDistanceText != null)
        {
            float totalDistance = 0f;
            Player[] allPlayers = FindObjectsOfType<Player>();
            
            foreach (Player player in allPlayers)
            {
                float playerDistance = player.GetTotalDistance();
                totalDistance += playerDistance;
                Debug.Log($"[HostUIManager] 플레이어 {player.netId} 거리: {playerDistance}");
            }
            
            // km 값만 표시 (소수점 쉼표 포함)
            totalDistanceText.text = $"{(totalDistance / 1000f):N0} km";
            Debug.Log($"[HostUIManager] 총 거리: {totalDistance}m ({totalDistance / 1000f:F1}km)");
        }
    }

    // 외부에서 버튼 카운트 변경 알림
    public void OnButtonCountChanged(int newCount)
    {
        UpdateButtonCount();
        UpdateEnvironmentLevel();
    }

    // 외부에서 유저 목록 변경 알림
    public void OnUserListChanged()
    {
        UpdateUserList();
    }
}