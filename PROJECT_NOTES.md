# Unity Multiplay Sample Project - 중요 사항

## 네트워크 아키텍처

### Mirror Networking 구조
- **NetworkManager**: 네트워크 연결 관리 (호스트/클라이언트)
- **Player Prefab**: 네트워크로 스폰되는 플레이어 객체
- **isLocalPlayer**: 로컬 플레이어 구분 (클라이언트에서만 true)
- **isServer**: 서버(호스트) 구분

## 중요 제약사항

### 1. NetworkBehaviour 제약
**절대 새로운 NetworkBehaviour를 추가하지 마세요!**
- 기존 Player.cs 스크립트만 사용
- 새 NetworkBehaviour 추가 시 Mirror 동기화 문제 발생
- 모든 네트워크 기능은 Player.cs 내에서 처리

### 2. 네트워크 애니메이션
```csharp
// SyncVar를 통한 애니메이션 상태 동기화
[SyncVar(hook = nameof(OnAnimationStateChanged))]
private int currentAnimationState = 0; // 0=idle, 1=walk, 2=meet

// Command를 통한 서버 상태 업데이트
[Command]
void CmdSetAnimationState(int newState)
{
    currentAnimationState = newState;
}

// Hook 메서드로 모든 클라이언트에서 애니메이션 재생
void OnAnimationStateChanged(int oldState, int newState)
{
    characterAnimator.SetTrigger(...);
}
```

### 3. 카메라 설정
- **호스트**: Overview Camera 사용 (플레이어 카메라 비활성화)
- **클라이언트**: 플레이어 개별 카메라 사용
- **중요**: `isLocalPlayer && !isServer` 조건으로 클라이언트만 구분

## WebGL 빌드 이슈

### Terrain Detail Mesh 렌더링 문제
- **문제**: BushDry, Heather, Plant 등 terrain detail mesh가 WebGL에서 안 보임
- **원인**: URP Terrain/Lit 셰이더가 WebGL의 MAX_TEXTURE_IMAGE_UNITS(16) 한계 초과
- **시도한 해결책**:
  - GPU Instancing 비활성화
  - Alpha Clip Threshold 조정 (0.5)
  - Shader keywords 수정
  - Dynamic Batching 활성화
  - Code Stripping 비활성화
- **현재 상태**: GrassDry만 정상 작동, 나머지는 여전히 안 보임 (Unity 버그로 추정)

### URP 설정
- **파일**: `Assets/Settings/URP-Performant.asset`, `URP-Balanced.asset`
- WebGL용 설정:
  ```yaml
  m_UseSRPBatcher: 0
  m_SupportsDynamicBatching: 1
  ```

## 프로젝트 구조

### 주요 스크립트
- `Assets/Scripts/Player.cs`: 플레이어 이동, 애니메이션, 네트워크 동기화
- `Assets/Scripts/NetworkManagerScript.cs`: 네트워크 연결 관리
- `Assets/Scripts/MobileInputManager.cs`: 모바일 조이스틱 입력
- `Assets/Scripts/UI/`: UI 관련 스크립트들

### 프리팹
- `Assets/Prefebs/Player.prefab`: 네트워크 플레이어 프리팹
  - NetworkIdentity 컴포넌트 필수
  - NetworkTransformReliable 컴포넌트 포함
  - Player 스크립트 연결

### Terrain 자원
- `Assets/Resources/Terrain/`: 터레인 관련 자원
  - `ground.asset`: 메인 터레인 데이터
  - `grass_prefab/`: 디테일 메쉬 프리팹들
  - 3개의 terrain layer 사용 (1.grass, 2.muddy, 3.pebbl)

## 빌드 설정

### WebGL 빌드
1. Player Settings:
   - Strip Engine Code: 비활성화
   - Managed Stripping Level: Disabled
2. Graphics Settings:
   - Always Included Shaders에 필요한 셰이더 추가
3. Quality Settings:
   - WebGL은 Balanced 품질 사용

## 주의사항

1. **Player.cs 수정 시**:
   - 터레인 관련 코드 최소화
   - 네트워크 동기화 코드 복잡하게 만들지 않기
   - 모든 로직은 간단하게 유지

2. **새 기능 추가 시**:
   - NetworkBehaviour 상속 금지
   - 기존 Player.cs 내에서만 구현
   - Command/ClientRpc/SyncVar 사용법 준수

