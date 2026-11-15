using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// TextAsset 채보를 노트 데이터 목록으로 변환한다.
// 형식: 한 줄에 "시각(초),레인번호" (예: 2.0,1). 빈 줄은 무시한다.
// 롱노트: "시작시각,레인번호,종료시각" (예: 8.0,0,9.0). 종료 시각은 시작보다 늦어야 한다.
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
            string line = lines[i].Trim();
            if (line.Length == 0) continue;

            string[] parts = line.Split(',');
            double endTime = 0;
            if ((parts.Length != 2 && parts.Length != 3)
                || !double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double time)
                || !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lane)
                || time < 0 || lane < 0 || lane >= laneCount
                // 3번째 칸(롱노트 종료 시각)이 있으면 숫자이고 시작보다 늦어야 한다
                || (parts.Length == 3
                    && (!double.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out endTime)
                        || endTime <= time)))
            {
                Debug.LogWarning($"ChartLoader: {chart.name} {i + 1}번째 줄을 건너뜁니다: \"{line}\"");
                continue;
            }

            notes.Add(new NoteData(time, lane, endTime));
        }

        notes.Sort((a, b) => a.Time.CompareTo(b.Time));
        return notes;
    }
}
