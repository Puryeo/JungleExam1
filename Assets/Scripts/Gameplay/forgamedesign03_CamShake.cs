// Prototype Only – Delete or replace in production
using UnityEngine;
using System.Collections;

/// forgamedesign03_CamShake
/// - 카메라 흔들림(셰이크)을 생성하고 forgamedesign03_CameraController에 오프셋을 전달하는 컴포넌트
/// - 위치 쉐이크만 처리(회전 쉐이크는 현 구조상 미지원)
/// - Time.unscaledDeltaTime을 사용해 히트스톱/일시정지 중에도 흔들림 유지
public class forgamedesign03_CamShake : MonoBehaviour
{
    [Header("CamShake Settings")]
    [SerializeField, Tooltip("카메라 흔들림 지속 시간 (초)")]
    private float shakeDuration = 0.15f;  // 지속 시간 기본값
    [SerializeField, Tooltip("카메라 흔들림 진폭")]
    private float shakeAmplitude = 0.3f;  // 진폭 기본값
    [SerializeField, Tooltip("흔들림 주파수 (높을수록 빠른 진동)")]
    private float shakeFrequency = 30f;
    [SerializeField, Tooltip("진폭 감쇠 곡선 (흔들림이 줄어드는 정도)")]
    private AnimationCurve shakeDecayCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    [Header("Shake Intensity")]
    [SerializeField, Tooltip("X축 흔들림 세기")]
    private float xShakeIntensity = 2f;
    [SerializeField, Tooltip("Y축 흔들림 세기")]
    private float yShakeIntensity = 2f;
    [SerializeField, Tooltip("Z축 흔들림 세기")]
    private float zShakeIntensity = 1f;
    
    [Header("Advanced Settings")]
    [SerializeField, Tooltip("흔들림 강도 배율")]
    private float globalShakeMultiplier = 1f;
    [SerializeField, Tooltip("부드러운 흔들림 (Perlin 노이즈 기반, false면 매 프레임 랜덤)")]
    private bool usePerlinNoise = true;
    [SerializeField, Tooltip("랜덤 시드 (흔들림 패턴)")]
    private float randomSeed = 0f;
    
    [Header("Direction Control")]
    [SerializeField, Tooltip("수평 성분 비율 (0 = 수평 없음, 1 = 그대로)")]

    private float horizontalMultiplier = 0f; // 기본: 0 => 수평 제거, 아래로만 흔들림
    [SerializeField, Tooltip("수직 성분을 항상 아래로 강제 (true면 Y는 음수로 고정)")]
    private bool forceDownward = true;
    
    // 싱글톤 인스턴스
    private static forgamedesign03_CamShake instance;
    // 코루틴 실행 여부 플래그
    private bool isShaking = false;
    // 카메라 컨트롤러 참조 (SetShakeOffset 호출용)
    private forgamedesign03_CameraController cameraController;
    
    // 전역 접근용 싱글톤
    // - 씬에 없으면 CameraController가 있는 오브젝트에 자동으로 추가
    public static forgamedesign03_CamShake Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<forgamedesign03_CamShake>();
                if (instance == null)
                {
                    // CameraController가 있는 오브젝트에 자동 추가 시도
                    forgamedesign03_CameraController cameraController = FindFirstObjectByType<forgamedesign03_CameraController>();
                    if (cameraController != null)
                    {
                        instance = cameraController.gameObject.AddComponent<forgamedesign03_CamShake>();
                    }
                }
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        // 싱글톤 보장 및 CameraController 참조 획득
        if (instance == null)
        {
            instance = this;
            
            // CameraController 참조: 같은 오브젝트 → 씬 전체 순서로 시도
            cameraController = GetComponent<forgamedesign03_CameraController>();
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<forgamedesign03_CameraController>();
            }
            
            if (cameraController != null)
            {
                Debug.Log($"[CamShake] CameraController 연결 성공: {cameraController.gameObject.name}");
            }
            else
            {
                Debug.LogError("[CamShake] CameraController를 찾을 수 없습니다!");
            }
            
