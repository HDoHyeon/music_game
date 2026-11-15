using UnityEngine;

// 노트 배속 설정. x1.0 ~ x5.0, 0.5 단위이며 기본값 x3.0이 기존 노트 속도다.
// PlayerPrefs에 저장해 다시 실행해도 유지된다. 배속은 노트 화면 위치에만 쓰이고 판정 시각·음악에는 영향이 없다.
public static class NoteSpeed
{
    public const float Min = 1f;
    public const float Max = 5f;
    public const float Step = 0.5f;
    public const float Default = 3f;           // 기존 노트 속도(GameplayManager.fallSpeed)에 해당하는 배속
    private const string PrefsKey = "NoteSpeed"; // PlayerPrefs 키 이름

    private static float value;
    private static bool loaded; // 처음 사용할 때 PlayerPrefs에서 불러온다

    // 현재 배속
    public static float Value
    {
        get
        {
            EnsureLoaded();
            return value;
        }
    }

    // 표시용 문자열 (예: "SPEED x3.0")
    public static string Label => "SPEED x" + Value.ToString("0.0");

    // 한 단계(0.5) 올리고 저장한다. 최대값에서는 그대로.
    public static void Increase() => Set(Value + Step);

    // 한 단계(0.5) 내리고 저장한다. 최소값에서는 그대로.
    public static void Decrease() => Set(Value - Step);

    private static void Set(float newValue)
    {
        value = Snap(newValue);
        loaded = true;
        PlayerPrefs.SetFloat(PrefsKey, value);
        PlayerPrefs.Save();
    }

    // 0.5 단위로 맞추고 범위를 넘지 않게 자른다. (저장값이 잘못되어도 가장 가까운 유효값이 된다)
    private static float Snap(float v)
    {
        return Mathf.Clamp(Mathf.Round(v / Step) * Step, Min, Max);
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        value = Snap(PlayerPrefs.GetFloat(PrefsKey, Default));
        loaded = true;
    }

    // Enter Play Mode Options로 static 값이 남으므로 Play 시작 시마다 PlayerPrefs에서 다시 불러오게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        loaded = false;
    }
}
