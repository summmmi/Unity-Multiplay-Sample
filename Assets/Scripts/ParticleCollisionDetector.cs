using UnityEngine;
using System.Collections.Generic;

public class ParticleCollisionDetector : MonoBehaviour
{
    [HideInInspector]
    public SimpleRainController rainController;
    
    private ParticleSystem ps;
    private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }
    
    void OnParticleCollision(GameObject other)
    {
        if (rainController == null || ps == null) return;
        
        // 충돌 이벤트 가져오기
        int numCollisionEvents = ps.GetCollisionEvents(other, collisionEvents);
        
        // 각 충돌 지점에서 스플래시 생성
        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 collisionPoint = collisionEvents[i].intersection;
            rainController.OnParticleCollision(collisionPoint);
        }
    }
}