            // 하ard코딩된 기본값 강제 설정 (인스펙터 무시)
            horizontalMultiplier = 0f; // 수평 성분 제거
            forceDownward = true;      // 항상 아래로 흔들림
        }
        else if (instance != this)
        {
            // 중복 인스턴스 방지
            Destroy(this);
        }
    }
    
    /* CamShake 실행 진입점 ---------------------------------------- */
    
    // 기본 설정(shakeDuration, shakeAmplitude)로 흔들림 시작
    public void TriggerCamShake()
    {
        Debug.Log("[CamShake] TriggerCamShake() 호출됨 - 기본 설정");
        StartCoroutine(CamShakeCoroutine(shakeDuration, shakeAmplitude));
    }
    
    // 커스텀 지속시간/진폭으로 흔들림 시작
    public void TriggerCamShake(float duration, float amplitude)
    {
        Debug.Log($"[CamShake] TriggerCamShake({duration}, {amplitude}) 호출됨");
        StartCoroutine(CamShakeCoroutine(duration, amplitude));
    }
    
    // 공용 코루틴 래퍼: 유효성 검사 → 실행 → 종료 처리
    private IEnumerator CamShakeCoroutine(float duration, float amplitude)
    {
        if (cameraController == null) 
        {
            Debug.LogError("[CamShake] cameraController가 null입니다! 흔들림을 실행할 수 없습니다.");
            yield break;
        }
        
        Debug.Log($"[CamShake] 흔들림 시작: duration={duration}, amplitude={amplitude}");
        isShaking = true;
        
        // CameraController와 연동하여 흔들림 처리
        yield return StartCoroutine(ExecuteShake(duration, amplitude));
        
        isShaking = false;
        Debug.Log("[CamShake] 흔들림 종료");
    }
    
    // 실제 흔들림 생성 루프
    // - Perlin 노이즈 또는 Random을 사용해 매 프레임 오프셋 생성
    // - 감쇠 곡선(shakeDecayCurve)로 시간에 따라 진폭을 줄임
    // - cameraController.SetShakeOffset()을 통해 위치 쉐이크를 적용
    // - Time.unscaledDeltaTime 사용(슬로우/일시정지 영향 최소화)
    private IEnumerator ExecuteShake(float duration, float amplitude)
    {
        float elapsedTime = 0f;
        float initialSeed = randomSeed + Time.time;
        
        while (elapsedTime < duration)
        {
            float normalizedTime = elapsedTime / duration;
            
            // 감쇠 곡선으로 현재 프레임의 진폭 산출
            float decayFactor = shakeDecayCurve.Evaluate(normalizedTime);
            float currentAmplitude = amplitude * decayFactor * globalShakeMultiplier;
            
            Vector3 shakeOffset;
            
            if (usePerlinNoise)
            {
                // Perlin 노이즈 기반 부드러운 흔들림(연속성 보장)
                float xNoise = (Mathf.PerlinNoise(initialSeed + elapsedTime * shakeFrequency, 0f) - 0.5f) * 2f;
                float yNoise = (Mathf.PerlinNoise(0f, initialSeed + elapsedTime * shakeFrequency) - 0.5f) * 2f;
                float zNoise = (Mathf.PerlinNoise(initialSeed + elapsedTime * shakeFrequency, initialSeed + elapsedTime * shakeFrequency) - 0.5f) * 2f;
                
                shakeOffset = new Vector3(
                    xNoise * currentAmplitude * xShakeIntensity,
                    yNoise * currentAmplitude * yShakeIntensity,
                    zNoise * currentAmplitude * zShakeIntensity
                );
            }
            else
            {
                // 프레임별 랜덤 기반 흔들림(노이즈 대비 불연속적)
                shakeOffset = new Vector3(
                    Random.Range(-1f, 1f) * currentAmplitude * xShakeIntensity,
                    Random.Range(-1f, 1f) * currentAmplitude * yShakeIntensity,
                    Random.Range(-1f, 1f) * currentAmplitude * zShakeIntensity
                );
            }
            
            // 방향 제어: 수평 성분 감쇄, 아래로 강제 등
            if (horizontalMultiplier != 1f)
            {
                shakeOffset.x *= horizontalMultiplier;
                shakeOffset.z *= horizontalMultiplier;
            }
            if (forceDownward)
            {
                shakeOffset.y = -Mathf.Abs(shakeOffset.y);
            }
            
            // CameraController에 흔들림 오프셋 적용
            // - CameraController는 LateUpdate에서 위치 보간 후 이 오프셋을 가산하므로,
            //   흔들림이 SmoothDamp/Lerp에 의해 희석되지 않음.
            cameraController.SetShakeOffset(shakeOffset);
            
            // 첫 프레임 디버그 확인(옵션)
            if (elapsedTime < 0.02f)
            {
                Debug.Log($"[CamShake] 흔들림 오프셋 적용됨: {shakeOffset} (진폭: {currentAmplitude}, 경과 시간: {elapsedTime})");
            }
            
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // 흔들림 종료: 오프셋 원복
        cameraController.SetShakeOffset(Vector3.zero);
        Debug.Log("[CamShake] 흔들림 오프셋 제거됨");
    }
    
    /* 유틸리티 ---------------------------------------------------- */
    
    // 현재 흔들림 동작 중인지 여부
    public bool IsShaking()
    {
        return isShaking;
    }
    
    // 강제 중단(오프셋 초기화 포함)
    public void StopCamShake()
    {
        if (isShaking)
        {
            StopAllCoroutines();
            
            if (cameraController != null)
            {
                cameraController.SetShakeOffset(Vector3.zero);
            }
            
            isShaking = false;
        }
    }
}