using UnityEngine;

[CreateAssetMenu]
public class StageData : ScriptableObject
{
	[Header("Camera Limit")]
	[SerializeField]
	private	float	cameraLimitMinX;	// 현재 스테이지에서 카메라가 갈 수 있는 최소 x 위치 (왼쪽)
	[SerializeField]
	private	float	cameraLimitMaxX;	// 현재 스테이지에서 카메라가 갈 수 있는 최대 x 위치 (오른쪽)

	[Header("Player Limit")]
	[SerializeField]
	private	float	playerLimitMinX;	// 현재 스테이지에서 플레이어가 갈 수 있는 최소 x 위치 (왼쪽)
	[SerializeField]
	private	float	playerLimitMaxX;	// 현재 스테이지에서 플레이어가 갈 수 있는 최대 x 위치 (오른쪽)

	[Header("Map Limit")]
	[SerializeField]
	private	float	mapLimitMinY;		// 현재 스테이지의 가장 아래 바닥 y 위치

	// 외부에서 변수 데이터에 접근하기 위한 프로퍼티 get
	public	float	CameraLimitMinX => cameraLimitMinX;
	public	float	CameraLimitMaxX => cameraLimitMaxX;
	public	float	PlayerLimitMinX => playerLimitMinX;
	public	float	PlayerLimitMaxX => playerLimitMaxX;
	public	float	MapLimitMinY	=> mapLimitMinY;
}
