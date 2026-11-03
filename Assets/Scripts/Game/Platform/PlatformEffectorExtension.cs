using System.Collections;
using UnityEngine;

public class PlatformEffectorExtension : MonoBehaviour
{
    private PlatformEffector2D effector;

    private void Awake()
    {
        effector = GetComponent<PlatformEffector2D>();
    }

    public void OnDownWay()
    {
        StartCoroutine(nameof(ReverseRotationalOffset));
    }
    private IEnumerator ReverseRotationalOffset()
    {
        effector.rotationalOffset = 180; // 회전 각도를 180으로 뒤집어 기존 규칙을 뒤짚는다.

        yield return new WaitForSeconds(0.5f); // 0.5초 후 기존 설정으로 되돌린다.

        effector.rotationalOffset = 0;

    }
}
