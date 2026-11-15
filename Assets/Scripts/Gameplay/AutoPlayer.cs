using System;
using System.Collections.Generic;
using UnityEngine;

// AUTO TEST: 채보를 보고 자동으로 입력한다. 단계(0~3)에 따라 일부러 틀리거나, 섞어 치거나, 완벽하게 친다.
// 판정은 GameplayManager의 기존 코드가 그대로 하고, 여기서는 "언제 누르고 언제 뗄지"만 정한다.
// 모든 노트 처리가 끝나면 판정 횟수와 점수를 요약 로그로 남긴다.
public class AutoPlayer : MonoBehaviour
{
    // 0: OFF(키보드) / 1: 실패 점검 / 2: 여러 판정(랜덤) / 3: 완벽한 플레이
    [SerializeField, Range(0, 3)] private int level;
    [SerializeField] private GameplayManager gameplay;
    [SerializeField] private ScoreComboState scoreCombo; // 요약 로그의 화면 점수 비교용

    // 1단계: 판정 범위보다 이만큼 먼저 눌러 입력이 무시되게 한다 (Good 범위 0.15초보다 큼)
    private const double EarlyPressOffset = 0.25;
    // 1단계: 롱노트 머리를 친 뒤 이 시간 후 바로 뗀다
    private const double QuickRelease = 0.05;
    // 2단계: 입력 타이밍 랜덤 오차 범위(±초)
    private const double RandomOffsetRange = 0.2;
    // 2단계: 머리를 친 롱노트를 중간에 일찍 뗄 확률
    private const double EarlyReleaseChance = 0.3;
    // 롱노트를 끝까지 누를 때 종료 시각보다 조금 더 누르고 있다가 뗀다 (종료 판정이 먼저 나도록)
    private const double HoldExtra = 0.05;

    // 노트 하나의 자동 입력 계획. PressTime이 NaN이면 누르지 않는다.
    private class Plan
    {
        public double PressTime;
        public double ReleaseTime;
    }

    private readonly Dictionary<Note, Plan> plans = new Dictionary<Note, Plan>();
    private readonly System.Random random = new System.Random(); // 시드 없음: 매번 다른 결과
    private int failPatternIndex; // 1단계 일반 노트 실패 방식을 번갈아 쓰기 위한 카운터

    // 요약 로그용 집계
    private readonly int[] judgedCounts = new int[4]; // Judgement 순서 (Perfect, Great, Good, Miss)
    private int tickCount;
    private int expectedScore; // 판정 횟수로 따로 계산한 예상 점수
    private bool reported;

    public int Level
    {
        get => level;
        set
        {
            level = Mathf.Clamp(value, 0, 3);
            ReplanWaitingNotes(); // 아직 안 친 노트부터 새 단계로 적용
        }
    }

    private void OnEnable()
    {
        if (gameplay == null) return;
        gameplay.OnJudged += CountJudged;
        gameplay.OnTick += CountTick;
    }

    private void OnDisable()
    {
        if (gameplay == null) return;
        gameplay.OnJudged -= CountJudged;
        gameplay.OnTick -= CountTick;
    }

    // 인스펙터에서 단계를 바꾼 경우에도 플레이 중이면 바로 적용한다.
    private void OnValidate()
    {
        if (Application.isPlaying) ReplanWaitingNotes();
    }

    // 모든 노트 처리가 끝나면 요약 로그를 한 번 남긴다.
    private void Update()
    {
        if (reported || gameplay == null || !gameplay.IsFinished) return;
        reported = true;

        int shownScore = scoreCombo != null ? scoreCombo.Score : -1;
        string match = shownScore == expectedScore ? "일치" : "불일치";
        Debug.Log($"[AutoPlay] 단계 {level} 완료: Perfect {judgedCounts[0]} / Great {judgedCounts[1]} / Good {judgedCounts[2]} / Miss {judgedCounts[3]} / Tick {tickCount} / 점수 {shownScore} (예상 {expectedScore}, {match})");
    }