3. **애니메이션 추가 시**:
   - SyncVar + Hook 패턴 사용
   - Trigger 기반 애니메이션 전환
   - 네트워크 동기화 필수

4. **WebGL 테스트**:
   - 에디터와 WebGL 빌드 동작이 다를 수 있음
   - 특히 terrain detail mesh 렌더링 확인 필요
   - 브라우저 콘솔에서 WebGL 에러 확인

## 아두이노 연동 환경 변화 시스템 (통합 매니저)

### ChangeEnviroment.cs - 중앙 환경 관리 매니저
모든 환경 변화를 통합 관리하는 중앙 매니저로 리팩토링됨

### 현재 구현된 환경 변화 (버튼 누적 기반)

#### 1. Post Processing 효과
- **Bloom**: 버튼당 +5.0 intensity (최대 50)
- **Vignette**: 버튼당 +0.05 intensity (최대 0.8) - 가장자리 어두움
- **Color Filter**: 점진적으로 차가운 톤으로 변화
- **Exposure**: 버튼당 -0.1 (최대 -2.0) - 점진적으로 어둡게

#### 2. 날씨 시스템
- **비 강도**: 버튼당 +100% 증가
  - Emission Rate: 500 * intensity
  - Max Particles: emission rate * 15
  - 낙하 속도: -20 * intensity ~ -10 * intensity
  - 최대 강도: 5000%
- **비 사운드**: 강도에 비례 (0.4 ~ 1.0)

#### 3. 안개 (Fog)
- **밀도**: 버튼당 +0.01 (0.02 ~ 0.3)
- **색상**: 점진적으로 어둡고 차가운 톤으로 변화

#### 4. 조명 (Lighting)
- **강도**: 버튼당 -0.1 감소 (1.0 ~ 0.1)
- **색온도**: 따뜻한 톤에서 차가운 톤으로 변화

#### 5. 바람 (Wind)
- **강도**: 버튼당 +2.0 (0 ~ 50)
- **난류**: 버튼당 +0.5 (0 ~ 5)
- Wind Zone 없을 시 자동 생성

### 아두이노 통신 설정
- **포트**: /dev/cu.usbmodem2201
- **Baud Rate**: 19200
- **동작 조건**: NetworkServer.active (호스트에서만 동작)
- **데이터 처리**: SerialPort.ReadLine()으로 버튼 입력 수신

### 네트워크 동기화
- **SyncVar**: buttonPressCount로 누적 카운트 동기화
- **ClientRpc**: RpcSyncEnvironmentChanges()로 모든 클라이언트에 환경 변화 전달
- **테스트**: Space 키로 수동 테스트 가능

### 시스템 특징
- **통합 관리**: 모든 환경 요소를 ChangeEnviroment.cs에서 중앙 관리
- **모듈식 구조**: 각 환경 시스템별 독립적인 Apply 메서드
- **자동 초기화**: 필요한 컴포넌트 자동 검색 및 생성
- **Inspector 설정**: 각 변화 multiplier를 Inspector에서 조정 가능

### 추가 가능한 환경 변화 아이디어
1. **추가 Post Processing 효과**
   - Chromatic Aberration (색수차)
   - Depth of Field (피사계 심도)
   - Motion Blur (모션 블러)
   - Film Grain (필름 그레인)

2. **터레인 변화**
   - 텍스처 블렌딩 비율 변경
   - Detail Mesh 밀도 조절
   - 터레인 색조 변경

3. **파티클 효과**
   - 낙엽 효과
   - 먼지 파티클
   - 번개 효과

4. **사운드 환경**
   - 환경음 추가 (바람소리, 천둥소리)
   - 에코/리버브 효과
   - 3D 공간 사운드

## 해결되지 않은 문제

1. **WebGL Terrain Detail Mesh**:
   - BushDry, Heather, Plant가 WebGL에서 렌더링 안 됨
   - 그림자는 나타나지만 메쉬 자체가 안 보임
   - Unity의 URP WebGL 호환성 버그로 추정

2. **해결 시도 기록**:
   - Material 설정 수정 (GPU Instancing, Alpha Clip 등)
   - Shader 변경 시도 (Legacy shader)
   - Project Settings 조정 (Code Stripping 등)
   - 모두 실패, GrassDry만 정상 작동