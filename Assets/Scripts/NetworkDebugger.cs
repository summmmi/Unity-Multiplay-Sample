using UnityEngine;
using Mirror;

public class NetworkDebugger : NetworkBehaviour
{
    void Start()
    {
        Debug.Log($"NetworkDebugger Start() - isServer: {isServer}, isClient: {isClient}, isLocalPlayer: {isLocalPlayer}");
    }

    void Update()
    {
        // 네트워크 상태 디버깅 (5초마다)
        if (Time.time % 5f < Time.deltaTime)
        {
            if (NetworkManager.singleton != null)
            {
                Debug.Log($"=== Network Status ===");
                Debug.Log($"NetworkServer.active: {NetworkServer.active}");
                Debug.Log($"NetworkClient.active: {NetworkClient.active}");
                Debug.Log($"NetworkClient.isConnected: {NetworkClient.isConnected}");
                Debug.Log($"NetworkServer.connections.Count: {NetworkServer.connections.Count}");
                Debug.Log($"NetworkManager.mode: {NetworkManager.singleton.mode}");

                if (NetworkServer.active)
                {
                    Debug.Log($"Server - Active connections: {NetworkServer.connections.Count}");
                    foreach (var conn in NetworkServer.connections.Values)
                    {
                        Debug.Log($"  Connection {conn.connectionId}: {conn.address}");
                    }
                }

                if (NetworkClient.active)
                {
                    Debug.Log($"Client - Connected: {NetworkClient.isConnected}, Ready: {NetworkClient.ready}");
                    Debug.Log($"Client connection exists: {NetworkClient.connection != null}");
                }
            }
        }
    }
}