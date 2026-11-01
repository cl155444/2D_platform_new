using UnityEditor.PackageManager.Requests;
using UnityEngine;

public class PlatformJump : PlatformBase
{
    [SerializeField]
    private float jumpForce = 22f;
    [SerializeField]
    private float resetTime = 1f;

    private PlatformJumpAnimator jumpAnimator;
    private Animator animator;

    private void Awake()
    {
        jumpAnimator = GetComponentInChildren<PlatformJumpAnimator>();
        animator = GetComponentInChildren<Animator>();
    }

    public override void UpdateCollision(GameObject target)
    {
        if (IsHit == true) return;

        jumpAnimator.Setup(target, jumpForce);
        animator.SetTrigger("onJump");

        Invoke(nameof(Reset), resetTime);

   
    }
    private void Reset()
    {
        IsHit = false;
    }
}
