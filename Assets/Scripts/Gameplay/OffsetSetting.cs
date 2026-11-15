using UnityEngine;

// 오프셋(싱크 보정) 설정. -200 ~ +200ms, 5ms 단위, 기본 0. PlayerPrefs에 저장해 다시 실행해도 유지된다.
// +면 노트가 늦게 판정선에 도착하고 판정도 늦어진다 (소리가 노트보다 늦게 들리면 +로 올린다).
// 음악이 있는 곡에만 적용되며, 플레이 시작 시 GameplayManager가 값을 읽어 그 플레이 동안 고정한다.
// 값은 탭 보정 화면(OffsetCalibration)에서 측정해 Set으로 저장한다.
public static class OffsetSetting
{
    public const int Min = -200;
    public const int Max = 200;
    public const int Step = 5;
    public const int Default = 0;
    private const string PrefsKey = "TimingOffsetMs"; // PlayerPrefs 키 이름

    private static int value;
    private static bool loaded; // 처음 사용할 때 PlayerPrefs에서 불러온다

    // 현재 오프셋(ms)
    public static int Value
    {
        get
        {
            EnsureLoaded();
            return value;
        }
    }

    // 표시용 문자열 (예: "OFFSET 0 ms", "OFFSET +15 ms", "OFFSET -20 ms")
    public static string Label => "OFFSET " + Value.ToString("+0;-0;0") + " ms";

    // 값을 지정해 저장한다. 5ms 단위로 맞추고 ±200ms 범위로 자른다.
    public static void Set(int newValue)
    {
        value = Snap(newValue);
        loaded = true;
        PlayerPrefs.SetInt(PrefsKey, value);
        PlayerPrefs.Save();
    }

    // 5ms 단위로 맞추고 범위를 넘지 않게 자른다. (저장값이 잘못되어도 가장 가까운 유효값이 된다)
    private static int Snap(int v)
    {
        return Mathf.Clamp(Mathf.RoundToInt(v / (float)Step) * Step, Min, Max);
    }

    private static void EnsureLoaded()
    {
        if (loaded) return;
        value = Snap(PlayerPrefs.GetInt(PrefsKey, Default));
        loaded = true;
    }

    // Enter Play Mode Options로 static 값이 남으므로 Play 시작 시마다 PlayerPrefs에서 다시 불러오게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        loaded = false;
    }
}
