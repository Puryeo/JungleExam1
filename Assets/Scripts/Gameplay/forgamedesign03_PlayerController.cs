// Prototype Only – Delete or replace in production
using UnityEngine;
using UnityEngine.InputSystem;
using static forgamedesign03_GameFeelManager;

[RequireComponent(typeof(Rigidbody))]
public class forgamedesign03_PlayerController : MonoBehaviour
{
    [Header("GameFeel Manager")]
    [SerializeField, Tooltip("GameFeel 설정 관리자 (없으면 자동 생성)")]
    private forgamedesign03_GameFeelManager gameFeelManager;
    
    [Header("Debug")]
    [SerializeField, Tooltip("현재 Slam 사용 가능 여부 (읽기 전용)")]
    private bool canSlam = false;
    [SerializeField, Tooltip("게임 오버 상태 (읽기 전용)")]
    private bool gameOver = false;
    
    [Header("Checkpoint System")]
    [SerializeField, Tooltip("현재 저장된 체크포인트 위치")]
    private Vector3 checkpointPosition = Vector3.zero;
    [SerializeField, Tooltip("체크포인트 저장 여부")]
    private bool hasCheckpoint = false;
    
    [Header("Fall Detection")]
    [SerializeField, Tooltip("낙사 감지 Y좌표 (이 값 이하로 떨어지면 체크포인트로 복원)")]
    private float fallThresholdY = -30f;
    [SerializeField, Tooltip("낙사 감지 활성화")]
    private bool enableFallDetection = true;

    private Rigidbody rb;
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private BounceType currentBounceType = BounceType.Normal;
    
    // Slam 상태 추적
    [HideInInspector] public bool isSlamming = false;  // 인디케이터에서 접근 가능
    private bool hasSlamCharge = false;
    private float slamStartTime;
    
