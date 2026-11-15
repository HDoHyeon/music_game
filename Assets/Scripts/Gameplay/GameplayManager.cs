using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 핵심 플레이 루프: 곡 시간 관리, 노트 생성/이동, 키 입력, 판정을 담당한다.
public class GameplayManager : MonoBehaviour
{
    [Header("채보 / 오디오")]
    [SerializeField] private TextAsset chart;          // "시각,레인" 형식 채보
    [SerializeField] private AudioSource audioSource;  // 선택. clip이 있으면 곡 재생 시간을 곡 시간으로 사용

    [Header("노트")]
    [SerializeField] private Note notePrefab;
    [SerializeField] private float fallSpeed = 5f;     // 기준 배속(x3.0)일 때 노트 낙하 속도(유닛/초)
    [SerializeField] private float spawnY = 6f;        // 노트가 생성되는 Y (화면 위 바깥)
    [SerializeField] private float judgeLineY = -3f;   // 판정선 Y

    [Header("레인")]
    // 레인별 입력 키는 KeyBindings(키 설정, PlayerPrefs 저장)에서 읽는다.
    [SerializeField] private float[] laneXs = { -1.5f, -0.5f, 0.5f, 1.5f };

    [Header("AUTO TEST")]
    [SerializeField] private AutoPlayer autoPlayer; // 선택. 단계 1~3이면 키보드 대신 자동 입력 사용

    // 판정이 확정될 때 한 번 호출된다. (판정 등급, 레인)
    public event Action<Judgement, int> OnJudged;

    // 롱노트 유지 중 틱 판정마다 호출된다. (롱노트 시작 판정 등급, 레인)
    public event Action<Judgement, int> OnTick;

    private const double TickInterval = 0.15; // 롱노트 틱 판정 간격(초)
    private const double TickEpsilon = 1e-6;  // 0.15 누적 시 부동소수점 오차로 종료 시각과 겹치는 틱이 생기지 않도록 하는 여유값

    private List<NoteData> chartNotes;  // 시각순 채보
    private int nextNoteIndex;          // 다음에 생성할 채보 인덱스
    private List<Note>[] laneNotes;     // 레인별 화면에 있는 미판정 노트
    private readonly List<Note> missedLongNotes = new List<Note>(); // Miss 판정 후 종료 시각까지 회색으로 표시 중인 롱노트
    private double startTime;           // 오디오가 없을 때 곡 시간의 기준 시각
    private double prevSongTime;        // 직전 프레임의 곡 시간 (자동 입력의 "이번 프레임에 눌림" 판단용)
    private bool UseAudioClock => audioSource != null && audioSource.clip != null;

    // 현재 곡 시간(초)
    public double SongTime => UseAudioClock ? audioSource.time : Time.timeAsDouble - startTime;

    // 채보의 모든 노트가 생성되고 최종 판정까지 끝났으며, 화면에 남은 노트(회색 Miss 롱노트 포함)도 없는지
    public bool IsFinished
    {
        get
        {
            if (chartNotes == null || nextNoteIndex < chartNotes.Count) return false;
            foreach (List<Note> notes in laneNotes)
                if (notes.Count > 0) return false;
            return missedLongNotes.Count == 0;
        }
    }

    // 음악 재생이 끝났는지. 곡이 지정되지 않았으면 끝난 것으로 본다.
    public bool IsMusicFinished => !UseAudioClock || !audioSource.isPlaying;

    // 배속을 반영한 실제 낙하 속도. x3.0이면 fallSpeed 그대로, 그 외에는 배속 비율만큼 빠르거나 느리다.
    private float FallSpeed => fallSpeed * NoteSpeed.Value / NoteSpeed.Default;

    private bool UseAutoPlay => autoPlayer != null && autoPlayer.Level > 0;

