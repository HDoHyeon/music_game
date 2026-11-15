using UnityEngine;
using UnityEngine.InputSystem;

// 레인별 입력 키 설정. PlayerPrefs에 저장해 다시 실행해도 유지된다.
// 이미 다른 레인이 쓰는 키로 바꾸면 두 레인의 키를 서로 맞바꾼다.
public static class KeyBindings
{
    public const int LaneCount = 4;
    private static readonly Key[] DefaultKeys = { Key.D, Key.F, Key.J, Key.K };
    private const string PrefsPrefix = "KeyBinding.Lane"; // PlayerPrefs 키 이름: KeyBinding.Lane0 ~ Lane3

    private static Key[] keys; // 처음 사용할 때 PlayerPrefs에서 불러온다

    // 이 레인의 현재 키
    public static Key Get(int lane)
    {
        EnsureLoaded();
        return keys[lane];
    }

    // 레인 키로 쓸 수 있는 키인지. Esc(취소)·Enter·방향키(곡 선택 조작)는 쓸 수 없다.
    public static bool IsAllowed(Key key)
    {
        switch (key)
        {
            case Key.None:
            case Key.Escape:
            case Key.Enter:
            case Key.NumpadEnter:
            case Key.UpArrow:
            case Key.DownArrow:
            case Key.LeftArrow:
            case Key.RightArrow:
                return false;
            default:
                return true;
        }
    }

    // 레인 키 변경: 다른 레인이 이미 쓰는 키면 서로 맞바꾼다. 금지 키거나 레인 번호가 잘못되면 false.
    public static bool TrySet(int lane, Key key)
    {
        EnsureLoaded();
        if (!IsAllowed(key) || lane < 0 || lane >= LaneCount) return false;
        int other = System.Array.IndexOf(keys, key);
        if (other >= 0) keys[other] = keys[lane]; // 맞바꿈
        keys[lane] = key;
        Save();
        return true;
    }

    // 기본 키(D/F/J/K)로 되돌리고 저장한다.
    public static void Reset()
    {
        keys = (Key[])DefaultKeys.Clone();
        Save();
    }

    private static void EnsureLoaded()
    {
        if (keys != null) return;
        keys = (Key[])DefaultKeys.Clone();
        for (int i = 0; i < LaneCount; i++)
        {
            Key saved = (Key)PlayerPrefs.GetInt(PrefsPrefix + i, (int)DefaultKeys[i]);
            if (IsAllowed(saved) && System.Enum.IsDefined(typeof(Key), saved)) keys[i] = saved;
        }
        // 저장값이 잘못되어 키가 겹치면 기본값으로 되돌린다
        for (int i = 0; i < LaneCount; i++)
            for (int j = i + 1; j < LaneCount; j++)
                if (keys[i] == keys[j]) { keys = (Key[])DefaultKeys.Clone(); return; }
    }

    private static void Save()
    {
        for (int i = 0; i < LaneCount; i++) PlayerPrefs.SetInt(PrefsPrefix + i, (int)keys[i]);
        PlayerPrefs.Save();
    }

    // Enter Play Mode Options로 static 값이 남으므로 Play 시작 시마다 PlayerPrefs에서 다시 불러오게 한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        keys = null;
    }
}
