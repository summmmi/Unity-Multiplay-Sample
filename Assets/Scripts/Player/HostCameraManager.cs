using UnityEngine;
using Mirror;

public class HostCameraManager : NetworkBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera;
    
    [Header("Optional Settings")]
    public bool enableAudioListener = true;
    
    // MainCamera에서 가져올 값들 (Inspector에 표시용)
    [Header("Current MainCamera Values (Read Only)")]
    [SerializeField] private Vector3 currentPosition;
    [SerializeField] private Vector3 currentRotation;
    [SerializeField] private float currentFOV;

    void Start()
    {
        SetupCamera();
    }

    void SetupCamera()
    {
        // 메인 카메라를 찾지 못했다면 자동으로 찾기
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
            }
        }

        if (mainCamera == null)
        {
            Debug.LogError("HostCameraManager: Main Camera를 찾을 수 없습니다!");
            return;
        }

        // MainCamera의 현재 값들을 저장 (Inspector 표시용)
        currentPosition = mainCamera.transform.position;
        currentRotation = mainCamera.transform.eulerAngles;
        currentFOV = mainCamera.fieldOfView;

        Debug.Log($"[HostCameraManager] MainCamera 현재 값들:");
        Debug.Log($"- 위치: {currentPosition}");
        Debug.Log($"- 회전: {currentRotation}");
        Debug.Log($"- FOV: {currentFOV}");

        // 서버(호스트)에서만 오버뷰 카메라로 설정
        if (isServer)
        {
            SetupHostOverviewCamera();
            DisablePlayerCameras(); // 호스트에서 플레이어 카메라들 비활성화
            Debug.Log("호스트 오버뷰 카메라 설정 완료 - MainCamera 값 사용");
        }
        else
        {
            // 클라이언트에서는 메인 카메라 비활성화 (플레이어 카메라를 사용할 예정)
            if (mainCamera != null)
            {
                mainCamera.gameObject.SetActive(false);
                Debug.Log("클라이언트: 메인 카메라 비활성화");
            }
        }
    }

    void SetupHostOverviewCamera()
    {
        if (mainCamera == null) return;

        // MainCamera의 현재 Transform과 FOV 값을 그대로 사용 (변경하지 않음)
        // 위치, 회전, FOV는 Scene에서 설정된 MainCamera 값 그대로 유지

        // AudioListener 설정 (호스트에서만 활성화)
        AudioListener audioListener = mainCamera.GetComponent<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = enableAudioListener;
            Debug.Log($"[HostCameraManager] Main Camera AudioListener: {(enableAudioListener ? "활성화" : "비활성화")}");
        }

        // 다른 모든 AudioListener 비활성화
        AudioListener[] allAudioListeners = FindObjectsOfType<AudioListener>();
        foreach (AudioListener listener in allAudioListeners)
        {
            if (listener != audioListener && listener.enabled)
            {
                listener.enabled = false;
                Debug.Log($"[HostCameraManager] 다른 AudioListener 비활성화: {listener.gameObject.name}");
            }
        }

        // 카메라가 활성화되도록 확실히 설정
        mainCamera.gameObject.SetActive(true);

        Debug.Log($"호스트 오버뷰 카메라 설정 (MainCamera 원본 값 사용):");
        Debug.Log($"- 위치: {currentPosition}");
        Debug.Log($"- 회전: {currentRotation}");
        Debug.Log($"- FOV: {currentFOV}");
    }

    void DisablePlayerCameras()
    {
        // 호스트에서 모든 플레이어 카메라 비활성화
        Player[] players = FindObjectsOfType<Player>();
        foreach (Player player in players)
        {
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null && playerCamera != mainCamera)
            {
                playerCamera.enabled = false;

                AudioListener audioListener = playerCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.enabled = false;
                }

                Debug.Log($"[HostCameraManager] 플레이어 카메라 비활성화: {playerCamera.name}");
            }
        }
    }

    // Inspector에서 MainCamera 값들 표시 업데이트
    protected override void OnValidate()
    {
        base.OnValidate(); // 부모 클래스의 OnValidate 호출

        // Editor에서 MainCamera 값들 업데이트 (표시용)
        if (mainCamera != null)
        {
            currentPosition = mainCamera.transform.position;
            currentRotation = mainCamera.transform.eulerAngles;
            currentFOV = mainCamera.fieldOfView;
        }
    }
}