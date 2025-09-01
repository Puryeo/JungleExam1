// Prototype Only – Delete or replace in production
using UnityEngine;

public class forgamedesign03_ZombieBoss : MonoBehaviour
{
    [Header("Boss Settings")]
    [SerializeField, Tooltip("보스 체력")]
    private int health = 4;
    [SerializeField, Tooltip("보스가 살아있는지 여부")]
    private bool isAlive = true;
    
    [Header("Collision Detection")]
    [SerializeField, Tooltip("충돌 감지용 콜라이더")]
    private Collider zombieCollider;
    
    private void Awake()
    {
        // 태그 설정
        gameObject.tag = "Boss";
        
        // 콜라이더 가져오기
        zombieCollider = GetComponent<Collider>();
        if (zombieCollider == null)
        {
            Debug.LogError("ZombieBoss에 Collider가 없습니다!");
        }
    }
    
    /* 충돌 처리는 PlayerController에서 처리 */
    // OnCollisionEnter 제거 - PlayerController에서 모든 충돌 처리
    
    public void TakeDamage(int damage)
    {
        if (!isAlive) return;
        
        health -= damage;
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        isAlive = false;
        Debug.Log("🔥 ZombieBoss 처치됨! Enhanced Bounce 트리거!");
        
        // Player에게 Enhanced Bounce 신호 전송
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerController = player.GetComponent<forgamedesign03_PlayerController>();
            if (playerController != null)
            {
                playerController.TriggerEnhancedBounce();
            }
        }
        
        // ZombieBoss 오브젝트 비활성화 (나중에 파괴 가능)
        gameObject.SetActive(false);
    }
} 