    private void Start()
    {
        // 곡 선택 화면에서 고른 곡이 있으면 그 곡의 채보·음악으로 플레이 (없으면 인스펙터 값 사용)
        SongData song = SongSelection.Selected;
        if (song != null)
        {
            chart = song.Chart;
            if (audioSource != null) audioSource.clip = song.Music; // 음악이 없으면 null → 시작 후 경과 시간 사용
        }

        int laneCount = Mathf.Min(KeyBindings.LaneCount, laneXs.Length);
        chartNotes = ChartLoader.Load(chart, laneCount);
        laneNotes = new List<Note>[laneCount];
        for (int i = 0; i < laneCount; i++) laneNotes[i] = new List<Note>();

        startTime = Time.timeAsDouble;
        if (UseAudioClock) audioSource.Play();
        prevSongTime = SongTime;
    }

    private void Update()
    {
        double songTime = SongTime;
        SpawnNotes(songTime);
        MoveNotes(songTime);
        HandleInput(songTime);
        HandleHolds(songTime);
        HandleMisses(songTime);
        prevSongTime = songTime;
    }

    // 화면 위에 나타나야 할 시점이 된 노트를 미리 생성한다.
    private void SpawnNotes(double songTime)
    {
        double leadTime = (spawnY - judgeLineY) / FallSpeed; // 생성 위치에서 판정선까지 걸리는 시간
        while (nextNoteIndex < chartNotes.Count && chartNotes[nextNoteIndex].Time - songTime <= leadTime)
        {
            NoteData data = chartNotes[nextNoteIndex++];
            Note note = Instantiate(notePrefab, transform);
            note.Init(data, laneXs[data.Lane]);
            note.UpdatePosition(songTime, judgeLineY, FallSpeed);
            laneNotes[data.Lane].Add(note);
            if (autoPlayer != null) autoPlayer.RegisterNote(note);
        }
    }

    // 모든 노트 위치를 곡 시간에 맞춰 갱신한다.
    // Miss 표시 중인 롱노트는 종료 시각(꼬리가 판정선에 닿는 시각)이 지나면 삭제한다.
    private void MoveNotes(double songTime)
    {
        foreach (List<Note> notes in laneNotes)
            foreach (Note note in notes)
                note.UpdatePosition(songTime, judgeLineY, FallSpeed);

        for (int i = missedLongNotes.Count - 1; i >= 0; i--)
        {
            Note note = missedLongNotes[i];
            if (songTime >= note.Data.EndTime)
            {
                missedLongNotes.RemoveAt(i);
                Destroy(note.gameObject);
            }
            else
            {
                note.UpdatePosition(songTime, judgeLineY, FallSpeed);
            }
        }
    }

    // 레인 키가 눌리면 그 레인의 대기 중인 노트 중 시간 차가 가장 작은 노트를 판정한다. Good 범위 밖이면 무시한다.
    // 롱노트는 시작 판정만 보고하고 유지 상태로 바꾼다(노트는 화면에 남음).
    private void HandleInput(double songTime)
    {
        for (int lane = 0; lane < laneNotes.Length; lane++)
        {
            if (!WasPressed(lane, songTime)) continue;

            Note closest = null;
            double closestDiff = double.MaxValue;
            foreach (Note note in laneNotes[lane])
            {
                if (note.State != HoldState.Waiting) continue; // 유지 중인 롱노트는 제외
                double diff = note.Data.Time - songTime;
                if (Math.Abs(diff) < Math.Abs(closestDiff))
                {
                    closest = note;
                    closestDiff = diff;
                }
            }

            if (closest == null || !JudgementWindow.TryGetJudgement(closestDiff, out Judgement result)) continue;

            if (closest.Data.IsLong)
            {
                closest.State = HoldState.Holding;
                closest.HeadJudgement = result;
                closest.NextTickTime = closest.Data.Time + TickInterval;
                Report(closest, result, "head");
            }
            else
            {
                Judge(closest, result);
            }
        }
    }

