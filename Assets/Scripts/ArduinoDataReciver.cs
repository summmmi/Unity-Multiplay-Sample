using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using System;
using Mirror;

public class ArduinoDataReciver : NetworkBehaviour
{
    SerialPort serialPort;
    public string portName = "COM3";
    public int baudRate = 19200;
    private ChangeEnviroment changeEnvironment;
    
    void Start()
    {
        // 서버에서만 아두이노 연결
        if (!isServer) return;
        
        changeEnvironment = FindObjectOfType<ChangeEnviroment>();
        if (changeEnvironment == null)
        {
            Debug.LogError("ChangeEnviroment component not found!");
            return;
        }
        
        serialPort = new SerialPort(portName, baudRate);
        try
        {
            serialPort.Open();
            Debug.Log("Arduino serial port opened successfully");
        }
        catch (Exception e)
        {
            Debug.LogError("Error opening serial port: " + e.Message);
        }
    }
    
    void Update()
    {
        // 서버에서만 실행
        if (!isServer) return;
        
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadLine();
                Debug.Log("Received data: " + data);
                
                // 아두이노에서 버튼 데이터 받으면 환경 변경
                if (!string.IsNullOrEmpty(data) && changeEnvironment != null)
                {
                    changeEnvironment.OnButtonPressed(data.Trim());
                }
            }
            catch (TimeoutException) { }
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
