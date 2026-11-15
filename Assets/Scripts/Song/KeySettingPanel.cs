using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

// 곡 선택 화면의 키 설정 패널: 레인 버튼을 누르고 새 키를 누르면 KeyBindings에 적용·저장한다.
// 키 대기 중 Esc는 취소, 금지 키(Enter·방향키)는 "CANNOT USE" 표시. RESET으로 D/F/J/K 복원.
public class KeySettingPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;   // 패널 전체 (처음엔 숨김)
    [SerializeField] private Button openButton;      // 곡 선택 화면의 KEY SETTING 버튼
    [SerializeField] private Button closeButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button[] laneButtons;   // 인덱스 = 레인
    [SerializeField] private Text messageText;
    [SerializeField] private Color normalColor = new Color(0.25f, 0.25f, 0.3f, 1f);
    [SerializeField] private Color waitingColor = new Color(1f, 0.6f, 0.1f, 1f); // 키 입력을 기다리는 레인 색

    private int waitingLane = -1; // 키 입력을 기다리는 레인 (-1: 대기 안 함)

    // 패널이 열려 있는지 (열려 있는 동안 곡 선택 화면은 ↑↓/Enter를 무시한다)
    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    // 버튼을 연결하고 패널을 닫힌 상태로 시작한다.
    private void Start()
    {
        if (openButton != null) openButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (resetButton != null) resetButton.onClick.AddListener(ResetKeys);
        for (int i = 0; i < laneButtons.Length; i++)
        {
            int lane = i;
            laneButtons[i].onClick.AddListener(() => BeginRebind(lane));
        }
        if (panelRoot != null) panelRoot.SetActive(false);
        Refresh();
    }

    // 키 입력 대기 중이면 이번 프레임에 눌린 키를 찾아 적용한다.
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (waitingLane < 0 || keyboard == null) return;
        foreach (KeyControl control in keyboard.allKeys)
        {
            if (control == null || !control.wasPressedThisFrame) continue;
            ApplyKey(control.keyCode);
            break;
        }
    }

    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        waitingLane = -1;
        SetMessage("");
        Refresh();
    }

    public void Close()
    {
        waitingLane = -1;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // 레인 버튼 클릭: 이 레인의 새 키 입력을 기다린다.
    public void BeginRebind(int lane)
    {
        waitingLane = lane;
        // 클릭한 버튼이 선택 상태로 남으면 Space/Enter가 UI 제출로도 처리되어 다시 대기에 들어가므로 선택을 해제한다
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        SetMessage("PRESS A KEY  (ESC: CANCEL)");
        Refresh();
    }

    // 대기 중인 레인에 키를 적용한다. Esc는 취소, 금지 키는 안내 후 계속 대기.
    public void ApplyKey(Key key)
    {
        if (waitingLane < 0) return;
        if (key == Key.Escape)
        {
            waitingLane = -1;
            SetMessage("CANCELED");
        }
        else if (!KeyBindings.TrySet(waitingLane, key))
        {
            SetMessage("CANNOT USE  " + key);
            return;
        }
        else
        {
            waitingLane = -1;
            SetMessage("");
        }
        Refresh();
    }

    // RESET: 기본 키(D/F/J/K)로 되돌린다.
    public void ResetKeys()
    {
        KeyBindings.Reset();
        waitingLane = -1;
        SetMessage("RESET TO D / F / J / K");
        Refresh();
    }

    // 레인 버튼 글자(LANE n  키)와 대기 중 강조 색을 갱신한다.
    private void Refresh()
    {
        for (int i = 0; i < laneButtons.Length; i++)
        {
            Text label = laneButtons[i].GetComponentInChildren<Text>();
            if (label != null) label.text = "LANE " + (i + 1) + "   " + KeyBindings.Get(i);
            laneButtons[i].image.color = i == waitingLane ? waitingColor : normalColor;
        }
    }

    private void SetMessage(string value)
    {
        if (messageText != null) messageText.text = value;
    }
}
