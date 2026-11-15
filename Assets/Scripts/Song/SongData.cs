using UnityEngine;

// 곡 하나의 정보 (제목, 아티스트, 채보, 음악). 인스펙터에서 곡을 추가·수정한다.
[CreateAssetMenu(fileName = "SongData", menuName = "music_game/Song Data")]
public class SongData : ScriptableObject
{
    [SerializeField] private string title;
    [SerializeField] private string artist;
    [SerializeField] private TextAsset chart; // "시각,레인[,종료시각]" 형식 채보
    [SerializeField] private AudioClip music; // 선택. 없으면 시작 후 경과 시간으로 곡 시간을 잰다

    public string Title => title;
    public string Artist => artist;
    public TextAsset Chart => chart;
    public AudioClip Music => music;
}
