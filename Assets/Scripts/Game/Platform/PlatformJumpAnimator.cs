using UnityEngine;

public class PlatformJumpAnimator : MonoBehaviour
{
    private GameObject target;
    private float jumpForce;

    public bool IsBusy => target != null;   // 점프 처리 중인지

    public void Setup(GameObject target, float jumpForce)
    {
        this.target = target;
        this.jumpForce = jumpForce;
    }

    public void JumpAction() // Platform_02_Jump_Play 애니메이션 이벤트
    {
        if (target == null) return;

        if (target.TryGetComponent(out MovementRigidbody2D movement))
            movement.JumpTo(jumpForce);

        target = null;
    }
}