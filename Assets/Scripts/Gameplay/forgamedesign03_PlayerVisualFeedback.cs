// Prototype Only – Delete or replace in production
using UnityEngine;

public class forgamedesign03_PlayerVisualFeedback : MonoBehaviour
{
    [Header("Player Material Settings")]
    [SerializeField, Tooltip("기본 상태 플레이어 머티리얼")]
    private Material defaultPlayerMaterial;
    [SerializeField, Tooltip("Slam 상태 플레이어 머티리얼 (빨간색/주황색 등)")]
    private Material slammingPlayerMaterial;
    
    [Header("Target Renderer")]
    [SerializeField, Tooltip("플레이어 Renderer (자동 찾기, 수동 할당 가능)")]
    private Renderer playerRenderer;
    
    [Header("Debug")]
    [SerializeField, Tooltip("머티리얼 변경 디버그 로그")]
    private bool showMaterialDebug = true;
    
    private forgamedesign03_PlayerController playerController;
    private bool wasSlammingLastFrame = false;
    
    private void Awake()
    {
        // PlayerController 컴포넌트 찾기
        playerController = GetComponent<forgamedesign03_PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("PlayerController를 찾을 수 없습니다!");
            return;
        }
        
        // Player Renderer 자동 찾기 (할당되지 않은 경우)
        if (playerRenderer == null)
        {
            playerRenderer = GetComponent<Renderer>();
            if (playerRenderer == null)
            {
                Debug.LogError("Player Renderer를 찾을 수 없습니다! Inspector에서 수동 할당해주세요.");
                return;
            }
        }
        
        // 초기 머티리얼 설정
        SetupInitialMaterial();
        
        Debug.Log("Player Visual Feedback 초기화 완료");
    }
    
    private void SetupInitialMaterial()
    {
        // 기본 머티리얼이 할당되지 않은 경우 현재 머티리얼 사용
        if (defaultPlayerMaterial == null && playerRenderer != null)
        {
            defaultPlayerMaterial = playerRenderer.material;
            Debug.Log($"기본 머티리얼 자동 설정: {defaultPlayerMaterial.name}");
        }
        
        // 초기 상태는 기본 머티리얼
        if (defaultPlayerMaterial != null && playerRenderer != null)
        {
            playerRenderer.material = defaultPlayerMaterial;
        }
    }
    
    private void Update()
    {
        // PlayerController 상태 확인
        if (playerController == null || playerRenderer == null) return;
        
        bool isSlammingNow = playerController.isSlamming;
        
        // Slam 상태가 변경된 경우에만 머티리얼 업데이트
        if (isSlammingNow != wasSlammingLastFrame)
        {
            UpdatePlayerMaterial(isSlammingNow);
            wasSlammingLastFrame = isSlammingNow;
        }
    }
    
    private void UpdatePlayerMaterial(bool isSlamming)
    {
        Material targetMaterial = isSlamming ? slammingPlayerMaterial : defaultPlayerMaterial;
        
        // 머티리얼이 할당되어 있는 경우에만 변경
        if (targetMaterial != null)
        {
            playerRenderer.material = targetMaterial;
            
            if (showMaterialDebug)
            {
                Debug.Log($"플레이어 머티리얼 변경: {targetMaterial.name} (isSlamming: {isSlamming})");
            }
        }
        else
        {
            if (showMaterialDebug)
            {
                Debug.LogWarning($"머티리얼이 할당되지 않음: {(isSlamming ? "Slamming" : "Default")} Material");
            }
        }
    }
    
    /* 공개 메서드 (다른 스크립트에서 호출 가능) ------------------- */
    
    public void SetDefaultMaterial(Material material)
    {
        defaultPlayerMaterial = material;
        
        // 현재 Slam 상태가 아니면 즉시 적용
        if (playerController != null && !playerController.isSlamming)
        {
            UpdatePlayerMaterial(false);
        }
    }
    
    public void SetSlammingMaterial(Material material)
    {
        slammingPlayerMaterial = material;
        
        // 현재 Slam 상태이면 즉시 적용
        if (playerController != null && playerController.isSlamming)
        {
            UpdatePlayerMaterial(true);
        }
    }
    
    public void ForceUpdateMaterial()
    {
        // 강제로 머티리얼 업데이트 (외부에서 호출 가능)
        if (playerController != null)
        {
            UpdatePlayerMaterial(playerController.isSlamming);
        }
    }
    
    /* 디버그 및 유틸리티 메서드 ---------------------------------- */
    
    public bool IsSlammingMaterialActive()
    {
        return playerController != null && playerController.isSlamming;
    }
    
    public Material GetCurrentMaterial()
    {
        return playerRenderer != null ? playerRenderer.material : null;
    }
    
    private void OnValidate()
    {
        // Inspector에서 값이 변경될 때 호출 (에디터에서만)
        if (Application.isPlaying && playerController != null)
        {
            ForceUpdateMaterial();
        }
    }
}