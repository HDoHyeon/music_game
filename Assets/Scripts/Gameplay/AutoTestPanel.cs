using UnityEngine;
using UnityEngine.UI;

// 화면 왼쪽 위 AUTO TEST 패널: [0]~[3] 버튼으로 자동 플레이 단계를 바꾸고, 선택된 버튼을 강조한다.
public class AutoTestPanel : MonoBehaviour
{
    [SerializeField] private AutoPlayer autoPlayer;
    [SerializeField] private Button[] levelButtons;                                  // 인덱스 = 단계(0~3)
    [SerializeField] private Color normalColor = new Color(0.25f, 0.25f, 0.25f, 1f);  // 선택 안 된 버튼 색
    [SerializeField] private Color selectedColor = new Color(1f, 0.6f, 0.1f, 1f);     // 선택된 버튼 색

    private int shownLevel = -1; // 현재 강조 표시 중인 단계

    // 버튼마다 클릭 시 해당 단계로 바꾸도록 연결한다.
    private void Start()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int level = i;
            levelButtons[i].onClick.AddListener(() => SelectLevel(level));
        }
        RefreshHighlight();
    }

    // 다른 경로(인스펙터, MCP)로 단계가 바뀐 경우에도 강조 표시를 맞춘다.
    private void Update()
    {
        if (autoPlayer != null && autoPlayer.Level != shownLevel) RefreshHighlight();
    }

    // 버튼 클릭: 자동 플레이 단계를 바꾼다.
    public void SelectLevel(int level)
    {
        if (autoPlayer == null) return;
        autoPlayer.Level = level;
        RefreshHighlight();
    }

    // 선택된 단계 버튼만 강조 색으로 칠한다.
    private void RefreshHighlight()
    {
        if (autoPlayer == null) return;
        shownLevel = autoPlayer.Level;
        for (int i = 0; i < levelButtons.Length; i++)
            levelButtons[i].image.color = i == shownLevel ? selectedColor : normalColor;
    }
}
