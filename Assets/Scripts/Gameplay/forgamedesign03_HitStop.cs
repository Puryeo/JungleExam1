// Prototype Only – Delete or replace in production
using UnityEngine;
using System.Collections;

public class forgamedesign03_HitStop : MonoBehaviour
{
    [Header("HitStop Settings")]
    [SerializeField, Tooltip("HitStop 지속 시간 (초)")]
    private float hitStopDuration = 0.10f;
    [SerializeField, Tooltip("HitStop 시 시간 배율")]
    private float hitStopTimeScale = 0.0f;
    
    [Header("Debug")]
    [SerializeField, Tooltip("HitStop 디버그 표시")]
    private bool showHitStopDebug = true;
    
    private static forgamedesign03_HitStop instance;
    private bool isHitStopping = false;
    private float originalTimeScale;
    
    public static forgamedesign03_HitStop Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<forgamedesign03_HitStop>();
                if (instance == null)
                {
                    GameObject hitStopObject = new GameObject("HitStopManager");
                    instance = hitStopObject.AddComponent<forgamedesign03_HitStop>();
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            originalTimeScale = Time.timeScale;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    /* HitStop 실행 ------------------------------------------------- */
    public void TriggerHitStop()
    {
        if (isHitStopping) return; // 이미 HitStop 중이면 무시
        
        StartCoroutine(HitStopCoroutine(hitStopDuration));
    }
    
    public void TriggerHitStop(float duration)
    {
        if (isHitStopping) return;
        
        StartCoroutine(HitStopCoroutine(duration));
    }
    
    private IEnumerator HitStopCoroutine(float duration = -1f)
    {
        if (duration < 0) duration = hitStopDuration;
        
        isHitStopping = true;
        originalTimeScale = Time.timeScale;
        
        // 시간 정지
        Time.timeScale = hitStopTimeScale;
        
        if (showHitStopDebug)
        {
            Debug.Log($"HitStop 시작! 지속시간: {duration}초");
        }
        
        // 실제 시간으로 대기 (Time.timeScale 영향 없음)
        yield return new WaitForSecondsRealtime(duration);
        
        // 시간 복구
        Time.timeScale = originalTimeScale;
        isHitStopping = false;
        
        if (showHitStopDebug)
        {
            Debug.Log("HitStop 종료!");
        }
    }
    
    /* 유틸리티 메서드 ---------------------------------------------- */
    public bool IsHitStopping()
    {
        return isHitStopping;
    }
    
    public void StopHitStop()
    {
        if (isHitStopping)
        {
            StopAllCoroutines();
            Time.timeScale = originalTimeScale;
            isHitStopping = false;
            
            if (showHitStopDebug)
            {
                Debug.Log("HitStop 강제 종료!");
            }
        }
    }
}