    // 노트가 생성될 때 GameplayManager가 호출한다. 현재 단계 기준으로 누를/뗄 시각을 정한다.
    public void RegisterNote(Note note)
    {
        plans[note] = MakePlan(note.Data);
    }

    // 노트가 최종 판정되어 제거될 때 GameplayManager가 호출한다.
    public void UnregisterNote(Note note)
    {
        plans.Remove(note);
    }

    // 이번 프레임(직전 곡 시간 < 누를 시각 <= 현재 곡 시간)에 이 레인을 눌렀는지
    public bool WasPressed(int lane, double prevTime, double songTime)
    {
        foreach (KeyValuePair<Note, Plan> pair in plans)
        {
            Note note = pair.Key;
            Plan plan = pair.Value;
            if (note.Data.Lane != lane || note.State != HoldState.Waiting || double.IsNaN(plan.PressTime)) continue;
            if (prevTime < plan.PressTime && plan.PressTime <= songTime) return true;
        }
        return false;
    }

    // 이 레인의 유지 중인 롱노트를 아직 누르고 있는지 (뗄 시각 전이면 누르고 있음)
    public bool IsHeld(int lane, double songTime)
    {
        foreach (KeyValuePair<Note, Plan> pair in plans)
        {
            Note note = pair.Key;
            if (note.Data.Lane == lane && note.State == HoldState.Holding && songTime < pair.Value.ReleaseTime) return true;
        }
        return false;
    }

    // 아직 시작 판정 전인 노트의 계획을 현재 단계로 다시 만든다.
    private void ReplanWaitingNotes()
    {
        var waiting = new List<Note>();
        foreach (Note note in plans.Keys)
            if (note.State == HoldState.Waiting) waiting.Add(note);
        foreach (Note note in waiting)
            plans[note] = MakePlan(note.Data);
    }

    // 단계별 입력 계획
    private Plan MakePlan(NoteData data)
    {
        double holdToEnd = data.EndTime + HoldExtra;
        switch (level)
        {
            case 1: // 실패 점검
                if (data.IsLong)
                    return new Plan { PressTime = data.Time, ReleaseTime = data.Time + QuickRelease }; // 머리는 치고 바로 떼기
                failPatternIndex++;
                return failPatternIndex % 2 == 1
                    ? new Plan { PressTime = double.NaN }                         // ① 안 치기
                    : new Plan { PressTime = data.Time - EarlyPressOffset };      // ② 범위보다 먼저 누르기

            case 2: // 여러 판정
            {
                double press = data.Time + (random.NextDouble() * 2 - 1) * RandomOffsetRange;
                double release = holdToEnd;
                if (data.IsLong && random.NextDouble() < EarlyReleaseChance)
                {
                    // 머리 이후 ~ 종료 0.3초 전 사이 랜덤 시각에 뗀다 (종료 150ms 전보다 이르므로 끝 Miss)
                    double from = Math.Max(press, data.Time) + 0.05;
                    double to = data.EndTime - 0.3;
                    if (to > from) release = from + random.NextDouble() * (to - from);
                }
                return new Plan { PressTime = press, ReleaseTime = release };
            }

            case 3: // 완벽한 플레이
                return new Plan { PressTime = data.Time, ReleaseTime = holdToEnd };

            default: // 0: OFF (계획은 쓰이지 않음)
                return new Plan { PressTime = double.NaN };
        }
    }

    // 요약 로그 집계: 판정 횟수와 예상 점수
    private void CountJudged(Judgement judgement, int lane)
    {
        judgedCounts[(int)judgement]++;
        expectedScore += ExpectedPoints(judgement);
    }

    private void CountTick(Judgement headJudgement, int lane)
    {
        tickCount++;
        expectedScore += ExpectedPoints(headJudgement) * 30 / 100;
    }

    // 예상 점수 계산용 판정 점수표 (score_combo_plan.md 확정값). 화면 점수와 비교하기 위해 따로 둔다.
    private static int ExpectedPoints(Judgement judgement)
    {
        switch (judgement)
        {
            case Judgement.Perfect: return 1000;
            case Judgement.Great: return 700;
            case Judgement.Good: return 300;
            default: return 0;
        }
    }
}
