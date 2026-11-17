using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 채보 검사기 (에디터 전용, 게임 빌드에는 포함되지 않음). 메뉴 Tools > Validate Charts.
// SongList의 모든 곡·난이도 채보를 검사해 Console에 요약(노트 수·길이 등)과 문제(파일명·줄 번호·종류·원인)를 출력한다.
// 채보·에셋은 읽기만 하고 고치지 않는다. 게임의 채보 읽기(ChartLoader)와 같은 줄 검사(TryParseLine)를 쓴다.
// 등급: 치명(플레이 불가) / 줄 건너뜀(그 줄만 빠짐) / 칠 수 없는 노트(읽히지만 Miss가 됨) / 경고(수정 권장)
public static class ChartValidator
{
    public const int LaneCount = 4;
    public const double MinLaneGap = 0.1;    // 같은 레인 노트 간격(롱노트는 종료부터)이 이보다 짧으면 경고
    public const double MusicEndMargin = 1.0; // 노트 종료가 음원 끝에서 이 시간 이내면 경고
    private const string SongListPath = "Assets/Songs/SongList.asset";

    public enum Severity { Fatal, LineSkipped, Unplayable, Warning }

    public class Issue
    {
        public Severity Severity;
        public string File;
        public int Line;      // 1부터. 파일 전체 문제는 0
        public string Kind;
        public string Reason;
        public override string ToString() => $"[{SeverityLabel(Severity)}] {File}{(Line > 0 ? " " + Line + "번째 줄" : "")} — {Kind}: {Reason}";
    }

    public class Stats
    {
        public int Total, Tap, Long, ChordTimes;
        public double First, LastEnd, Length;
    }

    public class Result
    {
        public List<Issue> Issues = new List<Issue>();
        public Stats Stats = new Stats();
        public bool HasProblem => Issues.Any(i => i.Severity != Severity.Warning);
    }

    private struct ParsedNote
    {
        public NoteData Data;
        public int Line;
        public double End => Data.IsLong ? Data.EndTime : Data.Time;
    }

    public static string SeverityLabel(Severity s)
    {
        switch (s)
        {
            case Severity.Fatal: return "치명";
            case Severity.LineSkipped: return "줄 건너뜀";
            case Severity.Unplayable: return "칠 수 없는 노트";
            default: return "경고";
        }
    }

    // 채보 1개 검사. music이 있으면 음원 길이와 비교한다.
    public static Result Validate(TextAsset chart, AudioClip music, int laneCount = LaneCount)
    {
        var result = new Result();
        if (chart == null)
        {
            result.Issues.Add(new Issue { Severity = Severity.Fatal, File = "(없음)", Kind = "채보 참조 없음", Reason = "TextAsset이 지정되지 않음" });
            return result;
        }
        string file = chart.name + ".txt";

        // ① 줄 검사 (ChartLoader와 같은 규칙), 줄 번호 보존
        var notes = new List<ParsedNote>();
        string[] lines = chart.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (!ChartLoader.TryParseLine(lines[i], laneCount, out NoteData note, out bool hasNote, out string error))
            {
                result.Issues.Add(new Issue { Severity = Severity.LineSkipped, File = file, Line = i + 1, Kind = "잘못된 줄", Reason = error + " (\"" + lines[i].Trim() + "\")" });
                continue;
            }
            if (hasNote) notes.Add(new ParsedNote { Data = note, Line = i + 1 });
        }
        if (notes.Count == 0)
        {
            result.Issues.Add(new Issue { Severity = Severity.Fatal, File = file, Kind = "노트 없음", Reason = "읽을 수 있는 노트가 0개 (빈 채보 또는 모든 줄이 잘못됨)" });
            return result;
        }

        // ② 같은 레인: 시각순으로 앞 노트(롱노트는 종료)와 다음 노트 비교
        for (int lane = 0; lane < laneCount; lane++)
        {
            var laneNotes = notes.Where(n => n.Data.Lane == lane).OrderBy(n => n.Data.Time).ThenBy(n => n.Line).ToList();
            for (int i = 1; i < laneNotes.Count; i++)
            {
                ParsedNote prev = laneNotes[i - 1], cur = laneNotes[i];
                if (cur.Data.Time == prev.Data.Time)
                    result.Issues.Add(new Issue { Severity = Severity.Unplayable, File = file, Line = cur.Line, Kind = "같은 레인 중복", Reason = $"{prev.Line}번째 줄과 같은 시각 {cur.Data.Time:0.####}초·레인 {lane} (한 번 누르면 하나만 판정, 나머지는 Miss)" });
                else if (prev.Data.IsLong && cur.Data.Time <= prev.Data.EndTime)
                    result.Issues.Add(new Issue { Severity = Severity.Unplayable, File = file, Line = cur.Line, Kind = "롱노트와 겹침", Reason = $"{prev.Line}번째 줄 롱노트({prev.Data.Time:0.####}~{prev.Data.EndTime:0.####}초) 안에서 시작 ({cur.Data.Time:0.####}초) — 누르고 있는 동안 다시 누를 수 없음" });
                else if (cur.Data.Time - prev.End < MinLaneGap)
                    result.Issues.Add(new Issue { Severity = Severity.Warning, File = file, Line = cur.Line, Kind = "같은 레인 간격 짧음", Reason = $"{prev.Line}번째 줄{(prev.Data.IsLong ? "(롱노트 종료)" : "")}과 {(cur.Data.Time - prev.End) * 1000:0}ms 간격 (기준 {MinLaneGap * 1000:0}ms)" });
            }
        }

