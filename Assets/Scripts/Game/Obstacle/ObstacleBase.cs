using UnityEngine;

public class ObstacleBase : MonoBehaviour
{
    [SerializeField]
    private bool isInstantDeath = true; // true면 즉사, false면 체력 감소 장애물

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 장애물은 플레이어와 충돌했을 때만 로직 처리
        if (!collision.CompareTag("Player")) return;

        if(isInstantDeath)
        {
            Debug.Log("플레이어 사망");
        }
        else
        {
            Debug.Log("플레이어 체력 감소");
        }
    }
}
