using UnityEngine;

// 곡 선택 화면에 표시할 곡 목록. 순서대로 목록에 나온다.
[CreateAssetMenu(fileName = "SongList", menuName = "music_game/Song List")]
public class SongList : ScriptableObject
{
    [SerializeField] private SongData[] songs;

    public SongData[] Songs => songs;
}
