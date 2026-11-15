using UnityEngine;

// 곡 선택 화면에서 고른 곡을 플레이 씬·결과 씬으로 넘겨 준다.
// null이면 곡 선택 없이 플레이 씬을 바로 실행한 경우로, GameplayManager는 인스펙터의 채보를 쓴다.
public static class SongSelection
{
    public static SongData Selected { get; set; }

    // 이 프로젝트는 Enter Play Mode Options가 켜져 있어 Play를 다시 시작해도 static 값이 남는다.
    // Play 시작 시마다 비워서, 플레이 씬을 바로 실행하면 이전 세션의 곡이 아닌 인스펙터 채보를 쓰게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Selected = null;
    }
}
