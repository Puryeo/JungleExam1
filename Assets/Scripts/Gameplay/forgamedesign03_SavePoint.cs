// Prototype Only – Delete or replace in production
using UnityEngine;

public class forgamedesign03_SavePoint : MonoBehaviour
{
    [Header("SavePoint Settings")]
    [SerializeField, Tooltip("체크포인트 활성화 여부")]
    private bool isActive = true;
    [SerializeField, Tooltip("저장 후 오브젝트 삭제")]
    private bool destroyAfterSave = true;
    [SerializeField, Tooltip("저장 완료 표시")]
    private bool hasSaved = false;
    
    [Header("Visual Feedback")]
    [SerializeField, Tooltip("저장 시 이펙트 오브젝트 (옵션)")]
    private GameObject saveEffect;
    [SerializeField, Tooltip("저장 시 사운드 재생 (옵션)")]
    private AudioSource audioSource;
    
    [Header("Debug")]
    [SerializeField, Tooltip("디버그 로그 출력")]
    private bool enableDebugLog = true;
    
    private void Awake()
    {
        // 태그 설정
        gameObject.tag = "SavePoint";
        
        // Trigger 콜라이더 확인
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[SavePoint] {gameObject.name}에 Collider가 없습니다!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning($"[SavePoint] {gameObject.name}의 Collider가 Trigger로 설정되지 않았습니다. 자동으로 설정합니다.");
            col.isTrigger = true;
        }
        
        // AudioSource가 없으면 자동 추가
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // 플레이어만 감지
        if (!other.CompareTag("Player")) return;
        
        // 비활성화 상태이거나 이미 저장한 경우 무시
        if (!isActive || hasSaved) return;
        
        // PlayerController 가져오기
        var playerController = other.GetComponent<forgamedesign03_PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[SavePoint] PlayerController를 찾을 수 없습니다!");
            return;
        }
        
        // 체크포인트 저장
        Vector3 savePosition = transform.position;
        playerController.SaveCheckpoint(savePosition);
        
        // 저장 완료 처리
        hasSaved = true;
        
        // 피드백 효과
        TriggerSaveEffects();
        
        if (enableDebugLog)
        {
            Debug.Log($"💾 [SavePoint] 체크포인트 저장됨: {savePosition} (SavePoint: {gameObject.name})");
        }
        
        // 저장 후 오브젝트 삭제
        if (destroyAfterSave)
        {
            if (enableDebugLog)
            {
                Debug.Log($"🗑️ [SavePoint] {gameObject.name} 삭제됨");
            }
            
            // 이펙트가 끝날 때까지 잠시 대기 후 삭제
            Destroy(gameObject, 0.6f); // 0.6초 후 삭제 (색상 복원 + 여유시간)
        }
    }
    
    private void TriggerSaveEffects()
    {
        // 이펙트 오브젝트 활성화
        if (saveEffect != null)
        {
            if (saveEffect.activeInHierarchy)
            {
                // 이미 활성화된 경우 재시작
                saveEffect.SetActive(false);
            }
            saveEffect.SetActive(true);
        }
        
        // 사운드 재생
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
        
        // 추가 시각적 피드백 (컬러 변경 등)
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // 저장 완료 표시로 색상 변경 (초록색)
            renderer.material.color = Color.green;
            
            // destroyAfterSave가 false인 경우에만 색상 복원
            if (!destroyAfterSave)
            {
                // 0.5초 후 원래 색상으로 복원
                Invoke(nameof(RestoreOriginalColor), 0.5f);
            }
        }
    }
    
    private void RestoreOriginalColor()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white; // 기본 색상으로 복원
        }
    }
    
    /* 공개 메서드 ------------------------------------------------- */
    
    public void ResetSavePoint()
    {
        hasSaved = false;
        isActive = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"[SavePoint] {gameObject.name} 초기화됨");
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        
        if (enableDebugLog)
        {
            Debug.Log($"[SavePoint] {gameObject.name} 활성화 상태: {active}");
        }
    }
    
    public bool HasSaved()
    {
        return hasSaved;
    }
    
    public Vector3 GetSavePosition()
    {
        return transform.position;
    }
    
    /* Inspector 버튼들 (에디터에서만) ----------------------------- */
    
    [ContextMenu("SavePoint 초기화")]
    private void ResetSavePointContextMenu()
    {
        ResetSavePoint();
    }
    
    [ContextMenu("저장 테스트")]
    private void TestSaveContextMenu()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            OnTriggerEnter(player.GetComponent<Collider>());
        }
        else
        {
            Debug.LogWarning("[SavePoint] 플레이어를 찾을 수 없습니다!");
        }
    }
}