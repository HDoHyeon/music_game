using System;

// 채보에 기록된 노트 하나의 데이터 (판정 시각, 레인, 롱노트 종료 시각)
[Serializable]
public struct NoteData
{
    public double Time;    // 판정선에 도달해야 하는 곡 시간(초). 롱노트는 시작 시각
    public int Lane;       // 0부터 시작하는 레인 번호
    public double EndTime; // 롱노트 종료 시각(초). 일반 노트는 0

    // 종료 시각이 시작 시각보다 늦으면 롱노트
    public bool IsLong => EndTime > Time;

    public NoteData(double time, int lane, double endTime = 0)
    {
        Time = time;
        Lane = lane;
        EndTime = endTime;
    }
}
