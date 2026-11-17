using UnityEngine;

// 곡 하나의 정보 (제목, 아티스트, 채보, 음악, 곡 기본 볼륨). 인스펙터에서 곡을 추가·수정한다.
// 난이도별 채보(Easy/Normal/Hard)를 하나라도 넣으면 난이도 곡이 되고, 모두 비우면 chart 하나로 플레이한다(Test Song).
[CreateAssetMenu(fileName = "SongData", menuName = "music_game/Song Data")]
public class SongData : ScriptableObject
{
    [SerializeField] private string title;
    [SerializeField] private string artist;
    [SerializeField] private TextAsset chart; // "시각,레인[,종료시각]" 형식 채보
    [SerializeField] private AudioClip music; // 선택. 없으면 시작 후 경과 시간으로 곡 시간을 잰다
    [SerializeField, Range(0f, 1f)] private float volume = 1f; // 곡 기본 볼륨: 곡끼리 소리 크기 차이를 맞춤 (음원은 그대로, 재생 시 곱함)
    [Header("난이도별 채보 (비우면 위의 chart 하나로 플레이)")]
    [SerializeField] private TextAsset easyChart;
    [SerializeField] private TextAsset normalChart;
    [SerializeField] private TextAsset hardChart;

    public string Title => title;
    public string Artist => artist;
    public TextAsset Chart => chart;
    public AudioClip Music => music;
    public float Volume => volume;

    // 난이도 채보가 하나라도 있으면 난이도 곡 (곡 선택 화면에 난이도 버튼 표시)
    public bool HasDifficulties => easyChart != null || normalChart != null || hardChart != null;

    // 플레이할 채보. 난이도 곡이면 그 난이도 채보(없으면 null → 플레이 불가), 아니면 기존 chart
    public TextAsset GetChart(Difficulty difficulty)
    {
        if (!HasDifficulties) return chart;
        switch (difficulty)
        {
            case Difficulty.Easy: return easyChart;
            case Difficulty.Hard: return hardChart;
            default: return normalChart;
        }
    }
}
