using UnityEngine;

// 곡 선택 화면에서 고른 곡과 난이도를 플레이 씬·결과 씬으로 넘겨 준다.
// RESTART·RETRY·곡 선택 복귀·설정 왕복은 모두 이 값을 다시 읽으므로 같은 곡·같은 난이도가 유지된다.
// null이면 곡 선택 없이 플레이 씬을 바로 실행한 경우로, GameplayManager는 인스펙터의 채보를 쓴다.
public static class SongSelection
{
    public static SongData Selected { get; set; }

    // 선택 난이도. 곡을 바꿔도 마지막으로 고른 값을 유지한다 (처음은 Normal). 채보가 하나뿐인 곡에서는 쓰이지 않는다.
    public static Difficulty SelectedDifficulty { get; set; } = Difficulty.Normal;

    // 이 프로젝트는 Enter Play Mode Options가 켜져 있어 Play를 다시 시작해도 static 값이 남는다.
    // Play 시작 시마다 비워서, 플레이 씬을 바로 실행하면 이전 세션의 곡이 아닌 인스펙터 채보를 쓰게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Selected = null;
        SelectedDifficulty = Difficulty.Normal;
    }
}
