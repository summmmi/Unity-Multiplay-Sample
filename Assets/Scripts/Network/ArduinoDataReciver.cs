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
        // Start에서는 초기화하지 않고, Update에서 조건이 맞을 때 초기화
    }

    private void InitializeSerial()
    {
        if (isInitialized) return;

        changeEnvironment = FindObjectOfType<ChangeEnviroment>();
        if (changeEnvironment == null)
        {
            Debug.LogError("ChangeEnviroment component not found!");
            return;
        }

        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50;
            serialPort.WriteTimeout = 1000;
            serialPort.Open();
            Debug.Log($"✅ Arduino connected on {portName}");
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Arduino connection failed: {e.Message}");
            serialPort = null;
        }
    }



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



        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
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
