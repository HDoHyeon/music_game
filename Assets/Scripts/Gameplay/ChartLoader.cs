using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// TextAsset 채보를 노트 데이터 목록으로 변환한다.
// 형식: 한 줄에 "시각(초),레인번호" (예: 2.0,1). 빈 줄은 무시한다.
// 롱노트: "시작시각,레인번호,종료시각" (예: 8.0,0,9.0). 종료 시각은 시작보다 늦어야 한다.
// 한 줄 검사(TryParseLine)는 에디터의 채보 검사기(ChartValidator)도 같이 쓴다.
public static class ChartLoader
{
    // 채보를 읽어 시각순으로 정렬된 노트 목록을 반환한다. 잘못된 줄은 경고 후 건너뛴다.
    public static List<NoteData> Load(TextAsset chart, int laneCount)
    {
        var notes = new List<NoteData>();
        if (chart == null)
        {
            Debug.LogWarning("ChartLoader: 채보가 지정되지 않았습니다.");
            return notes;
        }

        string[] lines = chart.text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (!TryParseLine(lines[i], laneCount, out NoteData note, out bool hasNote, out string error))
            {
                Debug.LogWarning($"ChartLoader: {chart.name} {i + 1}번째 줄을 건너뜁니다: \"{lines[i].Trim()}\"");
                continue;
            }
            if (hasNote) notes.Add(note); // 빈 줄은 hasNote = false
        }

        notes.Sort((a, b) => a.Time.CompareTo(b.Time));
        return notes;
    }

    // 채보 한 줄을 노트로 바꾼다. 빈 줄이면 hasNote = false로 true(건너뜀).
    // 칸 수(2·3), 숫자 변환, 시각 ≥ 0, 레인 범위, 롱노트 종료 > 시작을 확인하고, 틀리면 false와 이유(error)를 돌려준다.
    public static bool TryParseLine(string rawLine, int laneCount, out NoteData note, out bool hasNote, out string error)
    {
        note = default;
        hasNote = false;
        error = null;
        string line = rawLine.Trim();
        if (line.Length == 0) return true;

        string[] parts = line.Split(',');
        if (parts.Length != 2 && parts.Length != 3) { error = "칸 수가 2개(시각,레인) 또는 3개(시작,레인,종료)가 아님"; return false; }
        if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double time)) { error = "시각이 숫자가 아님"; return false; }
        if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lane)) { error = "레인이 정수가 아님"; return false; }
        if (time < 0) { error = "시각이 음수"; return false; }
        if (lane < 0 || lane >= laneCount) { error = "레인이 범위(0~" + (laneCount - 1) + ") 밖"; return false; }

        double endTime = 0;
        if (parts.Length == 3)
        {
            // 3번째 칸(롱노트 종료 시각)이 있으면 숫자이고 시작보다 늦어야 한다
            if (!double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out endTime)) { error = "롱노트 종료 시각이 숫자가 아님"; return false; }
            if (endTime <= time) { error = "롱노트 종료 시각이 시작보다 늦지 않음"; return false; }
        }

        note = new NoteData(time, lane, endTime);
        hasNote = true;
        return true;
    }
}