        // ③ 음원 길이
        if (music != null)
        {
            foreach (ParsedNote n in notes)
            {
                if (n.End >= music.length)
                    result.Issues.Add(new Issue { Severity = Severity.Fatal, File = file, Line = n.Line, Kind = "음원 길이 초과", Reason = $"노트 {(n.Data.IsLong ? "종료 " : "")}{n.End:0.####}초 ≥ 음원 {music.length:0.##}초 (음악이 끝나면 곡 시간이 돌아가 판정되지 않고 결과로 넘어가지 못할 수 있음)" });
                else if (n.End > music.length - MusicEndMargin)
                    result.Issues.Add(new Issue { Severity = Severity.Warning, File = file, Line = n.Line, Kind = "음원 끝에 가까움", Reason = $"노트 {(n.Data.IsLong ? "종료 " : "")}{n.End:0.####}초, 음원 {music.length:0.##}초까지 {(music.length - n.End) * 1000:0}ms" });
            }
        }

        // ④ 분석 수치
        Stats s = result.Stats;
        s.Total = notes.Count;
        s.Long = notes.Count(n => n.Data.IsLong);
        s.Tap = s.Total - s.Long;
        s.ChordTimes = notes.GroupBy(n => System.Math.Round(n.Data.Time, 4)).Count(g => g.Count() > 1);
        s.First = notes.Min(n => n.Data.Time);
        s.LastEnd = notes.Max(n => n.End);
        s.Length = s.LastEnd - s.First;
        return result;
    }

    // 곡 하나의 검사 대상 (난이도 곡이면 Easy/Normal/Hard, 아니면 chart 하나). 빈 난이도는 경고.
    public static List<(string label, TextAsset chart, Result result)> ValidateSong(SongData song)
    {
        var list = new List<(string, TextAsset, Result)>();
        if (song.HasDifficulties)
        {
            foreach (Difficulty d in System.Enum.GetValues(typeof(Difficulty)))
            {
                TextAsset chart = song.GetChart(d);
                Result r;
                if (chart == null)
                {
                    r = new Result();
                    r.Issues.Add(new Issue { Severity = Severity.Warning, File = "(없음)", Kind = "난이도 채보 비어 있음", Reason = d + " 칸이 비어 있어 이 난이도는 플레이할 수 없음(곡 선택에서 START 비활성)" });
                }
                else r = Validate(chart, song.Music);
                list.Add((d.ToString(), chart, r));
            }
        }
        else
        {
            TextAsset chart = song.GetChart(Difficulty.Normal);
            Result r = chart != null ? Validate(chart, song.Music) : new Result();
            if (chart == null) r.Issues.Add(new Issue { Severity = Severity.Fatal, File = "(없음)", Kind = "채보 참조 없음", Reason = "chart 칸과 난이도 칸이 모두 비어 있음" });
            list.Add(("Single", chart, r));
        }
        return list;
    }

    [MenuItem("Tools/Validate Charts")]
    public static void ValidateAll()
    {
        var songList = AssetDatabase.LoadAssetAtPath<SongList>(SongListPath);
        if (songList == null || songList.Songs == null)
        {
            Debug.LogError("ChartValidator: " + SongListPath + "을 찾을 수 없습니다.");
            return;
        }
        int charts = 0, problems = 0, warnings = 0;
        foreach (SongData song in songList.Songs)
        {
            if (song == null) { Debug.LogError("ChartValidator: SongList에 빈 칸이 있습니다."); problems++; continue; }
            foreach (var (label, chart, r) in ValidateSong(song))
            {
                charts++;
                Stats s = r.Stats;
                string head = $"[{song.RecordId} / {label}] {(chart != null ? chart.name + ".txt" : "(채보 없음)")}";
                string summary = s.Total > 0
                    ? $" — 노트 {s.Total} (일반 {s.Tap}, 롱 {s.Long}), 동시 {s.ChordTimes}곳, 첫 {s.First:0.##}초, 마지막 종료 {s.LastEnd:0.##}초, 길이 {s.Length:0.##}초"
                    : "";
                int p = r.Issues.Count(i => i.Severity != Severity.Warning), w = r.Issues.Count - p;
                problems += p; warnings += w;
                string line = head + summary + (r.Issues.Count == 0 ? " — 문제 없음" : $" — 문제 {p}, 경고 {w}");
                if (p > 0) Debug.LogError(line); else if (w > 0) Debug.LogWarning(line); else Debug.Log(line);
                foreach (Issue issue in r.Issues)
                {
                    if (issue.Severity == Severity.Warning || issue.Severity == Severity.LineSkipped) Debug.LogWarning("  " + issue);
                    else Debug.LogError("  " + issue);
                }
            }
        }
        string total = $"ChartValidator: 채보 {charts}개 검사 — 문제 {problems}, 경고 {warnings}";
        if (problems > 0) Debug.LogError(total); else if (warnings > 0) Debug.LogWarning(total); else Debug.Log(total);
    }
}
