using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [SerializeField] private RectTransform joystickBackground;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private float joystickRange = 50f;
    [SerializeField] private bool hideOnRelease = false;
    
    [Header("Visual Settings")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float alphaInactive = 0.3f;
    [SerializeField] private float alphaActive = 0.7f;
    
    public Vector2 InputDirection { get; private set; }
    public bool IsPressed { get; private set; }
    
    private Vector2 joystickCenter;
    private Camera uiCamera;
    
    void Start()
    {
        // UI 카메라 찾기
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = canvas.worldCamera;
        }
        
        // 초기 설정
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        if (canvasGroup != null)
            canvasGroup.alpha = alphaInactive;
        
        // 조이스틱 참조 확인 및 경고
        if (joystickBackground == null)
        {
            Debug.LogError("[VirtualJoystick] Joystick Background가 할당되지 않았습니다!");
        }
        
        if (joystickHandle == null)
        {
            Debug.LogError("[VirtualJoystick] Joystick Handle이 할당되지 않았습니다!");
        }
            
        ResetJoystick();
        
        Debug.Log("[VirtualJoystick] 초기화 완료 - WebGL: " + (Application.platform == RuntimePlatform.WebGLPlayer));
    }
    
    public void OnPointerDown(PointerEventData eventData)
    {
        IsPressed = true;
        
        if (canvasGroup != null)
            canvasGroup.alpha = alphaActive;
            
        // 조이스틱 중심점 설정
        joystickCenter = joystickBackground.position;
        
        Debug.Log($"[VirtualJoystick] Pointer Down - Center: {joystickCenter}");
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!IsPressed || joystickBackground == null || joystickHandle == null) return;
        
        Vector2 direction = eventData.position - joystickCenter;
        
        // 조이스틱 범위 제한
        if (direction.magnitude > joystickRange)
        {
            direction = direction.normalized * joystickRange;
        }
        
        // 핸들 위치 업데이트
        joystickHandle.position = joystickCenter + direction;
        
        // 입력 방향 계산 (-1 ~ 1 범위)
        InputDirection = direction / joystickRange;
        
        // 디버그 로그 (항상)
        if (Time.time % 1f < Time.deltaTime)
        {
            Debug.Log($"[VirtualJoystick] Input: {InputDirection}, Direction: {direction}, Magnitude: {InputDirection.magnitude}");
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        IsPressed = false;
        ResetJoystick();
        
        if (canvasGroup != null)
            canvasGroup.alpha = hideOnRelease ? 0f : alphaInactive;
    }
    
    void ResetJoystick()
    {
        InputDirection = Vector2.zero;
        if (joystickHandle != null && joystickBackground != null)
        {
            joystickHandle.position = joystickBackground.position;
        }
    }
    
    // 외부에서 조이스틱 위치 설정 (동적 조이스틱용)
    public void SetJoystickPosition(Vector2 position)
    {
        if (joystickBackground != null)
        {
            joystickBackground.position = position;
            joystickCenter = position;
            if (joystickHandle != null)
                joystickHandle.position = position;
        }
    }
}