# Unity WebGL 로컬 네트워크 멀티플레이어 설정 가이드

## 📖 개요
Unity WebGL + Mirror 네트워킹을 사용한 로컬 네트워크 멀티플레이어 게임 설정 방법입니다.
같은 WiFi에 연결된 여러 기기(PC, 모바일, 태블릿)에서 동시에 게임을 플레이할 수 있습니다.

---

## 🏗️ 시스템 구조

```
📱 WebGL 클라이언트들 (모바일/PC)
    ↓ http://IP주소:8080
🌐 Python 웹서버 (포트 8080) 
    ↓ WebSocket 연결
🎮 Unity Mirror 서버 (포트 7779)
    ↓ 아두이노 제어
🔌 아두이노 (환경 제어)
```

---

## 🛠️ 초기 설정

### 1. 파일 구조 확인
```
/Users/macmini/unity/multiplay-sample/
├── local_server_simple.py          # Python 웹서버
├── Assets/Scripts/Network/AutoNetworkManager.cs  # Unity 네트워크 관리자
└── WebGL 빌드 폴더 (별도 위치)
```

### 2. Unity 설정
**파일:** `Assets/Scripts/Network/AutoNetworkManager.cs`
```csharp
[Header("Local Network Settings")]
[SerializeField] private string localServerAddress = "192.168.219.105"; // 여기에 IP 입력
```

**Inspector 설정:**
- Telepathy Port: 7778 (PC 클라이언트)
- SimpleWeb Port: 7779 (WebGL 클라이언트) 
- Local Server Address: 현재 IP 주소

### 3. WebGL 빌드 경로
**Python 서버 설정:** `local_server_simple.py`
```python
BUILD_PATH = "/Users/macmini/Desktop/1-Projects/수퍼 테스트 배드/팀작업/Unity_WebGL/public/Build"
```

---

## 🚀 실행 방법

### 1단계: 현재 IP 주소 확인
```bash
ifconfig | grep "inet " | grep -v 127.0.0.1
```
**결과 예시:** `inet 192.168.219.105`

### 2단계: Unity 설정 업데이트
1. Unity 열기
2. AutoNetworkManager 오브젝트 선택
3. Inspector에서 `Local Server Address` 필드에 새 IP 입력
4. 저장

### 3단계: Python 웹서버 실행
```bash
cd /Users/macmini/unity/multiplay-sample
python3 local_server_simple.py
```

**실행 결과:**
```
====================================
🎮 Unity WebGL 로컬 서버
====================================
로컬: http://localhost:8080
네트워크: http://192.168.219.105:8080
====================================
```

### 4단계: Unity Mirror 서버 시작
1. Unity 에디터에서 **Play 버튼** 클릭
2. 아두이노 연결 시 자동으로 Host 모드 시작
3. Console에서 "Host started successfully" 확인

### 5단계: 클라이언트 접속
**같은 WiFi의 다른 기기에서:**
- 웹브라우저로 `http://192.168.219.105:8080` 접속
- 자동으로 Unity WebGL 게임 로드
- Mirror 네트워크 연결 자동 수행

---

## 🔄 다른 WiFi 환경에서 사용하기

### A. IP 주소 변경이 필요한 경우
- WiFi 네트워크 변경 시
- 공유기 재부팅 후 IP 변경 시
- 다른 장소로 이동 시

### B. 업데이트 위치

#### 1. Unity AutoNetworkManager (필수)
**파일:** `Assets/Scripts/Network/AutoNetworkManager.cs`
**라인:** 17
```csharp
[SerializeField] private string localServerAddress = "새로운_IP_주소";
```

#### 2. 새 IP 주소 확인 방법
```bash
# macOS/Linux
ifconfig | grep "inet " | grep "192.168\|10.0\|172.16"

# Windows
ipconfig | findstr IPv4
```

#### 3. 업데이트 순서
1. **새 IP 확인** → 터미널 명령어 실행
2. **Unity 업데이트** → Inspector에서 IP 변경
3. **WebGL 재빌드** (선택사항)
4. **서버 재시작** → Python 서버 재실행

---

## 📱 클라이언트 접속 방법

### PC에서 접속
1. 웹브라우저 열기 (Chrome, Safari, Firefox)
2. 주소창에 `http://IP주소:8080` 입력
3. Unity WebGL 게임 자동 로드