    // 유지 중인 롱노트: 종료 150ms 전보다 일찍 떼면 끝 Miss(남은 틱 버림),
    // 유지 중이면 0.15초마다 틱 판정, 종료 시각까지 누르고 있으면 끝 Perfect.
    private void HandleHolds(double songTime)
    {
        for (int lane = 0; lane < laneNotes.Length; lane++)
        {
            bool pressed = IsHeld(lane, songTime);
            List<Note> notes = laneNotes[lane];
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                Note note = notes[i];
                if (note.State != HoldState.Holding) continue;

                if (!pressed && note.Data.EndTime - songTime > JudgementWindow.GoodWindow)
                {
                    note.State = HoldState.Failed;
                    Judge(note, Judgement.Miss);
                    continue;
                }

                // 이번 프레임까지 도달한 틱을 모두 처리한다 (종료 시각과 겹치는 틱은 끝 판정으로 대신함)
                while (note.NextTickTime < note.Data.EndTime - TickEpsilon && songTime >= note.NextTickTime)
                {
                    ReportTick(note);
                    note.NextTickTime += TickInterval;
                }

                if (songTime >= note.Data.EndTime)
                {
                    note.State = HoldState.Completed;
                    Judge(note, Judgement.Perfect);
                }
            }
        }
    }

    // Good 범위를 지나도록 입력되지 않은 대기 노트를 Miss 처리한다. 롱노트는 시작 Miss + 끝 Miss 2회.
    private void HandleMisses(double songTime)
    {
        foreach (List<Note> notes in laneNotes)
        {
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                Note note = notes[i];
                if (note.State != HoldState.Waiting) continue;
                if (songTime - note.Data.Time <= JudgementWindow.GoodWindow) continue;

                if (note.Data.IsLong)
                {
                    note.State = HoldState.Failed;
                    Report(note, Judgement.Miss, "head");
                }
                Judge(note, Judgement.Miss);
            }
        }
    }

    // 레인 입력(이번 프레임에 눌림): AUTO 단계 1~3이면 AutoPlayer 계획대로, 0이면 키보드
    private bool WasPressed(int lane, double songTime)
    {
        if (UseAutoPlay) return autoPlayer.WasPressed(lane, prevSongTime, songTime);
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[KeyBindings.Get(lane)].wasPressedThisFrame;
    }

    // 레인 입력(누르고 있음): AUTO 단계 1~3이면 AutoPlayer 계획대로, 0이면 키보드
    private bool IsHeld(int lane, double songTime)
    {
        if (UseAutoPlay) return autoPlayer.IsHeld(lane, songTime);
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard[KeyBindings.Get(lane)].isPressed;
    }

    // 최종 판정을 확정하고 노트를 판정 대상에서 뺀 뒤 알린다. 이미 판정된 노트는 무시한다.
    // 롱노트는 이 판정이 끝(tail) 판정이다. 롱노트 Miss는 바로 지우지 않고 회색으로 종료 시각까지 표시한다.
    private void Judge(Note note, Judgement result)
    {
        if (note.IsJudged) return;
        note.IsJudged = true;

        laneNotes[note.Data.Lane].Remove(note);
        if (autoPlayer != null) autoPlayer.UnregisterNote(note);
        if (note.Data.IsLong && result == Judgement.Miss)
        {
            note.ShowMissed();
            missedLongNotes.Add(note);
        }
        else
        {
            Destroy(note.gameObject);
        }

        Report(note, result, note.Data.IsLong ? "tail" : "note");
    }

    // 판정 1회를 로그로 남기고 OnJudged 이벤트로 알린다. part: note(일반) / head(롱노트 시작) / tail(롱노트 끝)
    private void Report(Note note, Judgement result, string part)
    {
        double time = part == "tail" ? note.Data.EndTime : note.Data.Time;
        Debug.Log($"[Judge] lane {note.Data.Lane} {result} ({part} {time:F3}s)");
        OnJudged?.Invoke(result, note.Data.Lane);
    }

    // 롱노트 틱 1회를 로그로 남기고 OnTick 이벤트로 알린다. (판정 횟수와 섞이지 않도록 OnJudged와 분리)
    private void ReportTick(Note note)
    {
        Debug.Log($"[Judge] lane {note.Data.Lane} Tick({note.HeadJudgement}) (tick {note.NextTickTime:F3}s)");
        OnTick?.Invoke(note.HeadJudgement, note.Data.Lane);
    }
}
