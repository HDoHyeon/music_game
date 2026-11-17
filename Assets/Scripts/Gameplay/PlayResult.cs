// 한 번의 플레이 결과 (생성 후 바뀌지 않음). 결과 씬으로 넘길 때 사용한다.
public class PlayResult
{
    // 마지막으로 끝난 플레이의 결과. 플레이가 끝나 결과 씬으로 넘어갈 때만 새 객체로 바뀐다.
    public static PlayResult Last { get; set; }

    // Enter Play Mode Options가 켜져 있어 Play를 다시 시작해도 static 값이 남으므로, Play 시작 시마다 비운다.
    // (결과 씬을 바로 실행하면 이전 세션 결과가 아닌 "NO RESULT"가 나오도록)
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        Last = null;
    }

    public int Score { get; }
    public int MaxCombo { get; }
    public int Perfect { get; }
    public int Great { get; }
    public int Good { get; }
    public int Miss { get; }

    // 기록 관련 (결과 화면 표시용, 점수 계산과 무관)
    public bool AutoPlayUsed { get; }   // AUTO TEST가 켜졌던 플레이 (기록 안 함)
    public bool IsNewBest { get; }      // 이번 결과로 저장된 최고 점수가 올라갔는지 (첫 기록 포함)
    public bool HasBest { get; }        // 이 곡·난이도에 저장된 기록이 있는지 (이번 저장 포함)
    public int BestScore { get; }       // 저장된 최고 점수 (이번 저장 반영 후)

    public PlayResult(int score, int maxCombo, int perfect, int great, int good, int miss)
        : this(score, maxCombo, perfect, great, good, miss, false, false, false, 0)
    {
    }

    public PlayResult(int score, int maxCombo, int perfect, int great, int good, int miss,
        bool autoPlayUsed, bool isNewBest, bool hasBest, int bestScore)
    {
        AutoPlayUsed = autoPlayUsed;
        IsNewBest = isNewBest;
        HasBest = hasBest;
        BestScore = bestScore;
        Score = score;
        MaxCombo = maxCombo;
        Perfect = perfect;
        Great = great;
        Good = good;
        Miss = miss;
    }

    // 판정 수 (틱 제외)
    public int Total => Perfect + Great + Good + Miss;

    // 정확도(%): 판정 가중 평균 (Perfect 100 / Great 70 / Good 30 / Miss 0, 틱 제외)
    public float Accuracy => Total == 0 ? 0f : (Perfect * 100f + Great * 70f + Good * 30f) / Total;

    // FULL COMBO: 판정이 1개 이상이고 Miss가 없음 (콤보는 Miss로만 끊긴다. 롱노트 중간 이탈·시작 놓침도 Miss로 집계됨)
    public bool IsFullCombo => Total > 0 && Miss == 0;

    // ALL PERFECT: 판정이 1개 이상이고 모두 Perfect (아래 Rank "SS" 조건과 같음). 틱은 판정 개수가 아니므로 제외
    public bool IsAllPerfect => Total > 0 && Perfect == Total;

    // 랭크: 전부 Perfect면 SS, 그 외 정확도 기준 S 95 / A 90 / B 80 / C 70 / D 그 미만
    public string Rank => Total > 0 && Perfect == Total ? "SS"
        : Accuracy >= 95f ? "S"
        : Accuracy >= 90f ? "A"
        : Accuracy >= 80f ? "B"
        : Accuracy >= 70f ? "C"
        : "D";
}
