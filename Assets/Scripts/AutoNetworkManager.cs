using System.Collections;
using System.IO.Ports;
using UnityEngine;
using Mirror;
using Mirror.SimpleWeb;

public class AutoNetworkManager : NetworkManager
{
    [Header("Auto Network Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private string[] arduinoPortPatterns = { "COM", "tty.usb", "tty.wchusbserial" };
    [SerializeField] private int telepathyPort = 7778;
    [SerializeField] private int simpleWebPort = 7779;

    [Header("MultiplexTransport Settings")]
    [SerializeField] private MultiplexTransport multiplexTransport;
    [SerializeField] private TelepathyTransport telepathyTransport;
    [SerializeField] private SimpleWebTransport simpleWebTransport;

    [Header("Transport Configuration")]
    [Tooltip("MultiplexTransport를 사용하여 TelepathyTransport와 SimpleWebTransport를 동시에 지원합니다.")]
    [SerializeField] private bool useMultiplexTransport = true;

    private bool isArduinoConnected = false;
    private bool isInitialized = false;

    /*
    === MultiplexTransport 설정 방법 ===
    
    1. Unity Inspector 설정:
       - GameObject에 MultiplexTransport 컴포넌트 추가
       - GameObject에 TelepathyTransport 컴포넌트 추가  
       - GameObject에 SimpleWebTransport 컴포넌트 추가
    
    2. MultiplexTransport 설정:
       - Transports 배열 크기를 2로 설정
       - Element 0: TelepathyTransport 할당
       - Element 1: SimpleWebTransport 할당
    
    3. NetworkManager 설정:
       - Transport 필드에 MultiplexTransport 할당
    
    4. 포트 설정:
       - TelepathyTransport Port: 7778
       - SimpleWebTransport Port: 7779
    
    5. 동작 방식:
       - Host (Standalone): 두 Transport 모두 리스닝
       - Client (WebGL): SimpleWebTransport로 연결
       - Client (Standalone): TelepathyTransport로 연결
    */

    public override void Awake()
    {
        // MultiplexTransport를 먼저 설정 (base.Awake() 호출 전에)
        SetupMultiplexTransport();
        
        base.Awake();
    }

    public override void Start()
    {
        base.Start();

        if (!isInitialized)
        {
            InitializeNetworkManager();
        }
    }

    private void InitializeNetworkManager()
    {
        isInitialized = true;

        // 아두이노 연결 상태 확인
        isArduinoConnected = HasArduinoDevice();

        // Transport는 이미 Awake()에서 설정됨
        // SetupTransport(); // MultiplexTransport 사용시 불필요

        // 네트워크 포트 설정
        SetNetworkPort();

        // 플랫폼별 자동 Host/Client 결정
        DetermineNetworkMode();

        if (enableDebugLogs)
        {
            LogNetworkConfiguration();
        }
    }

    private bool HasArduinoDevice()
    {
#if UNITY_EDITOR
        // 에디터에서는 테스트를 위해 true로 설정 (필요에 따라 변경)
        if (enableDebugLogs)
            Debug.Log("[Editor Mode] Simulating Arduino connection for testing");
        return true;
#elif UNITY_STANDALONE
        try
        {
            string[] portNames = SerialPort.GetPortNames();
            
            foreach (string portName in portNames)
            {
                foreach (string pattern in arduinoPortPatterns)
                {
                    if (portName.Contains(pattern))
                    {
                        if (enableDebugLogs)
                            Debug.Log($"Arduino device detected on port: {portName}");
                        return true;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"Error checking serial ports: {e.Message}");
        }
        
        if (enableDebugLogs)
            Debug.Log("No Arduino device detected");
        return false;
#else
        // WebGL이나 다른 플랫폼에서는 아두이노 감지 불가
        if (enableDebugLogs)
            Debug.Log("Arduino detection not supported on this platform");
        return false;
#endif
    }

    private void SetupMultiplexTransport()
    {
        if (!useMultiplexTransport)
        {
            // 기존 단일 Transport 방식 사용
            SetupSingleTransport();
            return;
        }

        // TelepathyTransport와 SimpleWebTransport를 먼저 설정
        if (telepathyTransport == null)
        {
            telepathyTransport = gameObject.GetComponent<TelepathyTransport>();
            if (telepathyTransport == null)
            {
                telepathyTransport = gameObject.AddComponent<TelepathyTransport>();
            }
        }
        
        if (simpleWebTransport == null)
        {
            simpleWebTransport = gameObject.GetComponent<SimpleWebTransport>();
            if (simpleWebTransport == null)
            {
                simpleWebTransport = gameObject.AddComponent<SimpleWebTransport>();
            }
        }

        // MultiplexTransport 자동 설정 (sub-transport들이 준비된 후)
        if (multiplexTransport == null)
        {
            multiplexTransport = gameObject.GetComponent<MultiplexTransport>();
            if (multiplexTransport == null)
            {
                multiplexTransport = gameObject.AddComponent<MultiplexTransport>();
                // AddComponent 직후 바로 transports 배열 설정
                multiplexTransport.transports = new Transport[] { telepathyTransport, simpleWebTransport };
            }
            else
            {
                // 기존 컴포넌트인 경우에도 transports 배열 설정
                multiplexTransport.transports = new Transport[] { telepathyTransport, simpleWebTransport };
            }
        }

        // MultiplexTransport를 기본 Transport로 설정
        Transport.active = multiplexTransport;
        transport = multiplexTransport;

        if (enableDebugLogs)
        {
            Debug.Log("[AutoNetworkManager] MultiplexTransport 설정 완료");
            Debug.Log($"Active Transport: {Transport.active?.GetType().Name}");
            Debug.Log($"NetworkManager Transport: {transport?.GetType().Name}");
            Debug.Log($"Sub-transports: TelepathyTransport, SimpleWebTransport");
        }
    }

    private void SetupSingleTransport()
    {
        // 기존 단일 Transport 로직 (하위 호환성)
#if UNITY_WEBGL && !UNITY_EDITOR
        if (simpleWebTransport == null)
        {
            simpleWebTransport = gameObject.GetComponent<SimpleWebTransport>();
            if (simpleWebTransport == null)
            {
                simpleWebTransport = gameObject.AddComponent<SimpleWebTransport>();
            }
        }
        transport = simpleWebTransport;
        if (enableDebugLogs)
            Debug.Log("WebGL Platform: Using SimpleWebTransport (Single Mode)");
#else
        if (telepathyTransport == null)
        {
            telepathyTransport = gameObject.GetComponent<TelepathyTransport>();
            if (telepathyTransport == null)
            {
                telepathyTransport = gameObject.AddComponent<TelepathyTransport>();
            }
        }
        transport = telepathyTransport;
        if (enableDebugLogs)
            Debug.Log("Standalone Platform: Using TelepathyTransport (Single Mode)");
#endif
    }

    private void SetNetworkPort()
    {
        if (useMultiplexTransport)
        {
            // MultiplexTransport 사용시 개별 Transport 포트 설정
            SetupMultiplexTransportPorts();
        }
        else
        {
            // 단일 Transport 포트 설정 (기존 로직)
            SetupSingleTransportPort();
        }
    }

    private void SetupMultiplexTransportPorts()
    {
        // TelepathyTransport 포트 설정
        if (telepathyTransport != null && telepathyTransport is PortTransport telepathyPortTransport)
        {
            telepathyPortTransport.Port = (ushort)telepathyPort;
            if (enableDebugLogs)
                Debug.Log($"TelepathyTransport port set to: {telepathyPort}");
        }

        // SimpleWebTransport 포트 설정
        if (simpleWebTransport != null)
        {
            simpleWebTransport.port = (ushort)simpleWebPort;
            simpleWebTransport.clientWebsocketSettings.ClientPortOption = WebsocketPortOption.DefaultSameAsServer;
            simpleWebTransport.clientWebsocketSettings.CustomClientPort = (ushort)simpleWebPort;

            if (enableDebugLogs)
                Debug.Log($"SimpleWebTransport port set to: {simpleWebPort}");
        }

        if (enableDebugLogs)
            Debug.Log($"[MultiplexTransport] Transports configured - Telepathy: {telepathyPort}, SimpleWeb: {simpleWebPort}");
    }

    private void SetupSingleTransportPort()
    {
        if (transport != null)
        {
            // SimpleWebTransport 설정
            if (transport is SimpleWebTransport simpleWeb)
            {
                simpleWeb.port = (ushort)simpleWebPort;
                simpleWeb.clientWebsocketSettings.ClientPortOption = WebsocketPortOption.DefaultSameAsServer;
                simpleWeb.clientWebsocketSettings.CustomClientPort = (ushort)simpleWebPort;

                if (enableDebugLogs)
                    Debug.Log($"SimpleWebTransport configured - Server Port: {simpleWebPort}");
            }
            // 다른 Transport들은 PortTransport 인터페이스 사용 (TelepathyTransport)
            else if (transport is PortTransport portTransport)
            {
                portTransport.Port = (ushort)telepathyPort;

                if (enableDebugLogs)
                    Debug.Log($"Transport port set to: {telepathyPort}");
            }
        }
    }

    private void DetermineNetworkMode()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: 무조건 Client 모드
        StartCoroutine(StartClientDelayed());
        
        if (enableDebugLogs)
            Debug.Log("WebGL: Starting as Client");
            
#else
        // Standalone: 아두이노 연결 여부에 따라 결정
        if (isArduinoConnected)
        {
            // 아두이노 있으면 Host 모드
            StartCoroutine(StartHostDelayed());

            if (enableDebugLogs)
                Debug.Log("Arduino detected: Starting as Host");
        }
        else
        {
            // 아두이노 없으면 Client 모드
            StartCoroutine(StartClientDelayed());

            if (enableDebugLogs)
                Debug.Log("No Arduino: Starting as Client");
        }
#endif
    }

    private IEnumerator StartHostDelayed()
    {
        // 약간의 지연 후 Host 시작 (초기화 완료 대기)
        yield return new WaitForSeconds(0.5f);

        if (!NetworkServer.active && !NetworkClient.active)
        {
            StartHost();
        }
    }

    private IEnumerator StartClientDelayed()
    {
        // 약간의 지연 후 Client 시작 (초기화 완료 대기)
        yield return new WaitForSeconds(0.5f);

        if (!NetworkClient.active)
        {
            StartClient();
        }
    }

    private void LogNetworkConfiguration()
    {
        Debug.Log("=== Auto Network Manager Configuration ===");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Arduino Connected: {isArduinoConnected}");
        Debug.Log($"Use MultiplexTransport: {useMultiplexTransport}");
        Debug.Log($"Active Transport: {Transport.active?.GetType().Name}");
        Debug.Log($"NetworkManager Transport: {transport?.GetType().Name}");
        Debug.Log($"Telepathy Port: {telepathyPort}, SimpleWeb Port: {simpleWebPort}");

        if (useMultiplexTransport && multiplexTransport != null)
        {
            Debug.Log($"MultiplexTransport Components:");
            Debug.Log($"  - TelepathyTransport: {(telepathyTransport != null ? "✓" : "✗")}");
            Debug.Log($"  - SimpleWebTransport: {(simpleWebTransport != null ? "✓" : "✗")}");
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Mode: Client (WebGL) - Will use SimpleWebTransport");
#else
        Debug.Log($"Mode: {(isArduinoConnected ? "Host" : "Client")} (Standalone) - Will use TelepathyTransport");
#endif
        Debug.Log("==========================================");
    }

    public override void OnStartHost()
    {
        base.OnStartHost();

        if (enableDebugLogs)
            Debug.Log("Host started successfully");

        // Host는 플레이어 프리팹을 생성하지 않음
        autoCreatePlayer = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (enableDebugLogs)
            Debug.Log("Client started successfully");

        // Client는 플레이어 프리팹을 자동 생성
        autoCreatePlayer = true;
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 플레이어 스폰 위치 결정
        Transform startPos = GetStartPosition();
        GameObject player = startPos != null ?
            Instantiate(playerPrefab, startPos.position, startPos.rotation) :
            Instantiate(playerPrefab);

        NetworkServer.AddPlayerForConnection(conn, player);

        if (enableDebugLogs)
            Debug.Log($"Player added for connection {conn.connectionId}");
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        if (enableDebugLogs)
            Debug.Log("Client connected to server");
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        if (enableDebugLogs)
            Debug.Log("Client disconnected from server");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);

        if (enableDebugLogs)
            Debug.Log($"Client {conn.connectionId} disconnected from server");
    }

    // Inspector에서 Transport 다시 감지하는 버튼용 함수
    [ContextMenu("Refresh Arduino Detection")]
    public void RefreshArduinoDetection()
    {
        isArduinoConnected = HasArduinoDevice();

        if (enableDebugLogs)
        {
            Debug.Log($"Arduino detection refreshed. Connected: {isArduinoConnected}");
        }
    }

    // Transport 수동 전환 함수
    public void SwitchToTelepathy()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (telepathyTransport == null)
        {
            telepathyTransport = gameObject.GetComponent<TelepathyTransport>();
            if (telepathyTransport == null)
            {
                telepathyTransport = gameObject.AddComponent<TelepathyTransport>();
            }
        }

        transport = telepathyTransport;
        SetNetworkPort();

        if (enableDebugLogs)
            Debug.Log("Switched to TelepathyTransport");
#endif
    }

    public void SwitchToSimpleWeb()
    {
        if (simpleWebTransport == null)
        {
            simpleWebTransport = gameObject.GetComponent<SimpleWebTransport>();
            if (simpleWebTransport == null)
            {
                simpleWebTransport = gameObject.AddComponent<SimpleWebTransport>();
            }
        }

        transport = simpleWebTransport;
        SetNetworkPort();

        if (enableDebugLogs)
            Debug.Log("Switched to SimpleWebTransport");
    }
}