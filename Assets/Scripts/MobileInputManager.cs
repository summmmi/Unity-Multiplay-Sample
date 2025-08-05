using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MobileInputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VirtualJoystick virtualJoystick;
    [SerializeField] private Camera playerCamera;
    
    [Header("Camera Rotation Settings")]
    [SerializeField] private float cameraSensitivity = 5f; // 2에서 5로 증가
    [SerializeField] private float cameraVerticalLimit = 60f;
    [SerializeField] private bool invertY = false;
    
    [Header("Joystick Area")]
    [SerializeField] private float joystickAreaRadius = 150f; // 조이스틱 영역 반지름 (픽셀)
    
    public Vector2 MovementInput { get; private set; }
    public Vector2 CameraInput { get; private set; }
    
    private Vector2 lastTouchPos;
    private bool isCameraRotating = false;
    private float currentCameraRotationX = 0f;
    private Transform cameraTransform;
    
    // 싱글톤 패턴
    public static MobileInputManager Instance { get; private set; }
    
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
        }
    }
    
    private bool hasCheckedNetworkMode = false;
    
    void Start()
    {
        // EventSystem 확인 및 생성
        SetupEventSystem();
        
        // 조이스틱 자동 찾기
        if (virtualJoystick == null)
            virtualJoystick = FindObjectOfType<VirtualJoystick>();
            
        if (playerCamera != null)
            cameraTransform = playerCamera.transform;
            
        Debug.Log($"[MobileInputManager] Start 완료 - 플랫폼: {Application.platform}");
    }
    
    void SetupEventSystem()
    {
        // EventSystem이 없으면 생성
        if (EventSystem.current == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            EventSystem eventSystem = eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            
            Debug.Log("[MobileInputManager] EventSystem 자동 생성됨 - 터치 이벤트 활성화");
        }
        else
        {
            Debug.Log("[MobileInputManager] EventSystem 이미 존재함");
        }
    }
    
    void Update()
    {
        // 네트워크 모드 체크 (한 번만)
        if (!hasCheckedNetworkMode)
        {
            CheckNetworkModeAndHideUI();
        }
        
        // UI가 활성화된 경우에만 입력 처리
        if (gameObject.activeInHierarchy)
        {
            UpdateMovementInput();
            UpdateCameraInput();
        }
    }
    
    void CheckNetworkModeAndHideUI()
    {
        // WebGL Client에서만 UI 표시
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("[MobileInputManager] WebGL Client - UI 표시");
            hasCheckedNetworkMode = true;
            return;
        }
        
        // 나머지는 모두 숨김 (PC Host, PC Client, Editor)
        HideUI("PC 또는 Host 모드");
        hasCheckedNetworkMode = true;
    }
    
    void HideUI(string reason)
    {
        // Canvas 전체 비활성화
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.SetActive(false);
            Debug.Log($"[MobileInputManager] {reason} - Canvas 전체 비활성화");
        }
        
        // 이 GameObject도 비활성화
        gameObject.SetActive(false);
        Debug.Log($"[MobileInputManager] {reason} - 모바일 UI 비활성화");
        
        hasCheckedNetworkMode = true;
    }
    
    void UpdateMovementInput()
    {
        if (virtualJoystick != null)
        {
            MovementInput = virtualJoystick.InputDirection;
        }
        else
        {
            MovementInput = Vector2.zero;
        }
    }
    
    void UpdateCameraInput()
    {
        CameraInput = Vector2.zero;
        
        // 터치 입력 처리
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                
                if (IsTouchInJoystickArea(touch.position))
                    continue; // 조이스틱 영역은 무시
                
                HandleCameraTouch(touch);
                break; // 첫 번째 유효한 터치만 처리
            }
        }
        else
        {
            isCameraRotating = false;
        }
        
        // 에디터에서 마우스로 테스트
        if (Application.isEditor && !Application.isMobilePlatform)
        {
            HandleMouseInput();
        }
    }
    
    void HandleCameraTouch(Touch touch)
    {
        switch (touch.phase)
        {
            case TouchPhase.Began:
                lastTouchPos = touch.position;
                isCameraRotating = true;
                break;
                
            case TouchPhase.Moved:
                if (isCameraRotating)
                {
                    Vector2 deltaPos = touch.position - lastTouchPos;
                    
                    // 스크린 크기에 비례한 정규화 (더 민감하게)
                    float normalizedX = deltaPos.x / Screen.width * 3f; // 3배 더 민감
                    float normalizedY = deltaPos.y / Screen.height * 3f; // 3배 더 민감
                    
                    CameraInput = new Vector2(normalizedX, normalizedY) * cameraSensitivity;
                    lastTouchPos = touch.position;
                }
                break;
                
            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                isCameraRotating = false;
                break;
        }
    }
    
    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            if (!IsTouchInJoystickArea(mousePos))
            {
                lastTouchPos = mousePos;
                isCameraRotating = true;
            }
        }
        else if (Input.GetMouseButton(0) && isCameraRotating)
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 deltaPos = currentMousePos - lastTouchPos;
            
            float normalizedX = deltaPos.x / Screen.width * 3f; // 3배 더 민감
            float normalizedY = deltaPos.y / Screen.height * 3f; // 3배 더 민감
            
            CameraInput = new Vector2(normalizedX, normalizedY) * cameraSensitivity;
            lastTouchPos = currentMousePos;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isCameraRotating = false;
        }
    }
    
    bool IsTouchInJoystickArea(Vector2 touchPosition)
    {
        if (virtualJoystick == null) return false;
        
        // RectTransform으로 조이스틱 위치 가져오기
        RectTransform joystickRect = virtualJoystick.GetComponent<RectTransform>();
        if (joystickRect == null) return false;
        
        // UI 좌표를 스크린 좌표로 변환
        Canvas canvas = virtualJoystick.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
        Vector2 joystickScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, joystickRect.position);
            
        float distance = Vector2.Distance(touchPosition, joystickScreenPos);
        bool inArea = distance <= joystickAreaRadius;
        
        // 디버그 로그
        if (Application.platform == RuntimePlatform.WebGLPlayer && Time.time % 3f < Time.deltaTime)
        {
            Debug.Log($"[MobileInput] Touch: {touchPosition}, Joystick: {joystickScreenPos}, Distance: {distance}, InArea: {inArea}");
        }
        
        return inArea;
    }
    
    // 카메라 회전 적용 (Player 스크립트에서 호출)
    public void ApplyCameraRotation()
    {
        if (cameraTransform == null || CameraInput == Vector2.zero) return;
        
        // 감도 조정 (WebGL에서 더 민감하게)
        float sensitivity = Application.platform == RuntimePlatform.WebGLPlayer ? 200f : 100f;
        
        // 수평 회전 (플레이어 Y축 회전)
        float mouseX = CameraInput.x * sensitivity * Time.deltaTime;
        if (cameraTransform.parent != null)
        {
            cameraTransform.parent.Rotate(Vector3.up * mouseX);
        }
        
        // 수직 회전 (카메라 X축 회전)
        float mouseY = CameraInput.y * sensitivity * Time.deltaTime * (invertY ? 1f : -1f);
        currentCameraRotationX -= mouseY;
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -cameraVerticalLimit, cameraVerticalLimit);
        
        cameraTransform.localRotation = Quaternion.Euler(currentCameraRotationX, 0f, 0f);
        
        // 디버그 로그
        if (Application.platform == RuntimePlatform.WebGLPlayer && (Mathf.Abs(CameraInput.x) > 0.01f || Mathf.Abs(CameraInput.y) > 0.01f))
        {
            if (Time.time % 2f < Time.deltaTime)
            {
                Debug.Log($"[MobileInput] Camera Rotation - Input: {CameraInput}, X: {currentCameraRotationX}");
            }
        }
    }
    
    // 외부에서 카메라 참조 설정
    public void SetPlayerCamera(Camera camera)
    {
        playerCamera = camera;
        cameraTransform = camera?.transform;
    }
    
    // 조이스틱 참조 설정
    public void SetVirtualJoystick(VirtualJoystick joystick)
    {
        virtualJoystick = joystick;
    }
}