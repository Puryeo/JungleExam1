// Prototype Only – Delete or replace in production
using UnityEngine;

// 단순화된 3인칭 카메라 컨트롤러
// - 플레이어를 기준으로 (뒤로 distance, 위로 height) 위치를 계산해 따라감
// - 부드러운 이동(SmoothDamp) 또는 선형 보간(Lerp) 선택
// - lookAtTarget이 true면 타겟을 바라본 뒤 X축 추가 각도 적용(Slerp 보간)
// - 강화 바운스(HasSlamCharge) 상태에서는 X축 추가 각도를 더해 더 아래를 보도록 함
// - 흔들림(shakeOffset)을 최종 위치에 가산
// - 에디터 편의: OnValidate로 실행 중 값 변경 시 X축 회전 즉시 반영, Gizmos로 시각화
public class forgamedesign03_CameraController : MonoBehaviour
{
    // [Target Settings] ------------------------------------------------
    [SerializeField, Tooltip("따라다닐 플레이어 오브젝트")]
    private Transform target;
    [SerializeField, Tooltip("플레이어를 자동으로 찾기")]
    private bool autoFindPlayer = true;

    // [3rd Person Camera Position] ------------------------------------
    [SerializeField, Tooltip("플레이어와의 거리")]
    private float distance = 8f;
    [SerializeField, Tooltip("카메라 높이 (플레이어 기준)")]
    private float height = 6f;

    // [Camera Movement] -----------------------------------------------
    [SerializeField, Tooltip("카메라 따라가기 속도")]
    private float followSpeed = 5f;
    [SerializeField, Tooltip("카메라 회전 속도")]
    private float rotationSpeed = 10f;
    [SerializeField, Tooltip("부드러운 움직임 사용 여부 (true: SmoothDamp, false: Lerp)")]
    private bool smoothMovement = true;

    // [Look At Settings] ----------------------------------------------
    [SerializeField, Tooltip("플레이어를 바라보기 (false: X축 회전값으로 직접 제어)")]
    private bool lookAtTarget = false;
    [SerializeField, Tooltip("바라보기 오프셋 (플레이어 기준)")]
    private Vector3 lookAtOffset = new Vector3(0, 2, 0);

    // [Camera Rotation] ------------------------------------------------
    [SerializeField, Tooltip("기본 X축 회전 - 위아래 각도 (0=수평, +값=아래쪽, -값=위쪽)")]
    private float baseRotationX = 15f;

    // [Enhanced Bounce] -----------------------------------------------
    [SerializeField, Tooltip("Enhanced Bounce 상태에서 추가할 X축 회전 (아래쪽 각도)")]
    private float enhancedBounceExtraRotationX = 30f;

    // [Camera Shake] ---------------------------------------------------
    // 외부에서 제공된 오프셋(월드 단위) — SetShakeOffset에서 보정하여 사용
    private Vector3 shakeOffset = Vector3.zero;

    // [Internal State] -------------------------------------------------
    private Camera cameraComponent;
    private Vector3 velocity = Vector3.zero; // SmoothDamp 속도 캐시
    private forgamedesign03_PlayerController playerController; // 강화 바운스 상태 조회용

    private void Awake()
    {
        // 카메라 컴포넌트 캐시
        cameraComponent = GetComponent<Camera>();

        // 플레이어 자동 찾기
        if (autoFindPlayer && target == null)
        {
            FindPlayer();
        }
    }

    // 에디터에서 값 수정 시, 실행 중이고 lookAtTarget이 꺼져 있으면 X축 회전을 즉시 반영
    private void OnValidate()
    {
        if (Application.isPlaying && !lookAtTarget)
        {
            float currentRotationX = GetCurrentRotationX();
            transform.rotation = Quaternion.Euler(currentRotationX, 0f, 0f);
        }
    }

    private void Start()
    {
        if (target == null) return;

        // 초기 위치/회전 설정
        SetInitialPosition();

        // lookAtTarget이 꺼져 있으면 X축 회전만 즉시 적용
        if (!lookAtTarget)
        {
            float currentRotationX = GetCurrentRotationX();
            transform.rotation = Quaternion.Euler(currentRotationX, 0f, 0f);
        }
    }

    private void LateUpdate()
    {
        // 목표 타겟이 없는 경우에도 shakeOffset은 적용되도록 처리
        if (target == null)
        {
            // 타겟이 없으면 위치 보간은 하지 않고, 현재 transform에 셰이크만 적용
            if (shakeOffset != Vector3.zero)
            {
                // 화면(카메라) 기준으로 적용
                transform.position += transform.TransformDirection(shakeOffset);
            }
            return;
        }

        // 매 프레임 위치/회전 갱신
        Update3rdPersonCamera();
    }

