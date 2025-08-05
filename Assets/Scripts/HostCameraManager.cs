using UnityEngine;
using Mirror;

public class HostCameraManager : NetworkBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera;
    public Vector3 overviewPosition = new Vector3(0, 5, -10);
    public Vector3 overviewRotation = new Vector3(30, 0, 0);
    public float overviewFOV = 60f;

    [Header("Optional Settings")]
    public bool enableAudioListener = true;

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

        // 서버(호스트)에서만 오버뷰 카메라로 설정
        if (isServer)
        {
            SetupHostOverviewCamera();
            DisablePlayerCameras(); // 호스트에서 플레이어 카메라들 비활성화
            Debug.Log("호스트 오버뷰 카메라 설정 완료");
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

        // 카메라 위치와 회전 설정 (전체 환경을 내려다보는 뷰)
        mainCamera.transform.position = overviewPosition;
        mainCamera.transform.rotation = Quaternion.Euler(overviewRotation);

        // FOV 설정 (넓은 시야각으로)
        mainCamera.fieldOfView = overviewFOV;

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

        Debug.Log($"호스트 오버뷰 카메라 설정:");
        Debug.Log($"- 위치: {overviewPosition}");
        Debug.Log($"- 회전: {overviewRotation}");
        Debug.Log($"- FOV: {overviewFOV}");
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

    // Inspector에서 실시간으로 값 변경 테스트용
    protected override void OnValidate()
    {
        base.OnValidate(); // 부모 클래스의 OnValidate 호출

        // Editor에서 안전하게 체크
        if (Application.isPlaying && mainCamera != null)
        {
            // NetworkBehaviour가 초기화되었는지 확인
            try
            {
                if (isServer)
                {
                    SetupHostOverviewCamera();
                }
            }
            catch (System.NullReferenceException)
            {
                // NetworkBehaviour가 아직 초기화되지 않았음 - 무시
            }
        }
    }
}