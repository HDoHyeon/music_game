using UnityEngine;

// 플레이 기록 저장소. PlayerPrefs에 곡·난이도마다 키 1개("Record.<곡ID>.<난이도>")로 PlayRecord를 JSON 문자열로 저장한다.
// 난이도 채보가 없는 곡(Test Song)은 난이도 대신 "Single". 다른 설정 키(KeyBinding·TimingOffsetMs·NoteSpeed·볼륨)와 겹치지 않는다.
// 없는 키·읽을 수 없는 문자열은 "기록 없음"으로 보고 게임은 계속된다.
public static class PlayRecordStore
{
    private const string Prefix = "Record.";
    public const string SingleKey = "Single";
    private static readonly string[] RankLadder = { "D", "C", "B", "A", "S", "SS" }; // 낮은 → 높은

    // 곡·난이도의 저장 키
    public static string Key(SongData song, Difficulty difficulty)
    {
        return Prefix + song.RecordId + "." + (song.HasDifficulties ? difficulty.ToString() : SingleKey);
    }

    // 기록을 읽는다. 없으면 빈 기록(미플레이), 손상되었으면 경고 후 빈 기록
    public static PlayRecord Load(SongData song, Difficulty difficulty)
    {
        if (song == null) return new PlayRecord();
        string key = Key(song, difficulty);
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json)) return new PlayRecord();
        try
        {
            PlayRecord record = JsonUtility.FromJson<PlayRecord>(json);
            if (record == null) return new PlayRecord();
            if (record.bestRank == null) record.bestRank = "";
            if (record.lastRank == null) record.lastRank = "";
            return record;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("PlayRecordStore: " + key + " 기록을 읽지 못해 기록 없음으로 처리합니다 (" + e.Message + ")");
            return new PlayRecord();
        }
    }

    // 기록을 저장한다 (정상 완료 1회에 1번)
    public static void Save(SongData song, Difficulty difficulty, PlayRecord record)
    {
        if (song == null || record == null) return;
        PlayerPrefs.SetString(Key(song, difficulty), JsonUtility.ToJson(record));
        PlayerPrefs.Save();
    }

    // 랭크 높낮이: D < C < B < A < S < SS. 빈 값(기록 없음)·모르는 값은 -1로 가장 낮다
    public static int RankOrder(string rank)
    {
        return System.Array.IndexOf(RankLadder, rank);
    }
}