    // forgamedesign03_PlayerController를 찾아 target/참조를 설정
    private void FindPlayer()
    {
        playerController = FindFirstObjectByType<forgamedesign03_PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
        }
    }

    // 초기 카메라 위치/회전 설정
    private void SetInitialPosition()
    {
        Vector3 initialPosition = Calculate3rdPersonPosition();
        transform.position = initialPosition;

        if (lookAtTarget)
        {
            // 타겟을 바라보는 기본 각도 설정
            Vector3 lookAtPosition = target.position + lookAtOffset;
            transform.LookAt(lookAtPosition);
        }
    }

    // 3인칭 카메라 위치/회전 갱신 (LateUpdate에서 호출)
    private void Update3rdPersonCamera()
    {
        // 목표 위치 계산
        Vector3 targetPosition = Calculate3rdPersonPosition();

        // 위치 보간(SmoothDamp or Lerp)
        Vector3 finalPosition = smoothMovement
            ? Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 1f / followSpeed)
            : Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // 먼저 기본 위치 적용 (셰이크는 회전 계산 이후에 화면 기준으로 적용)
        transform.position = finalPosition;

        // 회전 계산
        float currentRotationX = GetCurrentRotationX();

        if (lookAtTarget)
        {
            // 타겟을 바라본 후 추가 X축 회전을 곱해 최종 회전
            Vector3 lookAtPosition = target.position + lookAtOffset;
            Vector3 direction = lookAtPosition - transform.position;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                Quaternion additionalRotation = Quaternion.Euler(currentRotationX, 0f, 0f);
                Quaternion targetRotation = lookRotation * additionalRotation;

                // 회전 보간
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // X축 각도만 직접 적용
            Quaternion targetRotation = Quaternion.Euler(currentRotationX, 0f, 0f);
            transform.rotation = targetRotation;
        }

        // 회전 계산이 끝난 후에 카메라 로컬 기준으로 셰이크 적용
        if (shakeOffset != Vector3.zero)
        {
            transform.position += transform.TransformDirection(shakeOffset);
        }
    }

    // 타겟 기준의 3인칭 카메라 위치 계산(뒤로 distance, 위로 height)
    private Vector3 Calculate3rdPersonPosition()
    {
        if (target == null) return transform.position;

        Vector3 targetPosition = target.position;
        Vector3 basePosition = targetPosition + Vector3.back * distance + Vector3.up * height;
        return basePosition;
    }

    // 현재 X축 회전 각도 계산
    // - 기본값(baseRotationX)에 강화 바운스 상태면 추가 각도(enhancedBounceExtraRotationX)를 더함
    private float GetCurrentRotationX()
    {
        float currentRotationX = baseRotationX;

        if (playerController != null && playerController.HasSlamCharge)
        {
            currentRotationX += enhancedBounceExtraRotationX;
        }

        return currentRotationX;
    }

    /* 공개 메서드 (외부에서 파라미터 제어) --------------------------- */

    // 추적 대상 설정
    public void SetTarget(Transform newTarget) => target = newTarget;

    // 카메라-타겟 거리 설정(최소 1)
    public void SetDistance(float newDistance) => distance = Mathf.Max(1f, newDistance);

    // 카메라 높이 설정
    public void SetHeight(float newHeight) => height = newHeight;

    // 따라가기 속도 설정(최소 0.1)
    public void SetFollowSpeed(float newSpeed) => followSpeed = Mathf.Max(0.1f, newSpeed);

    // 기본 X축 회전 각도 설정
    public void SetBaseRotationX(float rotationX) => baseRotationX = rotationX;

    // 강화 바운스 추가 X축 회전 각도 설정
    public void SetEnhancedBounceExtraRotationX(float extraRotationX) => enhancedBounceExtraRotationX = extraRotationX;

    // 카메라 흔들림 오프셋 설정
    // - 빌드/에디터 간 일관된 화면 셰이크를 위해 카메라 거리 및 FOV를 기준으로 보정
    public void SetShakeOffset(Vector3 offset)
    {
        if (cameraComponent == null)
        {
            shakeOffset = offset;
            return;
        }

        // 기준값: 에디터에서 테스트한 기본 distance/FOV에 맞춰 조정 가능
        const float referenceDistance = 8f; // 튜닝 필요 시 프로젝터 기본값으로 변경
        const float referenceFOV = 60f;

        // 거리 및 FOV 기반 스케일링: 거리가 멀면 world-space offset은 화면상 작게 보이므로 보정
        float distScale = referenceDistance / Mathf.Max(0.001f, distance);
        float fovScale = referenceFOV / Mathf.Max(1f, cameraComponent.fieldOfView);
        float scale = Mathf.Clamp(distScale * fovScale, 0.1f, 10f);

        shakeOffset = offset * scale;
    }
}