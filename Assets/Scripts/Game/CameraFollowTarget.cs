using UnityEngine;

public class CameraFollowTarget : MonoBehaviour
{
	[SerializeField]
	private	Transform			target;				// 카메라가 추적할 대상 (플레이어)
	[SerializeField]
	private	float				smoothTime;			// 카메라가 target을 부드럽게 쫓아오기 위한 시간
	[SerializeField]
	private	StageData			stageData;			// 카메라의 한계 범위를 설정하기 위한 스테이지 데이터
	private	Vector3				position;			// 카메라의 목표 위치
	private	Vector3				velocity;
	private	float				offsetY;			// target과 카메라가 유지해야할 y 거리
	private	float				yThreshold = 5f;	// y축 임계값
	private	MovementRigidbody2D	targetMovement;

	private void Awake()
	{
		targetMovement	= target.GetComponent<MovementRigidbody2D>();
		offsetY			= Mathf.Abs(transform.position.y - target.position.y);
	}

	private void Update()
	{
		position = transform.position;

		// 플레이어(target)의 x 좌표를 따라가되 카메라의 좌/우측 이동 범위를 넘어가지 않도록 설정
		position.x = Mathf.Clamp(target.position.x, stageData.CameraLimitMinX, stageData.CameraLimitMaxX);

		// target이 공중에 떠있을 때 y 좌표를 변경하면 어지러울 수 있기 때문에 착지했을 때 카메라 이동 ||
		// 플레이어와 카메라의 거리가 너무 멀어지면 카메라 이동
		if ( targetMovement.IsGrounded || yThreshold <= Mathf.Abs(transform.position.y - target.position.y) )
		{
			// 카메라 y축이 target의 y축을 부드럽게 쫓아오도록 설정 (현재 위치, 목표 위치, 속력, 이동 시간)
			position.y = Mathf.SmoothDamp(transform.position.y, target.position.y + offsetY, ref velocity.y, smoothTime);
		}

		transform.position = position;
	}
}
