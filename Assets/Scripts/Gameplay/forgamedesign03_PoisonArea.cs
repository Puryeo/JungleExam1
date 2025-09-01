using UnityEngine;

/// - 이 스크립트가 붙은 오브젝트의 콜라이더와 다른 오브젝트가 충돌(Trigger/Collision)할 때,
///   플레이어를 지정한 savepoint 위치로 즉시 순간이동시킵니다.

public class forgamedesign03_PoisonArea : MonoBehaviour
{
    [SerializeField, Tooltip("플레이어를 이동시킬 월드 좌표(체크포인트 등)")]
    private Vector3 savepoint = Vector3.zero;

    /// Trigger 콜라이더 간에 접촉이 발생했을 때 호출됩니다.
    /// - other: 현재 영역에 들어온 상대방의 콜라이더
    /// - 여기서는 상대에 플레이어 컨트롤러가 있는지 확인하고, 있으면 순간이동만 수행합니다.
    private void OnTriggerEnter(Collider other)
    {
        TryTeleportPlayer(other);
    }

    /// 일반 콜라이더 간 충돌이 발생했을 때 호출됩니다.
    /// - collision: 충돌 정보(상대방 콜라이더 포함)
    /// - Trigger가 아닌 콜라이더를 사용하는 경우에도 동작하도록 동일 처리합니다.
    private void OnCollisionEnter(Collision collision)
    {
        TryTeleportPlayer(collision.collider);
    }

    /// 플레이어 판별 후 순간이동을 수행하는 공통 처리.
    /// - 상대 오브젝트(또는 부모)에 forgamedesign03_PlayerController가 붙어 있으면
    ///   해당 오브젝트를 savepoint로 이동시킵니다.
    /// <param name="other">충돌한 상대 컴포넌트(콜라이더 등)</param>
    
    private void TryTeleportPlayer(Component other)
    {
        // 상대(또는 부모)에서 플레이어 컨트롤러 탐색
        var playerController = other.GetComponentInParent<forgamedesign03_PlayerController>();
        if (playerController == null)
        {
            // 플레이어가 아니면 아무 것도 하지 않음
            return;
        }

        // 플레이어를 지정한 위치로 즉시 이동
        playerController.transform.position = savepoint;
    }
}