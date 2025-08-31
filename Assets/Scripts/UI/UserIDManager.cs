using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserIDManager : MonoBehaviour
{
    [Header("ID Input UI")]
    [SerializeField] private GameObject idInputPanel;
    [SerializeField] private TMP_InputField idInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI titleText;
    
    [Header("Game UI")]
    [SerializeField] private GameObject gameUIPanel; // 조이스틱 등 게임 UI 패널
    
    public static UserIDManager Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private int minIdLength = 2;
    [SerializeField] private int maxIdLength = 12;
    
    public string UserID { get; private set; } = "";
    public bool IsIDSet { get; private set; } = false;
    
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
            ShowIDInput();
        }
        else
        {
            // PC나 Host에서는 ID 입력 건너뛰기
            SkipIDInput();
        }
    }
    
    void InitializeUI()
    {
        // Inspector에서 할당된 UI 요소들 검증
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
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        
        // InputField 이벤트 연결
        idInputField.onValueChanged.RemoveAllListeners();
        idInputField.onValueChanged.AddListener(OnInputValueChanged);
        idInputField.onSubmit.RemoveAllListeners();
        idInputField.onSubmit.AddListener(OnInputSubmit);
        
        // 초기 UI 텍스트 설정 (옵션)
        if (titleText != null)
        {
            titleText.text = "사용자 이름을 입력하세요";
        }
        
        Debug.Log("[UserIDManager] UI 이벤트 연결 완료");
    }
    
    void ShowIDInput()
    {
        // ID 입력 패널 표시
        if (idInputPanel != null)
        {
            idInputPanel.SetActive(true);
        }
        
        // 게임 UI 패널 숨김
        if (gameUIPanel != null)
        {
            gameUIPanel.SetActive(false);
        }
        
        // InputField 포커스
        if (idInputField != null)
        {
            idInputField.Select();
            idInputField.ActivateInputField();
        }
        
        UpdateConfirmButton();
        
        Debug.Log("[UserIDManager] ID 입력 화면 표시");
    }
    
    void SkipIDInput()
    {
        // PC나 Host는 기본 ID 설정하고 건너뛰기
        UserID = "Host_User";
        IsIDSet = true;
        
        if (idInputPanel != null)
            idInputPanel.SetActive(false);
            
        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);
            
        Debug.Log("[UserIDManager] ID 입력 건너뛰기 - Host/PC 모드");
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
        
        // ID 입력 패널 숨기고 게임 UI 표시
        if (idInputPanel != null)
            idInputPanel.SetActive(false);
            
        if (gameUIPanel != null)
            gameUIPanel.SetActive(true);
            
        Debug.Log($"[UserIDManager] ID 설정 완료: {UserID}");
        
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