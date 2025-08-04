using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using Mirror;

public class ArduinoDataReciver : MonoBehaviour
{
    SerialPort serialPort;
    public string portName = "/dev/cu.usbmodem2201";
    public int baudRate = 19200;
    private ChangeEnviroment changeEnvironment;

    private bool isInitialized = false;

    void Start()
    {
        Debug.Log($"ArduinoDataReceiver Start() called");
        // Start에서는 초기화하지 않고, Update에서 조건이 맞을 때 초기화
    }

    private void InitializeSerial()
    {
        if (isInitialized) return;
        
        Debug.Log($"InitializeSerial() - NetworkServer.active: {NetworkServer.active}");
        
        changeEnvironment = FindObjectOfType<ChangeEnviroment>();
        if (changeEnvironment == null)
        {
            Debug.LogError("ChangeEnviroment component not found!");
            return;
        }
        else
        {
            Debug.Log("ChangeEnviroment component found successfully");
        }

        Debug.Log($"Available ports: {string.Join(", ", SerialPort.GetPortNames())}");
        Debug.Log($"Attempting to connect to Arduino on port: {portName} with baud rate: {baudRate}");
        
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50; // 50ms timeout
            serialPort.WriteTimeout = 1000;
            
            Debug.Log("Serial port object created, attempting to open...");
            serialPort.Open();
            Debug.Log($"✅ Arduino serial port opened successfully on {portName}");
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Error opening serial port {portName}: " + e.Message);
            Debug.LogError($"Exception type: {e.GetType().Name}");
            Debug.LogError($"Please make sure:");
            Debug.LogError("1. Arduino is connected");
            Debug.LogError("2. Arduino IDE Serial Monitor is CLOSED");
            Debug.LogError("3. Port name is correct");
            serialPort = null; // 명시적으로 null 설정
        }
    }

    private float debugTimer = 0f;
    private int readAttempts = 0;

    void Update()
    {
        // NetworkServer가 활성화되었을 때만 시리얼 포트 초기화
        if (NetworkServer.active && !isInitialized)
        {
            InitializeSerial();
        }
        
        // 서버에서만 실행
        if (!NetworkServer.active) return;

        // 시리얼 포트가 초기화되지 않았으면 리턴
        if (!isInitialized) return;

        // 5초마다 연결 상태 로그
        debugTimer += Time.deltaTime;
        if (debugTimer >= 5f)
        {
            debugTimer = 0f;
            if (serialPort != null)
            {
                Debug.Log($"Serial port status - IsOpen: {serialPort.IsOpen}, Port: {portName}, Read attempts: {readAttempts}");
            }
            else
            {
                Debug.Log("Serial port is null");
            }
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                readAttempts++;
                
                // 데이터가 있는지 먼저 확인
                if (serialPort.BytesToRead > 0)
                {
                    string data = serialPort.ReadLine();
                    Debug.Log($"Received Arduino data: '{data}'");

                    // 아두이노에서 버튼 데이터 받으면 환경 변경
                    if (!string.IsNullOrEmpty(data) && changeEnvironment != null)
                    {
                        string trimmedData = data.Trim();
                        Debug.Log($"Processing button data: '{trimmedData}'");
                        changeEnvironment.OnButtonPressed(trimmedData);
                    }
                }
            }
            catch (TimeoutException) 
            {
                // Timeout은 정상 - 데이터가 없을 때 발생
            }
            catch (Exception e)
            {
                Debug.LogError($"Serial read error: {e.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}
