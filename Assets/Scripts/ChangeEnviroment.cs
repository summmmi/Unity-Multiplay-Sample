using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Mirror;

public class ChangeEnviroment : NetworkBehaviour
{
    [Header("Environment Settings")]
    public Volume globalVolume;
    public Color[] tintColors = new Color[]
    {
        Color.white,
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow
    };

    private ColorAdjustments colorAdjustments;
    private int currentColorIndex = 0;

    void Start()
    {
        // Global Volume 찾기
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();
        }

        if (globalVolume == null)
        {
            Debug.LogError("Global Volume not found!");
            return;
        }

        // Color Adjustments 컴포넌트 가져오기 또는 추가
        if (!globalVolume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            colorAdjustments = globalVolume.profile.Add<ColorAdjustments>(false);
        }

        // 초기 설정
        colorAdjustments.colorFilter.overrideState = true;
        colorAdjustments.colorFilter.value = tintColors[0];
    }

    // 아두이노에서 호출되는 메서드
    public void OnButtonPressed(string buttonData)
    {
        Debug.Log($"OnButtonPressed called with data: '{buttonData}', NetworkServer.active: {NetworkServer.active}");

        // 서버에서만 실행
        if (!NetworkServer.active)
        {
            Debug.Log("Not server, ignoring button press");
            return;
        }

        Debug.Log($"Processing button press: {buttonData}");

        // 버튼 데이터에 따라 색상 인덱스 결정
        int colorIndex = GetColorIndexFromData(buttonData);
        Debug.Log($"Selected color index: {colorIndex}");

        // 색상 변경을 모든 클라이언트에 전송
        Debug.Log($"Sending RPC to change color to index {colorIndex}");
        RpcChangeEnvironmentColor(colorIndex);
    }

    private int GetColorIndexFromData(string data)
    {
        // 아두이노 데이터에 따라 색상 인덱스 반환
        // 예: "1", "2", "3" 등의 버튼 번호나
        // 특정 문자열에 따라 색상 선택
        if (int.TryParse(data, out int buttonNumber))
        {
            return Mathf.Clamp(buttonNumber - 1, 0, tintColors.Length - 1);
        }

        // 기본값: 다음 색상으로 순환
        currentColorIndex = (currentColorIndex + 1) % tintColors.Length;
        return currentColorIndex;
    }

    [ClientRpc]
    void RpcChangeEnvironmentColor(int colorIndex)
    {
        // 모든 클라이언트에서 색상 변경 실행
        if (colorIndex >= 0 && colorIndex < tintColors.Length)
        {
            currentColorIndex = colorIndex;
            ApplyColorChange(tintColors[colorIndex]);
        }
    }

    private void ApplyColorChange(Color newColor)
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = newColor;
            Debug.Log($"✅ Environment color changed to: {newColor}");
        }
    }

    // 테스트용 메서드 (키보드로 테스트 가능)
    void Update()
    {
        // 서버에서만 실행
        if (!NetworkServer.active) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space key pressed, testing environment change");
            OnButtonPressed("test");
        }
    }
}
