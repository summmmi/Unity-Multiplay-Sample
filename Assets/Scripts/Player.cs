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

    [Header("Mobile Controls")]
    [SerializeField] private bool enableMobileControls = true;

    private MobileInputManager mobileInput;

    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    private bool isMoving = false;

    [SyncVar(hook = nameof(OnAnimationStateChanged))]
    private int currentAnimationState = 0; // 0=idle, 1=walk, 2=meet

    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1.6f, 0); // 머리 높이 (필요시 Inspector에서 조정)

    void Start()
    {
        Debug.Log($"Player Start() - isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}, netId: {netId}");

        SetupCollider();
        SetupPlayerCamera();
        SetupMobileInput();
        SetupAnimation();
    }

    void SetupCollider()
    {
        // 기존 CapsuleCollider가 있는지 확인
        var capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            // 물리 충돌을 위해 isTrigger를 false로 설정
            capsuleCollider.isTrigger = false;
            Debug.Log($"[Player] 기존 CapsuleCollider 사용: radius={capsuleCollider.radius}, height={capsuleCollider.height}, center={capsuleCollider.center}, isTrigger={capsuleCollider.isTrigger}");
        }
        else
        {
            Debug.Log("[Player] CapsuleCollider가 없어서 Inspector 값 확인 불가");
        }
        
        // Rigidbody 추가 및 설정 (물리 적용)
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.Log("[Player] Rigidbody 추가됨");
        }
        
        // 물리 설정
        rb.mass = 1f;
        rb.drag = 5f; // 공기 저항
        rb.angularDrag = 5f;
        rb.freezeRotation = true; // 물리로 인한 회전 방지
        rb.useGravity = true; // 중력 적용
        
        Debug.Log($"[Player] Rigidbody 설정: mass={rb.mass}, drag={rb.drag}, useGravity={rb.useGravity}");
    }

    void SetupMobileInput()
    {
        // WebGL 클라이언트에서만 모바일 입력 활성화
        if (isLocalPlayer && !isServer && enableMobileControls)
        {
            // 약간의 지연 후 모바일 입력 매니저 찾기 (초기화 순서 문제 해결)
            StartCoroutine(SetupMobileInputDelayed());
        }
    }

    System.Collections.IEnumerator SetupMobileInputDelayed()
    {
        yield return new WaitForSeconds(0.5f);

        mobileInput = MobileInputManager.Instance;

        if (mobileInput != null && playerCamera != null)
        {
            mobileInput.SetPlayerCamera(playerCamera);
            Debug.Log($"[Player] 모바일 입력 매니저 연결됨 - WebGL: {Application.platform == RuntimePlatform.WebGLPlayer}");
        }
        else
        {
            Debug.LogWarning($"[Player] 모바일 입력 매니저 연결 실패 - Manager: {mobileInput != null}, Camera: {playerCamera != null}");
        }
    }

    void SetupAnimation()
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<Animator>();
            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>();
            }
        }

        if (characterAnimator != null)
        {
            Debug.Log($"[Player] Animator 연결됨: {characterAnimator.name}");

            // Animator Controller 확인
            if (characterAnimator.runtimeAnimatorController == null)
            {
                Debug.LogError("[Player] Animator Controller가 할당되지 않음! Inspector에서 Player_Animation.controller를 수동 할당해주세요!");
            }
            else
            {
                Debug.Log($"[Player] Animator Controller 할당됨: {characterAnimator.runtimeAnimatorController.name}");
            }
        }
        else
        {
            Debug.LogWarning("[Player] Animator를 찾을 수 없습니다!");
        }
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

                // 카메라를 플레이어 자식으로 설정 (위치는 프리팹 설정 그대로 사용)
                if (playerCamera.transform.parent != transform)
                {
                    playerCamera.transform.SetParent(transform);
                }
                // 위치 강제 설정 제거 - 프리팹의 카메라 위치 그대로 사용

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
            Vector3 moveDirection = Vector3.zero;
            bool hasInput = false;

            // 키보드/게임패드 입력 처리 (PC용 - 월드 기준)
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            if (moveX != 0 || moveZ != 0)
            {
                hasInput = true;
                // PC 키보드는 월드 기준으로 이동 (카메라 회전 무관)
                moveDirection = new Vector3(moveX, 0, moveZ);
            }

            // 모바일 조이스틱 입력 처리 (키보드 입력보다 우선)
            if (enableMobileControls && mobileInput != null)
            {
                Vector2 joystickInput = mobileInput.MovementInput;
                if (joystickInput != Vector2.zero)
                {
                    hasInput = true;

                    if (playerCamera != null)
                    {
                        // 카메라 기준으로 이동 방향 변환 (표준 FPS 방식)
                        Vector3 cameraForward = playerCamera.transform.forward;
                        Vector3 cameraRight = playerCamera.transform.right;

                        // Y축 제거 (수평면에서만 이동)
                        cameraForward.y = 0;
                        cameraRight.y = 0;
                        cameraForward.Normalize();
                        cameraRight.Normalize();

                        // 조이스틱 입력을 카메라 기준으로 변환
                        moveDirection = cameraRight * joystickInput.x + cameraForward * joystickInput.y;

                        // 디버그 로그 (2초마다)
                        if (Time.time % 2f < Time.deltaTime)
                        {
                            Debug.Log($"[Player] 조이스틱: {joystickInput}, 카메라 기준 이동: {moveDirection}");
                        }
                    }
                    else
                    {
                        // 카메라가 없으면 월드 기준으로 폴백
                        moveDirection = new Vector3(joystickInput.x, 0, joystickInput.y);
                    }
                }
            }

            // 카메라 좌우 회전 입력만 확인 (위아래 회전은 제외)
            bool hasHorizontalCameraInput = false;
            if (enableMobileControls && mobileInput != null)
            {
                Vector2 cameraInput = mobileInput.CameraInput;
                // X축 움직임이 Y축 움직임보다 2배 이상 클 때 (주로 좌우 회전)
                hasHorizontalCameraInput = Mathf.Abs(cameraInput.x) > 0.01f &&
                                         Mathf.Abs(cameraInput.x) > Mathf.Abs(cameraInput.y) * 2f;

                // 디버그 로그 (카메라 입력이 있을 때만)
                if (Mathf.Abs(cameraInput.x) > 0.01f || Mathf.Abs(cameraInput.y) > 0.01f)
                {
                    if (Time.time % 1f < Time.deltaTime)
                    {
                        Debug.Log($"[Player] 카메라 입력 - X: {cameraInput.x:F3}, Y: {cameraInput.y:F3}, 좌우회전 인식: {hasHorizontalCameraInput}");
                    }
                }
            }

            // 즉시 반응하는 이동 로직 (이동 또는 카메라 좌우 회전)
            if ((hasInput && moveDirection != Vector3.zero) || hasHorizontalCameraInput)
            {
                // 애니메이션: 이동 시작 (실제 이동이나 카메라 회전 시)
                if (!isMoving)
                {
                    isMoving = true;
                    TriggerMoveAnimation();
                }

                // 실제 이동이 있을 때만 위치 변경
                if (hasInput && moveDirection != Vector3.zero)
                {
                    // Rigidbody 물리 기반 이동
                    var rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        moveDirection.Normalize();
                        Vector3 targetVelocity = moveDirection * moveSpeed;
                        rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);
                    }

                    // 5초마다 위치 로그
                    if (playerCamera != null && Time.time % 5f < Time.deltaTime)
                    {
                        Debug.Log($"[Player] 이동: Player={transform.position}, Camera={playerCamera.transform.position}");
                    }
                }
            }
            else
            {
                // 입력 없으면 즉시 정지
                if (isMoving)
                {
                    isMoving = false;
                    TriggerStopAnimation();
                    
                    // Rigidbody 수평 속도 정지
                    var rb = GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = new Vector3(0, rb.velocity.y, 0);
                    }
                }
            }

            // 카메라가 플레이어를 따라가도록 강제 업데이트
            UpdateCameraPosition();

            // 모바일 카메라 회전 처리
            if (enableMobileControls && mobileInput != null)
            {
                mobileInput.ApplyCameraRotation();
            }
        }
    }

    void UpdateCameraPosition()
    {
        if (playerCamera != null && isLocalPlayer && !isServer)
        {
            // 카메라가 플레이어의 자식인지만 확인 (위치는 건드리지 않음)
            if (playerCamera.transform.parent != transform)
            {
                // 현재 월드 위치 저장
                Vector3 worldPosition = playerCamera.transform.position;
                Quaternion worldRotation = playerCamera.transform.rotation;

                // 부모 설정
                playerCamera.transform.SetParent(transform);

                // 월드 위치/회전 복원 (프리팹의 로컬 위치 유지)
                playerCamera.transform.position = worldPosition;
                playerCamera.transform.rotation = worldRotation;

                Debug.Log($"[Player] 카메라 부모 재설정 - Local: {playerCamera.transform.localPosition}");
            }

            // 모바일에서는 MobileInputManager가 카메라 회전을 처리
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

    void TriggerMoveAnimation()
    {
        if (isLocalPlayer)
        {
            CmdSetAnimationState(1); // walk state
        }
    }

    void TriggerStopAnimation()
    {
        if (isLocalPlayer)
        {
            CmdSetAnimationState(0); // idle state
        }
    }

    void TriggerMeetAnimation()
    {
        if (isLocalPlayer)
        {
            CmdSetAnimationState(2); // meet state
        }
    }

    [Command]
    void CmdSetAnimationState(int newState)
    {
        currentAnimationState = newState;
    }

    void OnAnimationStateChanged(int oldState, int newState)
    {
        if (characterAnimator != null)
        {
            switch (newState)
            {
                case 0: // idle
                    characterAnimator.SetTrigger("stop");
                    Debug.Log("[Player] 네트워크 동기화 애니메이션: stop");
                    break;
                case 1: // walk
                    characterAnimator.SetTrigger("move");
                    Debug.Log("[Player] 네트워크 동기화 애니메이션: move");
                    break;
                case 2: // meet
                    characterAnimator.SetTrigger("meet");
                    Debug.Log("[Player] 네트워크 동기화 애니메이션: meet");
                    break;
            }
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
