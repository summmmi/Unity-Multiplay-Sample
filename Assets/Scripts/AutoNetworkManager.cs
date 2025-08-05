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
    [SerializeField] private int networkPort = 7778;
    
    [Header("Transport References")]
    [SerializeField] private Transport telepathyTransport;
    [SerializeField] private Transport simpleWebTransport;
    
    private bool isArduinoConnected = false;
    private bool isInitialized = false;

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
        
        // Transport 설정
        SetupTransport();
        
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

    private void SetupTransport()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: SimpleWebTransport 사용
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
            Debug.Log("WebGL Platform: Using SimpleWebTransport");
            
#else
        // Standalone: TelepathyTransport 사용
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
            Debug.Log("Standalone Platform: Using TelepathyTransport");
#endif
    }

    private void SetNetworkPort()
    {
        if (transport != null)
        {
            // SimpleWebTransport 설정
            if (transport is SimpleWebTransport simpleWeb)
            {
                // 서버 포트 설정
                simpleWeb.port = (ushort)networkPort;
                
                // 클라이언트 설정: Default Same As Server, 포트 7778
                simpleWeb.clientWebsocketSettings.ClientPortOption = WebsocketPortOption.DefaultSameAsServer;
                simpleWeb.clientWebsocketSettings.CustomClientPort = (ushort)networkPort;
                
                if (enableDebugLogs)
                    Debug.Log($"SimpleWebTransport configured - Server Port: {networkPort}, Client: Default Same As Server");
            }
            // 다른 Transport들은 PortTransport 인터페이스 사용
            else if (transport is PortTransport portTransport)
            {
                portTransport.Port = (ushort)networkPort;
                
                if (enableDebugLogs)
                    Debug.Log($"Transport port set to: {networkPort}");
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
        Debug.Log($"Transport: {transport?.GetType().Name}");
        Debug.Log($"Network Port: {networkPort}");
        
#if UNITY_WEBGL && !UNITY_EDITOR
        Debug.Log("Mode: Client (WebGL)");
#else
        Debug.Log($"Mode: {(isArduinoConnected ? "Host" : "Client")} (Standalone)");
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