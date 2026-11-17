// 곡·난이도 하나의 플레이 기록. PlayRecordStore가 JSON(JsonUtility)으로 저장하므로 public 필드로 둔다.
// 최고값(점수·콤보·정확도·랭크)은 각각 따로, 기존보다 높을 때만 갱신하고 낮아지지 않는다.
// 마지막 결과와 플레이 횟수는 정상 완료마다 갱신한다. playCount가 0이면 미플레이.
[System.Serializable]
public class PlayRecord
{
    public int playCount;
    public int bestScore;
    public int bestCombo;
    public float bestAccuracy;
    public string bestRank = "";

    public int lastScore;
    public int lastCombo;
    public float lastAccuracy;
    public string lastRank = "";
    public int lastPerfect;
    public int lastGreat;
    public int lastGood;
    public int lastMiss;

    public bool HasRecord => playCount > 0;

    // 정상 완료 1회를 반영한다. 최고 점수가 올라갔으면(첫 기록 포함) true (결과 화면 NEW BEST)
    public bool Apply(PlayResult result)
    {
        bool newBest = !HasRecord || result.Score > bestScore;
        if (newBest) bestScore = result.Score;
        if (result.MaxCombo > bestCombo) bestCombo = result.MaxCombo;
        if (result.Accuracy > bestAccuracy) bestAccuracy = result.Accuracy;
        if (PlayRecordStore.RankOrder(result.Rank) > PlayRecordStore.RankOrder(bestRank)) bestRank = result.Rank;

        lastScore = result.Score;
        lastCombo = result.MaxCombo;
        lastAccuracy = result.Accuracy;
        lastRank = result.Rank;
        lastPerfect = result.Perfect;
        lastGreat = result.Great;
        lastGood = result.Good;
        lastMiss = result.Miss;
        playCount++;
        return newBest;
    }
}
