using UnityEngine;

// 롱노트 진행 상태: 대기 → 유지 → 완료 또는 실패
public enum HoldState
{
    Waiting,   // 아직 시작 판정 전
    Holding,   // 시작 판정 성공 후 키를 누르고 있는 중
    Completed, // 종료 시각까지 유지 성공
    Failed     // 시작을 놓쳤거나 너무 일찍 뗌
}

// 화면에 표시되는 노트 오브젝트. 곡 시간에 맞춰 판정선을 향해 내려온다.
public class Note : MonoBehaviour
{
    [SerializeField] private Transform body;        // 롱노트 몸통 (일반 노트는 숨김)
    [SerializeField] private float bodyWidth = 0.6f; // 몸통 폭(월드 유닛)
    [SerializeField] private Color missedColor = new Color(0.5f, 0.5f, 0.5f, 0.4f); // 롱노트 Miss 표시 색 (회색 반투명)

    public NoteData Data { get; private set; }
    public bool IsJudged { get; set; } // 최종 판정이 끝난 노트는 다시 판정하지 않기 위한 플래그
    public HoldState State { get; set; }
    public Judgement HeadJudgement { get; set; } // 롱노트 시작 판정 등급 (틱 점수 계산용)
    public double NextTickTime { get; set; }     // 롱노트 다음 틱(유지) 판정 시각

    // 생성 직후 노트 데이터와 레인 X 위치를 지정한다.
    public void Init(NoteData data, float laneX)
    {
        Data = data;
        IsJudged = false;
        State = HoldState.Waiting;
        transform.position = new Vector3(laneX, transform.position.y, transform.position.z);
        if (body != null) body.gameObject.SetActive(data.IsLong);
    }

    // 롱노트가 Miss(시작 놓침·조기 해제)를 받았을 때 머리와 몸통을 회색 반투명으로 바꾼다.
    public void ShowMissed()
    {
        SpriteRenderer head = GetComponent<SpriteRenderer>();
        if (head != null) head.color = missedColor;
        if (body != null)
        {
            SpriteRenderer bodyRenderer = body.GetComponent<SpriteRenderer>();
            if (bodyRenderer != null) bodyRenderer.color = missedColor;
        }
    }

    // 곡 시간 기준으로 Y 위치를 계산한다. 노트 시각에 정확히 판정선(judgeLineY)에 도달한다.
    // 롱노트는 유지 중이거나 Miss(Failed) 표시 중이면 머리를 판정선에 고정하고, 머리~꼬리(종료 시각) 길이로 몸통을 늘린다.
    public void UpdatePosition(double songTime, float judgeLineY, float fallSpeed)
    {
        float headY = State == HoldState.Holding || State == HoldState.Failed
            ? judgeLineY
            : judgeLineY + (float)(Data.Time - songTime) * fallSpeed;
        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, headY, pos.z);

        if (!Data.IsLong || body == null) return;

        float tailY = judgeLineY + (float)(Data.EndTime - songTime) * fallSpeed;
        float length = Mathf.Max(0f, tailY - headY);
        // 몸통은 노트(부모)의 스케일 영향을 받으므로 월드 크기가 나오도록 부모 스케일로 나눈다.
        Vector3 parentScale = transform.localScale;
        body.localScale = new Vector3(bodyWidth / parentScale.x, length / parentScale.y, 1f);
        body.localPosition = new Vector3(0f, length * 0.5f / parentScale.y, 0f);
    }
}
