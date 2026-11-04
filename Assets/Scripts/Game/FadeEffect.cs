using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public static class FadeEffect
{
    public static IEnumerator Fade(Tilemap target, float start, float end, float fadeTime = 1, UnityAction action= null)
    {
        if (target == null) yield break;

        float percent = 0;

        while (percent < 1)
        {
            percent += Time.deltaTime / fadeTime;

            Color color = target.color;
            color.a = Mathf.Lerp(start, end, percent);
            target.color = color;

            yield return null;
        }

        action?.Invoke(); //Invoke의 역할 : 일정 시간 뒤 실행하도록 -> 여기에서는 action에 메소드가 등록되었는지 null 검사를 진행한 후에 실행시키는 역할인 것인지?
    }
}
