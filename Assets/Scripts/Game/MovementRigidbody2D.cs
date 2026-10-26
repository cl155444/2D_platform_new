using UnityEngine;

public class MovementRigidbody2D : MonoBehaviour
{
	[Header("LayerMask")]
	[SerializeField]
	private	LayerMask		groundCheckLayer;			// 바닥 체크를 위한 충돌 레이어
	[SerializeField]
	private	LayerMask		aboveCollisionLayer;		// 머리 충돌 체크를 위한 레이어

	[Header("Move")]
	[SerializeField]
	private	float			walkSpeed = 5;				// 걷는 속도
	[SerializeField]
	private	float			runSpeed = 8;				// 뛰는 속도

	[Header("Jump")]
	[SerializeField]
	private	float			jumpForce = 13;				// 점프 힘 (높을수록 높게 점프)
	[SerializeField]
	private	float			lowGravityScale = 2;		// 점프 키를 오래 누르고 있을 때 적용되는 중력 (높은 점프)
	[SerializeField]
	private	float			highGravityScale = 3.5f;	// 일반적으로 적용되는 중력 (낮은 점프)

	private	float			moveSpeed;					// 이동 속도

	private	float			jumpBufferTime = 0.1f;		// 점프 선입력 가능 시간 (착지 0.1초 전까지)
	private	float			jumpBufferCounter;

	private	float			hangTime = 0.2f;			// 공중에서 점프 가능 시간
	private	float			hangCounter;

	private	Vector2			collisionSize;				// 머리, 발 위치에 생성하는 충돌 박스 크기
	private	Vector2			footPosition;				// 발 위치
	private	Vector2			headPosition;				// 머리 위치

	private	Rigidbody2D		rigid2D;					// 물리를 제어하는 컴포넌트
	private	new	Collider2D	collider2D;					// 현재 오브젝트의 충돌 범위

	public	bool			IsLongJump		{ set; get; } = false;			// 낮은 점프, 높은 점프 체크
	public	bool			IsGrounded		{ private set; get; } = false;	// 바닥 체크 (바닥에 닿아있을 때 true)
	public	Collider2D		HitAboveObject	{ private set; get; }			// 머리에 충돌한 오브젝트 정보

	public	Vector2			Velocity => rigid2D.linearVelocity;

	private void Awake()
	{
		moveSpeed	= walkSpeed;

		rigid2D		= GetComponent<Rigidbody2D>();
		collider2D	= GetComponent<Collider2D>();
	}

	private void Update()
	{
		UpdateCollision();
		JumpHeight();
		JumpAdditive();
	}

	/// <summary>
	/// x축 속력(linearVelocityX) 설정. 외부 클래스에서 호출
	/// </summary>
	public void MoveTo(float x)
	{
		// x의 절대값이 0.5이면 걷기(walkSpeed), 1이면 뛰기(runSpeed)
		moveSpeed = Mathf.Abs(x) != 1 ? walkSpeed : runSpeed;

		// x가 -0.5, 0.5의 값을 가질 때 x를 -1, 1로 변경
		if ( x != 0 ) x = Mathf.Sign(x);

		// x축 속력을 x * moveSpeed로 설정
		rigid2D.linearVelocityX = x * moveSpeed;
	}

	private void UpdateCollision()
	{
		// 플레이어 오브젝트의 Collider2D min, center, max 위치 정보
		Bounds bounds	= collider2D.bounds;

		// 플레이어의 발에 생성하는 충돌 범위
		collisionSize	= new Vector2((bounds.max.x - bounds.min.x) * 0.5f, 0.1f);

		// 플레이어의 머리, 발 위치
		headPosition	= new Vector2(bounds.center.x, bounds.max.y);
		footPosition	= new Vector2(bounds.center.x, bounds.min.y);

		// 플레이어가 바닥을 밟고 있는지 체크하는 충돌 박스
		IsGrounded		= Physics2D.OverlapBox(footPosition, collisionSize, 0, groundCheckLayer);

		// 플레이어의 머리에 충돌한 오브젝트 정보를 저장하는 충돌 박스
		HitAboveObject	= Physics2D.OverlapBox(headPosition, collisionSize, 0, aboveCollisionLayer);
	}

	/// <summary>
	/// y축 속력 설정(점프 행동). 외부 클래스에서 호출
	/// </summary>
	public void Jump()
	{
		/*if ( IsGrounded == true )
		{
			rigid2D.linearVelocityY = jumpForce;
		}*/
		jumpBufferCounter = jumpBufferTime;
	}

	private void JumpHeight()
	{
		// 낮은 점프, 높은 점프 구현을 위한 중력 계수(gravityScale) 조절 (Jump Up일 때만 적용)
		// 중력 계수가 낮은 if 문은 높은 점프가 되고, 중력 계수가 높은 else 문은 낮은 점프
		if ( IsLongJump && rigid2D.linearVelocity.y > 0 )
		{
			rigid2D.gravityScale = lowGravityScale;
		}
		else
		{
			rigid2D.gravityScale = highGravityScale;
		}
	}

	private void JumpAdditive()
	{
		// 낭떠러지에서 떨어질 때 아주 잠시 동안은 점프가 가능하도록 설정
		if ( IsGrounded )	hangCounter = hangTime;
		else				hangCounter -= Time.deltaTime;

		// 바닥에 착지 직전 조금 빨리 점프 키를 눌렀을 때 바닥에 착지하면 바로 점프하도록 설정 (점프 선입력)
		if ( jumpBufferCounter > 0 ) jumpBufferCounter -= Time.deltaTime;

		if ( jumpBufferCounter > 0 && hangCounter > 0 )
		{
			rigid2D.linearVelocityY	= jumpForce;
			jumpBufferCounter		= 0;
			hangCounter				= 0;
		}
	}

	public void ResetVelocityY()
	{
		rigid2D.linearVelocityY = 0;
	}
}
