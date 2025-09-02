using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        // Main Camera 찾기
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // 카메라를 바라보도록 회전 (텍스트가 뒤집히지 않게)
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
        else
        {
            // Camera.main이 null일 수 있으므로 재시도
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // 모든 카메라 중 활성화된 것 찾기
                Camera[] cameras = FindObjectsOfType<Camera>();
                foreach (Camera cam in cameras)
                {
                    if (cam.enabled && cam.gameObject.activeInHierarchy)
                    {
                        mainCamera = cam;
                        break;
                    }
                }
            }
        }
    }
}