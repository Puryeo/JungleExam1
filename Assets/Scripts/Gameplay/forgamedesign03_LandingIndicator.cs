// Prototype Only – Delete or replace in production
using UnityEngine;

/// <summary>
/// 착지 인디케이터 관리자
/// - Enhanced Bounce 상태에서만 인디케이터 표시
/// - 일반/Slam 상태에 따라 다른 인디케이터 표시
/// - SphereCast로 착지 지점 정확히 계산
/// - 카메라 오프셋은 CameraController에서 자동 처리
/// </summary>
public class forgamedesign03_LandingIndicator : MonoBehaviour
{
    [Header("Indicator Settings")]
    [SerializeField, Tooltip("기본 착지 인디케이터 오브젝트 (Player 자식)")]
    private GameObject landingIndicator;
    [SerializeField, Tooltip("Slam 착지 인디케이터 오브젝트 (Player 자식)")]
    private GameObject landingIndicatorSlam;
    
    [Header("Ray Settings")]
    [SerializeField, Tooltip("Ray 최대 거리")]
    private float maxRayDistance = 50f;
    [SerializeField, Tooltip("SphereCast 반지름 (플레이어 크기와 동일)")]
    private float sphereCastRadius = 0.5f;
    [SerializeField, Tooltip("Ray 디버그 색상 (충돌 시)")]
    private Color rayHitColor = Color.green;
    [SerializeField, Tooltip("Ray 디버그 색상 (충돌 없음)")]
    private Color rayMissColor = Color.red;
    
    [Header("Debug")]
    [SerializeField, Tooltip("Ray 디버그 표시")]
    private bool showRayDebug = true;
    [SerializeField, Tooltip("인디케이터 활성화 상태 (읽기 전용)")]
    private bool isIndicatorActive = false;
    
    private Transform playerTransform;
    private forgamedesign03_PlayerController playerController;
    
    private void Awake()
    {
        // Player 컴포넌트 찾기
        playerController = GetComponent<forgamedesign03_PlayerController>();
        playerTransform = transform;
        
        // 인디케이터 설정
        SetupIndicator();
    }
    
    private void SetupIndicator()
    {
        // 인디케이터 오브젝트 검증
        if (landingIndicator == null)
        {
            Debug.LogError("Landing Indicator가 할당되지 않았습니다! Inspector에서 할당해주세요.");
        }
        
        if (landingIndicatorSlam == null)
        {
            Debug.LogError("Landing Indicator Slam이 할당되지 않았습니다! Inspector에서 할당해주세요.");
        }
        
        // 초기에는 모두 비활성화
        if (landingIndicator != null)
            landingIndicator.SetActive(false);
        if (landingIndicatorSlam != null)
            landingIndicatorSlam.SetActive(false);
        
        Debug.Log("착지 인디케이터 설정 완료");
    }
    
    private void Update()
    {
        // Enhanced Bounce 상태 확인
        bool shouldShowIndicator = ShouldShowIndicator();
        
        if (shouldShowIndicator != isIndicatorActive)
        {
            isIndicatorActive = shouldShowIndicator;
            UpdateIndicatorVisibility();
            
        }
        
        // 인디케이터가 활성화된 경우 위치 업데이트
        if (isIndicatorActive)
        {
            UpdateIndicatorPosition();
        }
        
        // Slam 상태에 따른 인디케이터 전환
        UpdateIndicatorType();
    }
    
    private bool ShouldShowIndicator()
    {
        // PlayerController에서 Enhanced Bounce 상태 확인
        if (playerController != null)
        {
            // Enhanced Bounce 상태일 때만 표시
            bool shouldShow = playerController.HasSlamCharge;
            return shouldShow;
        }
        
        return false;
    }
    
