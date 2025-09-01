// GameFeel Manager
// - 물리 파라미터 및 피드백(카메라 셰이크, 히트스톱 등) 설정을 중앙에서 관리
// - 물리 관련 유틸리티(바운스 실행, 중력 업데이트 등) 제공
// - 피드백 트리거(TriggerFeedbackEffects)를 통해 다른 시스템에 피드백 요청 전달
using UnityEngine;

public class forgamedesign03_GameFeelManager : MonoBehaviour
{
    // 바운스 계수 (Normal, Enhanced 등)
    public static readonly float[] BounceCoefficients = new float[]
    {
        1.0f,  // Normal
        1.5f,  // Enhanced
    };

    // 물리 파라미터 구조체
    [System.Serializable]
    public struct PhysicsSettings
    {
        public float moveForce;           // 이동력
        public float slamForce;           // Slam 충격력
        public float normalUpSpeed;       // 기본 바운스 상승 속도
        public float normalGravityScale;  // 기본 바운스 중력 배율
        public float enhancedUpSpeed;     // 강화 바운스 상승 속도
        public float enhancedGravityScale;// 강화 바운스 중력 배율
        public float baseGravity;         // 기본 중력
        public float globalBounceStrength;// 전체 바운스 강도 배율
    }

    // 기본 물리 설정
    public static readonly PhysicsSettings DefaultPhysics = new PhysicsSettings
    {
        moveForce = 20f,
        slamForce = 25f,
        normalUpSpeed = 12f,
        normalGravityScale = 1f,
        enhancedUpSpeed = 18f,
        enhancedGravityScale = 0.8f,
        baseGravity = 9.81f,
        globalBounceStrength = 1f
    };

    // 피드백 파라미터 구조체
    [System.Serializable]
    public struct FeedbackSettings
    {
        public float hitStopDuration;     // 히트스톱 지속 시간 (초)
        public float camShakeDuration;    // 카메라 셰이크 지속 시간 (초)
        public float camShakeAmplitude;   // 카메라 셰이크 진폭
    }

    // 기본 피드백 설정
    public static readonly FeedbackSettings DefaultFeedback = new FeedbackSettings
    {
        hitStopDuration = 0.10f,
        camShakeDuration = 0.20f,
        camShakeAmplitude = 0.30f
    };

    // 바운스 타입
    public enum BounceType { Normal, Enhanced, Super }

    [Header("Physics Settings")]
    [SerializeField]
    private PhysicsSettings physicsSettings = DefaultPhysics;

    [Header("Feedback Settings")]
    [SerializeField]
    private FeedbackSettings feedbackSettings = DefaultFeedback;

    [Header("Super Bounce Settings")]
    [SerializeField]
    private float superBounceMultiplier = 1.5f;

    // 싱글톤
    public static forgamedesign03_GameFeelManager Instance { get; private set; }

    // 공개 프로퍼티
    public PhysicsSettings Physics => physicsSettings;
    public FeedbackSettings Feedback => feedbackSettings;
    public float SuperBounceMultiplier => superBounceMultiplier;

    private void Awake()
    {
        // 싱글톤 초기화
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 초기 중력 설정 적용
        UnityEngine.Physics.gravity = new Vector3(0, -physicsSettings.baseGravity, 0);
    }

    private void ApplyPhysicsSettings()
    {
        // 중력 즉시 적용
        UnityEngine.Physics.gravity = new Vector3(0, -physicsSettings.baseGravity, 0);
    }

    /* 공개 설정 API */
    public void SetPhysicsSettings(PhysicsSettings newSettings)
    {
        physicsSettings = newSettings;
        ApplyPhysicsSettings();
    }

    public void SetFeedbackSettings(FeedbackSettings newSettings)
    {
        feedbackSettings = newSettings;
    }

    public void SetSuperBounceMultiplier(float multiplier)
    {
        superBounceMultiplier = Mathf.Max(1.0f, multiplier);
    }

    public void ResetToDefaults()
    {
        physicsSettings = DefaultPhysics;
        feedbackSettings = DefaultFeedback;
        superBounceMultiplier = 1.5f;
        ApplyPhysicsSettings();
    }

    /* 바운스 관련 유틸리티 */
    public static void ExecuteBounce(Rigidbody rb, BounceType type, PhysicsSettings physics)
    {
        float upSpeed = GetUpSpeed(type, physics);
        upSpeed *= physics.globalBounceStrength;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            upSpeed,
            rb.linearVelocity.z);

        UpdateGravity(type, physics);
    }

    public static void ExecuteSuperBounce(Rigidbody rb, PhysicsSettings physics)
    {
        ExecuteSuperBounceWithCustomMultiplier(rb, physics, 2.0f);
    }

    public static void ExecuteSuperBounceWithCustomMultiplier(Rigidbody rb, PhysicsSettings physics, float multiplier)
    {
        float enhancedUpSpeed = GetUpSpeed(BounceType.Enhanced, physics);
        float superUpSpeed = enhancedUpSpeed * multiplier;
        superUpSpeed *= physics.globalBounceStrength;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            superUpSpeed,
            rb.linearVelocity.z);

        UpdateGravity(BounceType.Enhanced, physics);
    }

    public static void UpdateGravity(BounceType type, PhysicsSettings physics)
    {
        float gravityScale = GetGravityScale(type, physics);
        float currentGravity = physics.baseGravity * gravityScale;
        UnityEngine.Physics.gravity = new Vector3(0, -currentGravity, 0);
    }

    private static float GetUpSpeed(BounceType type, PhysicsSettings physics)
    {
        return type switch
        {
            BounceType.Normal => physics.normalUpSpeed,
            BounceType.Enhanced => physics.enhancedUpSpeed,
            BounceType.Super => physics.enhancedUpSpeed * 1.5f,
            _ => physics.normalUpSpeed
        };
    }

    private static float GetGravityScale(BounceType type, PhysicsSettings physics)
    {
        return type switch
        {
            BounceType.Normal => physics.normalGravityScale,
            BounceType.Enhanced => physics.enhancedGravityScale,
            BounceType.Super => physics.enhancedGravityScale,
            _ => physics.normalGravityScale
        };
    }

    /* Rigidbody 안정화 유틸리티
       - 외부에서 충돌 후 Rigidbody 안정화가 필요할 때 호출
       - 회전 방지, 댐핑, 고속 충돌 감지 등 설정을 적용 */
    public static void StabilizeRigidbody(Rigidbody rb, Transform transform)
    {
        if (rb == null || transform == null) return;

        rb.freezeRotation = true;
        rb.linearDamping = 2f;
        rb.angularDamping = 10f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        transform.rotation = Quaternion.identity;
    }

    /* 피드백 시스템
       - 외부(예: PlayerController)에서 상황별 FeedbackSettings를 만들어 전달하면
         HitStop 및 CamShake 등을 트리거한다.
       - CamShake로 전달되는 amplitude는 feedback.camShakeAmplitude 값이다. */
    public static void TriggerFeedbackEffects(FeedbackSettings feedback)
    {
        // Debug log added for tracing amplitude flow
        Debug.Log($"[GameFeelManager] TriggerFeedbackEffects requested: duration={feedback.camShakeDuration}, amplitude={feedback.camShakeAmplitude}");

        if (forgamedesign03_HitStop.Instance != null)
        {
            forgamedesign03_HitStop.Instance.TriggerHitStop(feedback.hitStopDuration);
        }

        if (forgamedesign03_CamShake.Instance != null)
        {
            forgamedesign03_CamShake.Instance.TriggerCamShake(feedback.camShakeDuration, feedback.camShakeAmplitude);
        }
    }
}