### 모바일/태블릿에서 접속  
1. WiFi가 같은 네트워크에 연결되어 있는지 확인
2. 모바일 브라우저에서 `http://IP주소:8080` 접속
3. 터치 조이스틱으로 게임 플레이

### 접속 주소 예시
- **현재 설정:** `http://192.168.219.105:8080`
- **일반적인 형태:** `http://192.168.1.XXX:8080`
- **사무실 네트워크:** `http://10.0.1.XXX:8080`

---

## 🔧 트러블슈팅

### 문제 1: 웹페이지가 로드되지 않음
**해결책:**
```bash
# 포트 8080 사용 확인
lsof -i :8080

# 프로세스 종료 후 재시작
kill -9 <프로세스ID>
python3 local_server_simple.py
```

### 문제 2: WebGL이 Mirror 서버에 연결되지 않음
**해결책:**
1. Unity Console에서 "Host started successfully" 확인
2. IP 주소가 정확한지 재확인
3. 방화벽에서 7779 포트 허용

### 문제 3: WASM 로드 에러
**증상:** `Incorrect response MIME type. Expected 'application/wasm'`
**해결책:** 
- 현재 Python 서버는 이미 수정됨
- 브라우저 캐시 삭제 후 재접속

### 문제 4: IP 주소를 모르겠음
**해결책:**
```bash
# macOS
ifconfig | grep "inet " | grep -v 127.0.0.1

# 간단한 방법
curl ifconfig.me  # 외부 IP (공유기 환경에서는 부정확)
```

---

## 🎮 게임 기능 확인

### 네트워크 연결 확인
- Unity Console: "Client started successfully"
- WebGL Console: 연결 관련 로그 확인
- 플레이어 움직임이 실시간으로 동기화되는지 확인

### 아두이노 환경 제어 확인  
- 아두이노 버튼 입력 시 날씨 변화
- 모든 클라이언트에 환경 변화 동기화
- 비, 번개, 안개 효과 정상 작동

### 모바일 조작 확인
- 가상 조이스틱 정상 작동
- 터치 카메라 회전 기능
- 네트워크 지연 없이 부드러운 움직임

---

## 📊 성능 최적화

### 동시 접속 권장 수
- **테스트 환경:** 5-10명
- **전시 환경:** 10-20명
- **안정적인 범위:** 최대 15명

### 네트워크 요구사항
- **WiFi 대역폭:** 최소 50Mbps (권장 100Mbps)
- **Ping:** 50ms 이하
- **안정성:** 5GHz WiFi 권장

### 서버 사양
- **CPU:** Intel i5 이상
- **RAM:** 8GB 이상  
- **저장공간:** 2GB (Unity 프로젝트 + 빌드)

---

## 🔐 보안 주의사항

### 네트워크 보안
- **공용 WiFi에서 사용 금지** (보안 위험)
- **사설 네트워크에서만 사용** 권장
- **방화벽 설정** 확인 (7779, 8080 포트)

### 데이터 보호
- 개인정보 수집하지 않음
- 로컬 네트워크만 사용 (인터넷 연결 불필요)
- 게임 데이터는 메모리에서만 처리

---

## 📞 지원 및 문의

### 로그 확인 방법
```bash
# Unity Console 로그
Unity Editor → Console 창

# Python 서버 로그  
터미널에서 실시간 확인

# WebGL 브라우저 로그
F12 → Console 탭
```

### 백업 방법
```bash
# 프로젝트 전체 백업
cp -r /Users/macmini/unity/multiplay-sample ~/Desktop/backup_$(date +%Y%m%d)

# 설정 파일만 백업
cp Assets/Scripts/Network/AutoNetworkManager.cs ~/Desktop/
```

---

## 📝 버전 정보

- **Unity:** 2022.3 LTS
- **Mirror:** 최신 버전
- **Python:** 3.13.6
- **지원 브라우저:** Chrome, Safari, Firefox, Edge
- **지원 플랫폼:** Windows, macOS, iOS, Android

---

**마지막 업데이트:** 2025년 8월 26일  
**문서 버전:** 1.0  
**작성자:** Unity 멀티플레이어 시스템