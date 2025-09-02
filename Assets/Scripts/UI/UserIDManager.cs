using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class UserIDManager : MonoBehaviour
{
    [Header("Logo UI")]
    [SerializeField] private GameObject logoPanel;
    [SerializeField] private Button startButton;
    
    [Header("Waiting UI")]
    [SerializeField] private GameObject lobbyPanel; // Waiting 화면 패널
    [SerializeField] private Image waitingImage;
    
    [Header("ID Input UI")]
    [SerializeField] private GameObject idInputPanel; // ID 입력 패널
    [SerializeField] private TMP_InputField idInputField;
    [SerializeField] private Button confirmButton;
    
    [Header("Game UI")]
    [SerializeField] private GameObject gameUIPanel; // 조이스틱 등 게임 UI 패널
    
    public static UserIDManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private int minIdLength = 2;
    [SerializeField] private int maxIdLength = 12;
    
    public string UserID { get; private set; } = "";
    public bool IsIDSet { get; private set; } = false;
    
    [Header("UI State")]
    private bool isWaitingForHost = true;
    private bool networkConnectionStarted = false;
    
    public enum UIState
    {
        Logo,
        WaitingForHost, 
        IDInput,
        Game
    }
    
    private UIState currentState = UIState.Logo;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        InitializeUI();
        
        // WebGL Client에서만 동작
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            ShowLogo();
        }
        else
        {
            // PC나 Host에서는 모든 UI 건너뛰기
            SkipToGame();
        }
    }
    
    void InitializeUI()
    {
        // Inspector에서 할당된 UI 요소들 검증
        if (logoPanel == null)
        {
            Debug.LogError("[UserIDManager] Logo Panel이 Inspector에 할당되지 않았습니다!");
            return;
        }
        
        if (startButton == null)
        {
            Debug.LogError("[UserIDManager] Start Button이 Inspector에 할당되지 않았습니다!");
            return;
        }
        
        if (lobbyPanel == null)
        {
            Debug.LogError("[UserIDManager] Lobby Panel이 Inspector에 할당되지 않았습니다!");
            return;
        }
        
        if (idInputPanel == null)
        {
            Debug.LogError("[UserIDManager] ID Input Panel이 Inspector에 할당되지 않았습니다!");
            return;
        }
        
        if (idInputField == null)
        {
            Debug.LogError("[UserIDManager] ID Input Field가 Inspector에 할당되지 않았습니다!");
            return;
        }
        
        if (confirmButton == null)
        {
            Debug.LogError("[UserIDManager] Confirm Button이 Inspector에 할당되지 않았습니다!");
            return;
        }
        
        // 게임 UI 패널 검증
        if (gameUIPanel == null)
        {
            Debug.LogWarning("[UserIDManager] Game UI Panel이 Inspector에 할당되지 않았습니다!");
        }
        
        // 버튼 이벤트 연결
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartButtonClicked);
        
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        
        // InputField 이벤트 연결
        idInputField.onValueChanged.RemoveAllListeners();
        idInputField.onValueChanged.AddListener(OnInputValueChanged);
        idInputField.onSubmit.RemoveAllListeners();
        idInputField.onSubmit.AddListener(OnInputSubmit);
        
        // 초기 Waiting Image 설정 (필요시)
        // waitingImage는 Inspector에서 기본 "Waiting for Host" 이미지로 설정
        
        // 모든 패널 초기 숨김
        HideAllPanels();
        
        Debug.Log("[UserIDManager] UI 이벤트 연결 완료");
    }
    
    void HideAllPanels()
    {
        if (logoPanel != null) logoPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (idInputPanel != null) idInputPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
    }
    
    void ShowLogo()
    {
        HideAllPanels();
        currentState = UIState.Logo;
        
        if (logoPanel != null)
        {
            logoPanel.SetActive(true);
        }
        
        Debug.Log("[UserIDManager] 로고 화면 표시");
    }
    
    public void OnStartButtonClicked()
    {
        Debug.Log("[UserIDManager] 시작 버튼 클릭");
        ShowWaitingForHost();
    }
    
    void ShowWaitingForHost()
    {
        HideAllPanels();
        currentState = UIState.WaitingForHost;
        
        // Waiting 로비 패널만 표시
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
        }
        
        // Host 연결 체크 시작
        StartCoroutine(CheckHostConnection());
        
        Debug.Log("[UserIDManager] Host 대기 화면 표시 - Lobby Panel");
    }
    
    void ShowIDInput()
    {
        HideAllPanels();
        currentState = UIState.IDInput;
        
        // ID 입력 패널만 표시
        if (idInputPanel != null)
        {
            idInputPanel.SetActive(true);
        }
        
        // ID 입력창 활성화
        if (idInputField != null)
        {
            idInputField.Select();
            idInputField.ActivateInputField();
        }
        
        UpdateConfirmButton();
        Debug.Log("[UserIDManager] ID 입력 화면 활성화 - ID Input Panel");
    }
    
    void SkipToGame()
    {
        HideAllPanels();
        currentState = UIState.Game;
        
        // PC나 Host는 기본 ID 설정하고 건너뛰기
        UserID = "Host_User";
        IsIDSet = true;
        isWaitingForHost = false;
        
        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);
            
        Debug.Log("[UserIDManager] 모든 UI 건너뛰기 - Host/PC 모드");
    }
    
    void OnInputValueChanged(string value)
    {
        UpdateConfirmButton();
    }
    
    void OnInputSubmit(string value)
    {
        if (IsValidID(value))
        {
            OnConfirmButtonClicked();
        }
    }
    
    void UpdateConfirmButton()
    {
        if (confirmButton != null && idInputField != null)
        {
            bool isValid = IsValidID(idInputField.text);
            confirmButton.interactable = isValid;
            
            // 버튼 색상 변경 (선택사항)
            ColorBlock colors = confirmButton.colors;
            colors.normalColor = isValid ? Color.white : Color.gray;
            confirmButton.colors = colors;
        }
    }
    
    bool IsValidID(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (id.Length < minIdLength) return false;
        if (id.Length > maxIdLength) return false;
        
        // 기본적인 문자 검사 (영문, 숫자, 한글, 일부 특수문자만)
        foreach (char c in id)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            {
                return false;
            }
        }
        
        return true;
    }
    
    public void OnConfirmButtonClicked()
    {
        if (idInputField == null) return;
        
        string inputID = idInputField.text.Trim();
        
        if (!IsValidID(inputID))
        {
            Debug.LogWarning($"[UserIDManager] 유효하지 않은 ID: {inputID}");
            return;
        }
        
        // ID 설정
        UserID = inputID;
        IsIDSet = true;
        
        Debug.Log($"[UserIDManager] ID 설정 완료: {UserID}");
        
        // 네트워크 연결 시작
        StartNetworkConnection();
    }
    
    
    System.Collections.IEnumerator CheckHostConnection()
    {
        while (isWaitingForHost && Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // AutoNetworkManager 찾기
            AutoNetworkManager autoNetworkManager = FindObjectOfType<AutoNetworkManager>();
            
            if (autoNetworkManager != null)
            {
                // 실제로 연결 시도해서 Host 있는지 확인
                autoNetworkManager.StartClientManually();
                
                // 연결 시도 후 잠시 대기
                yield return new WaitForSeconds(1f);
                
                // 연결되었는지 확인
                if (NetworkClient.isConnected || NetworkClient.active)
                {
                    // 연결 성공하면 바로 끊고 ID 입력 화면으로
                    NetworkClient.Disconnect();
                    
                    isWaitingForHost = false;
                    ShowIDInput();
                    Debug.Log("[UserIDManager] Host 발견! ID 입력 화면으로 전환");
                    break;
                }
                else
                {
                    Debug.Log("[UserIDManager] Host 찾는 중...");
                }
            }
            
            yield return new WaitForSeconds(2f); // 2초마다 재시도
        }
    }
    
    bool CanConnectToHost()
    {
        // AutoNetworkManager를 통해 Host 연결 가능 여부 확인
        AutoNetworkManager autoNetworkManager = FindObjectOfType<AutoNetworkManager>();
        if (autoNetworkManager != null)
        {
            // 실제로 Host가 실행 중인지 테스트 연결 시도
            // localServerAddress에 Host가 있는지 확인
            return TestHostConnection();
        }
        return false;
    }
    
    bool TestHostConnection()
    {
        // 간단한 방법: NetworkManager가 이미 초기화되었고 Host가 실행 가능한 상태인지
        // 실제로는 Host가 이미 실행 중이어야 함
        try
        {
            // 간이 체크: localhost나 설정된 IP에 Host가 있다고 가정
            // WebGL에서는 Host가 다른 기기에서 실행되어야 함
            return true; // Host가 다른 기기에서 실행 중이라고 가정
        }
        catch
        {
            return false;
        }
    }
    
    void StartNetworkConnection()
    {
        if (networkConnectionStarted) return;
        
        networkConnectionStarted = true;
        
        // AutoNetworkManager를 통해 Client 연결 시작
        AutoNetworkManager autoNetworkManager = FindObjectOfType<AutoNetworkManager>();
        if (autoNetworkManager != null)
        {
            autoNetworkManager.StartClientManually();
            Debug.Log("[UserIDManager] 네트워크 클라이언트 연결 시작");
            
            // 연결 완료까지 기다린 후 게임 UI 표시
            StartCoroutine(WaitForNetworkConnection());
        }
        else
        {
            Debug.LogError("[UserIDManager] AutoNetworkManager를 찾을 수 없습니다!");
        }
    }
    
    System.Collections.IEnumerator WaitForNetworkConnection()
    {
        // 네트워크 연결 대기
        while (!NetworkClient.isConnected)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // 연결 완료 후 게임 UI 표시
        HideAllPanels();
        currentState = UIState.Game;
        
        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);
            
        Debug.Log("[UserIDManager] 네트워크 연결 완료 - 게임 UI 표시");
        
        // MobileInputManager에 알림
        NotifyMobileInputManager();
    }
    
    void NotifyMobileInputManager()
    {
        MobileInputManager inputManager = MobileInputManager.Instance;
        if (inputManager != null)
        {
            // MobileInputManager가 이제 동작할 수 있도록 알림
            inputManager.OnUserIDSet();
        }
    }
    
    // 외부에서 현재 사용자 ID 가져오기
    public static string GetUserID()
    {
        if (Instance != null)
        {
            return Instance.UserID;
        }
        return "Unknown_User";
    }
    
    // 외부에서 ID 설정 여부 확인
    public static bool IsUserIDSet()
    {
        if (Instance != null)
        {
            return Instance.IsIDSet;
        }
        return true; // Instance가 없으면 Host라고 가정
    }
}