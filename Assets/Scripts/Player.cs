using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Player : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1.6f, 0); // 머리 높이

    void Start()
    {
        Debug.Log($"Player Start() - isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}, netId: {netId}");
        SetupPlayerCamera();
    }

    void SetupPlayerCamera()
    {
        Debug.Log($"[Player] SetupPlayerCamera - isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}");

        // 카메라와 오디오 리스너가 설정되지 않았다면 자동으로 찾기
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
            Debug.Log($"[Player] 카메라 자동 검색 결과: {(playerCamera != null ? "찾음" : "없음")}");
        }

        if (audioListener == null)
        {
            audioListener = GetComponentInChildren<AudioListener>();
            Debug.Log($"[Player] 오디오리스너 자동 검색 결과: {(audioListener != null ? "찾음" : "없음")}");
        }

        // 로컬 플레이어이고 서버가 아닌 경우에만 카메라와 오디오 활성화 (클라이언트 전용)
        if (isLocalPlayer && !isServer)
        {
            Debug.Log("[Player] 클라이언트 로컬 플레이어 - 카메라 활성화");

            if (playerCamera != null)
            {
                playerCamera.enabled = true;

                // 카메라를 머리 위치에 배치
                if (playerCamera.transform.parent != transform)
                {
                    playerCamera.transform.SetParent(transform);
                }
                playerCamera.transform.localPosition = cameraOffset;

                // Post-Processing 설정 확인
                SetupCameraPostProcessing();

                Debug.Log($"[Player] 플레이어 카메라 활성화 - 위치: {cameraOffset}");
            }
            else
            {
                Debug.LogError("[Player] 플레이어 카메라를 찾을 수 없습니다!");
            }

            if (audioListener != null)
            {
                audioListener.enabled = true;
                Debug.Log("[Player] 플레이어 오디오 리스너 활성화");
            }
        }
        else if (isLocalPlayer && isServer)
        {
            Debug.Log("[Player] 호스트 로컬 플레이어 - 오버뷰 카메라 사용으로 인해 플레이어 카메라 비활성화");

            // 호스트에서는 플레이어 카메라와 AudioListener 명시적으로 비활성화
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                Debug.Log("[Player] 호스트: 플레이어 카메라 비활성화");
            }

            if (audioListener != null)
            {
                audioListener.enabled = false;
                Debug.Log("[Player] 호스트: 플레이어 AudioListener 비활성화");
            }
        }
        else
        {
            Debug.Log("[Player] 원격 플레이어 - 카메라 비활성화");

            // 다른 플레이어의 카메라는 비활성화
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
                Debug.Log("[Player] 원격 플레이어 카메라 비활성화");
            }

            if (audioListener != null)
            {
                audioListener.enabled = false;
                Debug.Log("[Player] 원격 플레이어 오디오 리스너 비활성화");
            }
        }
    }

    void PlayerMovement()
    {
        if (isLocalPlayer && !isServer) // 클라이언트 플레이어만 이동
        {
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            if (moveX != 0 || moveZ != 0) // 움직임이 있을 때만 로그
            {
                Vector3 oldPosition = transform.position;
                Vector3 moveDirection = new Vector3(moveX, 0, moveZ);
                transform.position = transform.position + moveDirection * moveSpeed * Time.deltaTime;

                // 카메라 위치 확인
                if (playerCamera != null)
                {
                    Debug.Log($"[Player] 이동: Player={transform.position}, Camera={playerCamera.transform.position}");
                }
            }

            // 카메라가 플레이어를 따라가도록 강제 업데이트
            UpdateCameraPosition();
        }
    }

    void UpdateCameraPosition()
    {
        if (playerCamera != null && isLocalPlayer && !isServer)
        {
            // 카메라가 플레이어의 자식인지 확인하고, 위치를 강제로 업데이트
            if (playerCamera.transform.parent == transform)
            {
                // 이미 올바른 위치에 있는지 확인
                if (Vector3.Distance(playerCamera.transform.localPosition, cameraOffset) > 0.01f)
                {
                    playerCamera.transform.localPosition = cameraOffset;
                    Debug.Log($"[Player] 카메라 위치 강제 업데이트: {cameraOffset}");
                }
            }
            else
            {
                // 부모가 다르면 다시 설정
                playerCamera.transform.SetParent(transform);
                playerCamera.transform.localPosition = cameraOffset;
                Debug.Log($"[Player] 카메라 부모 재설정 및 위치 업데이트: {cameraOffset}");
            }
        }
    }

    void Update()
    {
        PlayerMovement();
        DebugCameraInfo();
    }

    void SetupCameraPostProcessing()
    {
        if (playerCamera == null) return;

        // Universal Additional Camera Data 확인
        var cameraData = playerCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
            Debug.Log("[Player] 카메라 Post-Processing 활성화");
        }
        else
        {
            Debug.LogWarning("[Player] Universal Additional Camera Data가 없습니다. 수동으로 추가해주세요.");
        }

        // Global Volume 확인
        Volume globalVolume = FindObjectOfType<Volume>();
        if (globalVolume != null)
        {
            Debug.Log($"[Player] Global Volume 발견: {globalVolume.name}, Priority: {globalVolume.priority}");

            // ColorAdjustments 확인
            if (globalVolume.profile != null && globalVolume.profile.TryGet<ColorAdjustments>(out ColorAdjustments colorAdj))
            {
                Debug.Log($"[Player] ColorAdjustments 발견 - 현재 색상: {colorAdj.colorFilter.value}");
            }
            else
            {
                Debug.LogWarning("[Player] ColorAdjustments가 Global Volume Profile에 없습니다!");
            }
        }
        else
        {
            Debug.LogError("[Player] Global Volume을 찾을 수 없습니다!");
        }
    }

    void DebugCameraInfo()
    {
        // 5초마다 카메라 정보 출력 (클라이언트 로컬 플레이어만)
        if (isLocalPlayer && !isServer && Time.time % 5f < Time.deltaTime)
        {
            if (playerCamera != null)
            {
                Debug.Log($"[Camera Debug] 활성화: {playerCamera.enabled}, 위치: {playerCamera.transform.position}, 부모: {playerCamera.transform.parent?.name}");
            }
            else
            {
                Debug.LogError("[Camera Debug] 플레이어 카메라가 null입니다!");
            }
        }
    }
}
