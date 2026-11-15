using System;

// 판정 등급
public enum Judgement
{
    Perfect,
    Great,
    Good,
    Miss
}

// 입력 시각과 노트 시각의 차이를 판정 등급으로 변환한다.
public static class JudgementWindow
{
    public const double PerfectWindow = 0.050; // ±50ms
    public const double GreatWindow = 0.100;   // ±100ms
    public const double GoodWindow = 0.150;    // ±150ms, 이 범위를 벗어나면 판정하지 않음

    // diff = 노트 시각 - 입력 시각(초). Good 범위 밖이면 false를 반환한다.
    public static bool TryGetJudgement(double diff, out Judgement result)
    {
        double abs = Math.Abs(diff);
        if (abs <= PerfectWindow) { result = Judgement.Perfect; return true; }
        if (abs <= GreatWindow) { result = Judgement.Great; return true; }
        if (abs <= GoodWindow) { result = Judgement.Good; return true; }
        result = Judgement.Miss;
        return false;
    }
}