    // 착지 인디케이터
    private forgamedesign03_LandingIndicator landingIndicator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // Input Actions 초기화
        try
        {
            inputActions = new InputSystem_Actions();
            Debug.Log("InputSystem_Actions 생성 성공");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"InputSystem_Actions 생성 실패: {e.Message}");
            return;
        }
        
        // GameFeel Manager 초기화
        if (gameFeelManager == null)
        {
            gameFeelManager = forgamedesign03_GameFeelManager.Instance;
            if (gameFeelManager == null)
            {
                Debug.LogWarning("GameFeel Manager가 씬에 없습니다. 기본 설정을 사용합니다.");
            }
        }
        
        // 물리 안정화 설정
        StabilizeRigidbody(rb, transform);
        Debug.Log("Rigidbody 안정화 설정 완료");
        
        // 착지 인디케이터 초기화
        landingIndicator = GetComponent<forgamedesign03_LandingIndicator>();
        if (landingIndicator == null)
        {
            landingIndicator = gameObject.AddComponent<forgamedesign03_LandingIndicator>();
        }
    }

    private void OnEnable()
    {
        if (inputActions != null)
        {
            inputActions.Enable();
            Debug.Log("Input Actions 활성화됨");
        }
        else
        {
            Debug.LogError("inputActions가 null입니다! OnEnable에서 활성화 실패");
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    /* Input Update --------------------------------------------- */
    private void Update()
    {
        // Input Actions null 체크
        if (inputActions == null)
        {
            Debug.LogError("inputActions가 null입니다! Update에서 입력 처리 불가");
            return;
        }

        try
        {
            // 이동 입력 직접 읽기
            moveInput = inputActions.PlayerControls.Move.ReadValue<Vector2>();
            
            // Slam 입력 체크 (hasSlamCharge가 true일 때만 가능)
            if (inputActions.PlayerControls.Slam.WasPressedThisFrame() && hasSlamCharge)
            {
                ExecuteSlam();
            }
            else if (inputActions.PlayerControls.Slam.WasPressedThisFrame() && !hasSlamCharge)
            {
                Debug.Log("Slam 불가능! 충전이 필요합니다. Enhanced Bounce로 충전하세요!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"입력 처리 중 오류: {e.Message}");
        }
        
        // 중력 동적 조절
        var physics = GetPhysicsSettings();
        UpdateGravity(currentBounceType, physics);
        
        // 인스펙터에 canSlam 상태 표시 (디버그용)
        canSlam = hasSlamCharge;
        
        // 착지 인디케이터 상태 업데이트
        UpdateLandingIndicator();
        
        // 낙사 감지
        CheckFallDetection();
    }

    /* Physics Update ----------------------------------------------- */
    private void FixedUpdate()
    {
        if (moveInput.sqrMagnitude > 0.001f)
        {
            // 질량 중심에 힘을 가해서 회전 방지
            var physics = GetPhysicsSettings();
            Vector3 force = new Vector3(moveInput.x, 0, moveInput.y) * physics.moveForce;
            rb.AddForce(force, ForceMode.Acceleration);
            
            // 추가 안정화: 회전을 강제로 0으로 설정
            transform.rotation = Quaternion.identity;
        }
    }

    /* Slam System ---------------------------------------------- */
    private void ExecuteSlam()
    {
        // Slam 충전 소모하여 Slam 실행
        var physics = GetPhysicsSettings();
        hasSlamCharge = false;
        isSlamming = true;
        slamStartTime = Time.time;
        Debug.Log($"Slam 실행! (충전 소모, 힘: {physics.slamForce})");
        rb.AddForce(Vector3.down * physics.slamForce, ForceMode.Impulse);
        // slam VFX/SFX 트리거 위치
    }

    /* Collision ---------------------------------------------------- */
    private void OnCollisionEnter(Collision c)
    {
        // 모든 충돌 지점에서 충돌체들을 검사
        GameObject bossTarget = null;
        GameObject superZombieTarget = null;
        GameObject zombieTarget = null;
        GameObject bouncableTarget = null;
        
        // 충돌한 모든 ContactPoint를 검사
        foreach (ContactPoint contact in c.contacts)
        {
            Collider hitCollider = contact.otherCollider;
            
            if (hitCollider.CompareTag("Boss"))
            {
                bossTarget = hitCollider.gameObject;
            }
            else if (hitCollider.CompareTag("SuperZombie"))
            {
                superZombieTarget = hitCollider.gameObject;
            }
            else if (hitCollider.CompareTag("Zombie"))
            {
                zombieTarget = hitCollider.gameObject;
            }
            else if (hitCollider.CompareTag("Bouncable"))
            {
                bouncableTarget = hitCollider.gameObject;
            }
        }
        
        // 추가로 메인 충돌체도 검사 (ContactPoint에 포함되지 않을 수 있음)
        if (c.collider.CompareTag("Boss"))
        {
            bossTarget = c.gameObject;
        }
        else if (c.collider.CompareTag("SuperZombie"))
        {
            superZombieTarget = c.gameObject;
        }
        else if (c.collider.CompareTag("Zombie"))
        {
            zombieTarget = c.gameObject;
        }
        else if (c.collider.CompareTag("Bouncable"))
        {
            bouncableTarget = c.gameObject;
        }
        
        // 우선순위에 따른 충돌 처리
        if (bossTarget != null)
        {
            // Boss 최고 우선순위 처리
            Debug.Log("Boss 충돌 처리!");
            HandleBossCollision(bossTarget);
            return;
        }
        else if (superZombieTarget != null)
        {
            // SuperZombie 우선 처리
            Debug.Log("SuperZombie 우선 충돌 처리!");
            HandleZombieCollision(superZombieTarget, true);
            return;
        }
        else if (zombieTarget != null)
        {
            // 일반 Zombie 처리
            Debug.Log("Zombie 충돌 처리!");
            HandleZombieCollision(zombieTarget, false);
            return;
        }
        else if (bouncableTarget != null)
        {
            // 일반 Bouncable 충돌 (Slam 충전 초기화)
            Debug.Log("Bouncable 충돌 처리!");
            var physics = GetPhysicsSettings();
            ExecuteBounce(rb, BounceType.Normal, physics);
            hasSlamCharge = false;  // Slam 충전 초기화
            isSlamming = false;
            currentBounceType = BounceType.Normal;
            Debug.Log("Bouncable 충돌 - Slam 충전 초기화됨");
            
            // 착지 후 회전 보정
            StabilizeRigidbody(rb, transform);
        }
    }
    
    private void HandleZombieCollision(GameObject zombie, bool isSuperZombie = false)
    {
        // Slam 상태인지 확인
        string zombieType = isSuperZombie ? "SuperZombie" : "Zombie";
        Debug.Log($"Slam 판정: isSlamming={isSlamming}, 타겟={zombieType}");
        
        var physics = GetPhysicsSettings();
        var feedback = GetFeedbackSettings();
        
        if (isSlamming)
        {
            // Slam으로 Zombie/SuperZombie 처치
            Debug.Log($"Slam으로 {zombieType} 처치!");
            
            // HitStop & CamShake 트리거
            TriggerFeedbackEffects(feedback);
            
            // Zombie 파괴
            Destroy(zombie);
            
            // 속도 초기화 후 Enhanced Bounce 실행 + Slam 충전
            rb.linearVelocity = Vector3.zero;
            Debug.Log("속도 초기화 후 Enhanced Bounce 실행");
            
            if (isSuperZombie)
            {
                // SuperZombie: Super Bounce (Enhanced의 1.5배)
                ExecuteSuperBounceWithMultiplier(rb, physics);
                currentBounceType = BounceType.Super;
                Debug.Log("Super Bounce 실행! (Enhanced Bounce의 1.5배 높이)");
            }
            else
            {
                // 일반 Zombie: Enhanced Bounce
                ExecuteBounce(rb, BounceType.Enhanced, physics);
                currentBounceType = BounceType.Enhanced;
                Debug.Log("Enhanced Bounce 실행");
            }
            
            hasSlamCharge = true;  // Slam 충전
            Debug.Log($"{zombieType} 처치로 Slam 충전 완료!");
            isSlamming = false;  // Slam 상태 종료
        }
        else
        {
            // 일반 Zombie/SuperZombie 충돌 - Slam 충전
            Debug.Log($"{zombieType}에 일반 충돌!");
            
            if (isSuperZombie)
            {
                // SuperZombie: Super Bounce (Enhanced의 1.5배) - 충돌 시에는 Enhanced로 처리
                ExecuteBounce(rb, BounceType.Enhanced, physics);
                currentBounceType = BounceType.Enhanced;
                Debug.Log("SuperZombie 충돌 - Enhanced Bounce로 Slam 충전!");
            }
            else
            {
                // 일반 Zombie: Enhanced Bounce
                ExecuteBounce(rb, BounceType.Enhanced, physics);
                currentBounceType = BounceType.Enhanced;
                Debug.Log("Enhanced Bounce로 Slam 충전!");
            }
            
            hasSlamCharge = true;  // Slam 충전
            isSlamming = false;  // 확실히 false로 설정
        }
        
        // 착지 후 회전 보정
        StabilizeRigidbody(rb, transform);
    }
    
    private void HandleBossCollision(GameObject boss)
    {
        Debug.Log($"Boss 충돌! Slam 판정: isSlamming={isSlamming}");
        
        var physics = GetPhysicsSettings();
        var feedback = GetFeedbackSettings();
        
        if (isSlamming)
        {
            // Slam으로 Boss 처치 - 게임 클리어!
            Debug.Log("🎉 BOSS 처치! 게임 클리어!");
            
            // 강력한 피드백 효과 (Boss 처치용)
            var bossFeedback = new FeedbackSettings
            {
                hitStopDuration = feedback.hitStopDuration * 2f,  // 2배 길게
                camShakeDuration = feedback.camShakeDuration * 2f,  // 2배 길게
                camShakeAmplitude = feedback.camShakeAmplitude * 1.5f  // 1.5배 강하게
            };
            TriggerFeedbackEffects(bossFeedback);
            
            // Boss 파괴
            Destroy(boss);
            
            // 속도 초기화 후 Super Bounce 실행
            rb.linearVelocity = Vector3.zero;
            ExecuteSuperBounceWithMultiplier(rb, physics);
            currentBounceType = BounceType.Super;
            
            hasSlamCharge = true;  // Slam 충전 유지
            isSlamming = false;    // Slam 상태 종료
            
            // 게임 오버 (승리) 설정
            gameOver = true;
            Debug.Log("🏆 게임 클리어! gameOver = true");
            
            // 게임 클리어 처리 (추후 GameManager나 UI에서 처리 가능)
            HandleGameClear();
        }
        else
        {
            // Boss와 일반 충돌 - Enhanced Bounce로 Slam 충전
            Debug.Log("Boss 일반 충돌 - Enhanced Bounce로 Slam 충전!");
            
            ExecuteBounce(rb, BounceType.Enhanced, physics);
            hasSlamCharge = true;
            currentBounceType = BounceType.Enhanced;
            isSlamming = false;
        }
        
        // 착지 후 회전 보정
        StabilizeRigidbody(rb, transform);
    }
    
    private void HandleGameClear()
    {
        Debug.Log("=== 🎉 게임 클리어! ===");
        Debug.Log("Boss를 처치했습니다!");
        
        // 추후 GameManager나 UI 시스템에서 처리할 수 있는 이벤트
        // 예: GameManager.Instance?.OnGameClear();
        // 예: UIManager.Instance?.ShowGameClearUI();
        
        // 현재는 콘솔에만 출력
        Debug.Log("게임을 다시 시작하려면 씬을 다시 로드하세요.");
        
        // 선택사항: 게임 일시 정지
        // Time.timeScale = 0f;
    }
    
    /* Checkpoint System ------------------------------------------- */
    
    public void SaveCheckpoint(Vector3 position)
    {
        checkpointPosition = position;
        hasCheckpoint = true;
        
        Debug.Log($"💾 [PlayerController] 체크포인트 저장: {position}");
        
        // 체크포인트 저장 시 피드백 (옵션)
        // 예: 짧은 HitStop이나 특별한 사운드 재생
        if (gameFeelManager != null)
        {
            var checkpointFeedback = new FeedbackSettings
            {
                hitStopDuration = 0.05f,  // 매우 짧은 HitStop
                camShakeDuration = 0.1f,
                camShakeAmplitude = 0.1f
            };
            TriggerFeedbackEffects(checkpointFeedback);
        }
    }
    
    public void RestoreToCheckpoint()
    {
        if (!hasCheckpoint)
        {
            Debug.LogWarning("⚠️ [PlayerController] 저장된 체크포인트가 없습니다!");
            return;
        }
        
        Debug.Log($"🔄 [PlayerController] 체크포인트로 복원: {checkpointPosition}");
        
        // 속도 초기화
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // 위치 복원
        transform.position = checkpointPosition;
        
        // 상태 초기화
        isSlamming = false;
        currentBounceType = BounceType.Normal;
        
        // 물리 안정화
        StabilizeRigidbody(rb, transform);
        
        // 복원 피드백 효과
        if (gameFeelManager != null)
        {
            var restoreFeedback = new FeedbackSettings
            {
                hitStopDuration = 0.15f,
                camShakeDuration = 0.3f,
                camShakeAmplitude = 0.2f
            };
            TriggerFeedbackEffects(restoreFeedback);
        }
        
        Debug.Log("✅ [PlayerController] 체크포인트 복원 완료!");
    }
    
    public void ClearCheckpoint()
    {
        checkpointPosition = Vector3.zero;
        hasCheckpoint = false;
        Debug.Log("🗑️ [PlayerController] 체크포인트 삭제됨");
    }
    
    /* 체크포인트 테스트용 메서드 (개발용) */
    [ContextMenu("체크포인트로 복원 (테스트)")]
    private void TestRestoreToCheckpoint()
    {
        RestoreToCheckpoint();
    }
    
    [ContextMenu("낙사 테스트 (강제 Y좌표 -50)")]
    private void TestFallDetection()
    {
        transform.position = new Vector3(transform.position.x, -50f, transform.position.z);
        Debug.Log("🪂 [Test] 낙사 테스트 실행 - Y좌표를 -50으로 설정");
    }

    /* Enhanced Bounce Trigger ------------------------------------ */
    public void TriggerEnhancedBounce()
    {
        Debug.Log("Enhanced Bounce 트리거됨!");
        var physics = GetPhysicsSettings();
        ExecuteBounce(rb, BounceType.Enhanced, physics);
        hasSlamCharge = true;
        currentBounceType = BounceType.Enhanced;
        Debug.Log("Enhanced Bounce로 Slam 충전 완료!");
    }
    
    /* Landing Indicator ------------------------------------------- */
    private void UpdateLandingIndicator()
    {
        if (landingIndicator == null) return;
        
        // Enhanced Bounce 상태이고 공중에 있을 때만 인디케이터 활성화
        bool shouldShowIndicator = hasSlamCharge && IsInAir();
        landingIndicator.SetIndicatorActive(shouldShowIndicator);
    }
    
    private bool IsInAir()
    {
        // 간단한 공중 체크 (지면에서 약간 떨어져 있거나 위쪽 속도가 있을 때)
        float groundCheckDistance = 0.2f;
        Vector3 rayOrigin = transform.position + Vector3.down * 0.5f;
        
        bool isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance);
        bool hasUpwardVelocity = rb.linearVelocity.y > 0.1f;
        
        return !isGrounded || hasUpwardVelocity;
    }
    
    /* Fall Detection ---------------------------------------------- */
    private void CheckFallDetection()
    {
        if (!enableFallDetection) return;
        
        // Y좌표가 임계값 이하로 떨어졌는지 확인
        if (transform.position.y <= fallThresholdY)
        {
            Debug.Log($"🪂 [PlayerController] 낙사 감지! Y좌표: {transform.position.y} (임계값: {fallThresholdY})");
            
            // 체크포인트가 있으면 복원
            if (hasCheckpoint)
            {
                Debug.Log("🔄 [PlayerController] 낙사로 인한 체크포인트 복원");
                RestoreToCheckpoint();
            }
            else
            {
                // 체크포인트가 없는 경우 기본 위치로 이동
                HandleFallWithoutCheckpoint();
            }
        }
    }
    
    private void HandleFallWithoutCheckpoint()
    {
        Debug.LogWarning("⚠️ [PlayerController] 체크포인트가 없어 시작 위치로 이동합니다!");
        
        // 기본 시작 위치 (0, 5, 0)로 이동
        Vector3 defaultSpawnPosition = new Vector3(0f, 5f, 0f);
        
        // 속도 초기화
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        
        // 위치 설정
        transform.position = defaultSpawnPosition;
        
        // 상태 초기화
        isSlamming = false;
        currentBounceType = BounceType.Normal;
        
        // 물리 안정화
        StabilizeRigidbody(rb, transform);
        
        // 낙사 피드백 효과
        if (gameFeelManager != null)
        {
            var fallFeedback = new FeedbackSettings
            {
                hitStopDuration = 0.2f,
                camShakeDuration = 0.4f,
                camShakeAmplitude = 0.25f
            };
            TriggerFeedbackEffects(fallFeedback);
        }
        
        Debug.Log($"✅ [PlayerController] 시작 위치로 복원 완료: {defaultSpawnPosition}");
    }
    
    /* Helper 메서드들 --------------------------------------------- */
    private PhysicsSettings GetPhysicsSettings()
    {
        return gameFeelManager != null ? gameFeelManager.Physics : DefaultPhysics;
    }
    
    private FeedbackSettings GetFeedbackSettings()
    {
        return gameFeelManager != null ? gameFeelManager.Feedback : DefaultFeedback;
    }
    
    private void ExecuteSuperBounceWithMultiplier(Rigidbody rb, PhysicsSettings physics)
    {
        float multiplier = gameFeelManager != null ? gameFeelManager.SuperBounceMultiplier : 1.5f;
        forgamedesign03_GameFeelManager.ExecuteSuperBounceWithCustomMultiplier(rb, physics, multiplier);
    }
    
    /* 공개 프로퍼티 (인디케이터에서 접근 가능) ---------------------- */
    public bool HasSlamCharge => hasSlamCharge;
    public bool IsGameOver => gameOver;
    public bool HasCheckpoint => hasCheckpoint;
    public Vector3 CheckpointPosition => checkpointPosition;
}