    private void UpdateIndicatorPosition()
    {
        // 플레이어 위치에서 아래쪽으로 SphereCast 발사
        Vector3 rayOrigin = playerTransform.position;
        Vector3 rayDirection = Vector3.down;
        
        // SphereCast 실행
        if (Physics.SphereCast(rayOrigin, sphereCastRadius, rayDirection, out RaycastHit hit, maxRayDistance))
        {
            // 플레이어 X,Z 위치 유지하고 Y값만 충돌 지점으로 설정
            Vector3 indicatorPosition = new Vector3(
                playerTransform.position.x,
                hit.point.y + 0.01f,
                playerTransform.position.z
            );
            
            // 현재 활성화된 인디케이터의 위치 업데이트
            GameObject activeIndicator = GetActiveIndicator();
            if (activeIndicator != null)
            {
                activeIndicator.transform.position = indicatorPosition;
            }
            
            // Debug 표시
            if (showRayDebug)
            {
                DrawSphereCastDebug(rayOrigin, rayDirection, hit.distance, rayHitColor);
            }
        }
        else
        {
            // 충돌 지점이 없으면 모든 인디케이터 숨김
            if (landingIndicator != null)
                landingIndicator.SetActive(false);
            if (landingIndicatorSlam != null)
                landingIndicatorSlam.SetActive(false);
            
            // Debug 표시
            if (showRayDebug)
            {
                DrawSphereCastDebug(rayOrigin, rayDirection, maxRayDistance, rayMissColor);
            }
        }
    }
    
    private void UpdateIndicatorVisibility()
    {
        // Enhanced Bounce 상태에 따른 전체 인디케이터 표시/숨김
        if (isIndicatorActive)
        {
            // Slam 상태에 따라 적절한 인디케이터 활성화
            UpdateIndicatorType();
        }
        else
        {
            // 모든 인디케이터 비활성화
            if (landingIndicator != null)
                landingIndicator.SetActive(false);
            if (landingIndicatorSlam != null)
                landingIndicatorSlam.SetActive(false);
        }
    }
    
    private void UpdateIndicatorType()
    {
        if (!isIndicatorActive) return;
        
        // PlayerController에서 isSlamming 상태 확인
        bool isSlamming = playerController != null && playerController.isSlamming;
        
        if (isSlamming)
        {
            // Slam 상태: 기본 인디케이터 비활성화, Slam 인디케이터 활성화
            if (landingIndicator != null)
                landingIndicator.SetActive(false);
            if (landingIndicatorSlam != null)
                landingIndicatorSlam.SetActive(true);
        }
        else
        {
            // 기본 상태: 기본 인디케이터 활성화, Slam 인디케이터 비활성화
            if (landingIndicator != null)
                landingIndicator.SetActive(true);
            if (landingIndicatorSlam != null)
                landingIndicatorSlam.SetActive(false);
        }
        
    }
    
    private GameObject GetActiveIndicator()
    {
        // 현재 활성화되어야 할 인디케이터 반환
        if (playerController != null && playerController.isSlamming)
        {
            return landingIndicatorSlam;
        }
        else
        {
            return landingIndicator;
        }
    }
    
    /* 공개 메서드 (PlayerController에서 호출 가능) ------------------- */
    
    public void SetIndicatorActive(bool active)
    {
        isIndicatorActive = active;
        UpdateIndicatorVisibility();
    }
    
    public void SetLandingIndicator(GameObject indicator)
    {
        landingIndicator = indicator;
        if (landingIndicator != null)
            landingIndicator.SetActive(false);
    }
    
    public void SetLandingIndicatorSlam(GameObject indicator)
    {
        landingIndicatorSlam = indicator;
        if (landingIndicatorSlam != null)
            landingIndicatorSlam.SetActive(false);
    }
    
    /* SphereCast 디버그 시각화 ------------------------------------ */
    
    private void DrawSphereCastDebug(Vector3 origin, Vector3 direction, float distance, Color color)
    {
        // 중앙 Ray 그리기
        Debug.DrawRay(origin, direction * distance, color);
        
        // SphereCast 원기둥 시각화
        int rayCount = 8;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = (360f / rayCount) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * sphereCastRadius,
                0,
                Mathf.Sin(angle) * sphereCastRadius
            );
            
            Vector3 rayStart = origin + offset;
            Debug.DrawRay(rayStart, direction * distance, color * 0.5f);
        }
        
        // 시작점과 끝점에 원 그리기
        DrawCircleDebug(origin, sphereCastRadius, color * 0.3f);
        DrawCircleDebug(origin + direction * distance, sphereCastRadius, color * 0.3f);
    }
    
    private void DrawCircleDebug(Vector3 center, float radius, Color color)
    {
        int segments = 16;
        Vector3 prevPoint = center + Vector3.right * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );
            
            Debug.DrawLine(prevPoint, newPoint, color);
            prevPoint = newPoint;
        }
    }
}