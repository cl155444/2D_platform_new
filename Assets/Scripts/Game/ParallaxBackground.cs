using UnityEngine;

[System.Serializable]
public struct BackgroundData
{
	public	Renderer	background;
	public	float		speed;
}

public class ParallaxBackground : MonoBehaviour
{
	[SerializeField]
	private	Transform			targetCamera;		// Main Camera
	[SerializeField]
	private	BackgroundData[]	backgrounds;		// 근경(00), 중경(01), 원경(02)
	[SerializeField]
	private	BackgroundData		backgroundCloud;	// 구름 배경

	private	float				targetStartX;

	private void Awake()
	{
		targetStartX = targetCamera.position.x;
	}

	private void Update()
	{
		// 구름 배경은 backgroundCloud.speed의 속도로 자동 이동
		float cloudOffset = Time.time * backgroundCloud.speed;
		backgroundCloud.background.material.mainTextureOffset = new Vector2(cloudOffset, 0);

		if ( targetCamera == null ) return;

		// 근경, 중경, 원경은 카메라 이동 거리 * 각각의 속도만큼 이동
		float x = targetCamera.position.x - targetStartX;
		for ( int i = 0; i < backgrounds.Length; ++ i )
		{
			float offset = x * backgrounds[i].speed;
			backgrounds[i].background.material.mainTextureOffset = new Vector2(offset, 0);
		}
	}
}
