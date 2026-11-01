using Mono.Cecil;
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
        target.GetComponent<MovementRigidbody2D>().JumpTo(jumpForce);
        target = null;

    }
}
