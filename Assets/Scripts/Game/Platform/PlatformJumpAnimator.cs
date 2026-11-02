using UnityEngine;

public class PlatformJumpAnimator : MonoBehaviour
{
    private GameObject target;
    private float jumpForce;

    public void Setup(GameObject target, float jumpForce)
    {
        this.target = target;
        this.jumpForce = jumpForce;
    }

    public void JumpAction() // Platform_02_Jump_Play 애니메이션의 특정 프레임에 호출
    {
        if (target == null)
        {
            Debug.LogWarning("JumpAction 호출 시 target이 null입니다. Setup이 먼저 호출되었는지 확인하세요.", this);
            return;
        }

        if (target.TryGetComponent(out MovementRigidbody2D movement))
        {
            movement.JumpTo(jumpForce);
        }
        else
        {
            Debug.LogWarning($"{target.name}에 MovementRigidbody2D가 없습니다.", target);
        }

        target = null;
    }
}