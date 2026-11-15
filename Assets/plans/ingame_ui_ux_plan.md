# 인게임 UI/UX 개선 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 대상: PLAN.md 2-4. UI/UX / 1. 인게임 UI/UX
- 목적: 기존 플레이 규칙(판정·점수·콤보·롱노트·배속·오프셋·결과 전환)은 그대로 두고, 인게임 화면을 예시 이미지(Assets/ex_image/ex_ingame.png) 구성에 맞춰 개선한다.
- 상태: 구현 완료(2026-11-16) / 사용자 확인 대기 (실제 키 입력 빛, 화면 가독성, kick 실시간 진행 시간, 해상도별 화면)
- 작성 경위: GPT 초안을 로컬 소스(커밋 d2cadb6 + 작업 트리)와 대조해 검토하고, 사용자 결정(2026-11-16)을 반영해 다시 작성.
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항 (사용자 확정 2026-11-16)
| # | 기능 | 내용 |
|---|---|---|
| 1 | 판정 표시 | Assets/ex_image의 note_perfect / great / good / miss 이미지를 Sprite로 사용 |
| 2 | 콤보 | "N COMBO" 대신 숫자만 화면 중앙에 표시, 증가 시 확대 + 터지는 이펙트, 노트를 가리지 않게 반투명 |
| 3 | 점수 패널 | 오른쪽 위 사선 테두리 패널 + "SCORE" 라벨 + 천 단위 쉼표 점수 |
| 4 | 레인 입력 빛 | 누르고 있는 레인에 아래에서 위로 옅어지는 빛 기둥 |
| 5 | 레인 / 판정선 | 4레인 어두운 배경, 레인 구분선, 레인별로 끊긴 빛나는 판정선 |
| 6 | 원근 바닥 | 판정선 아래로 레인 구분선이 바깥쪽으로 퍼지는 바닥 |
| 7 | 진행 시간 | 화면 맨 위 가로 진행 바(초록) + 오른쪽 위 "mm:ss / mm:ss" |

- 신규 에셋(스프라이트, 폰트)은 추가할 수 있으나 **추가 전에 사용자에게 묻는다.** (4.8 목록, 이 계획 승인 = 목록 승인, 폰트 다운로드는 실행 직전 다시 확인)
- 기존 판정·점수·콤보 계산, 오프셋, 배속, 레인 위치(laneXs), 판정선 Y, 노트 프리팹은 바꾸지 않는다.

### 제외 범위
- 노트(일반·롱노트) 프리팹 디자인 변경 (자동 싱크 화면이 같은 프리팹을 쓰므로 그 화면 모양도 그대로)
- 레인 폭·위치 변경 (예시 이미지는 레인이 화면 대비 더 넓지만, 레인 좌표는 판정·보정 화면과 묶여 있어 유지)
- 레인별 입력 키 글자 표시
- 곡 선택 / 결과 / 자동 싱크 화면 개편 (PLAN.md 2-4 #2, #3 별도)
- 패키지 추가, 프로젝트 설정 변경 (TextMeshPro 미사용, 기존 uGUI Text 유지)

## 2. 기존 코드 분석 (로컬 확인 2026-11-16)
- **IngameScene Canvas**: Screen Space Overlay, CanvasScaler Scale With Screen Size 1920×1080, Match Width(0). 글꼴은 전부 내장 LegacyRuntime.
  - ScoreText: 오른쪽 위 anchoredPosition (-40, -30), 600×120, 64, 오른쪽 정렬, 흰색
  - ComboText: 위쪽 가운데 (0, -200), 600×120, 80, 가운데 정렬, 흰색
  - AutoTestPanel: 왼쪽 위 (30, -30), 360×140
  - 순서: … ScoreText / ComboText / AutoTestPanel / PausePanel / CountdownText / FadeOverlay
- **월드**: Main Camera 직교 크기 5(화면 y −5~5, 16:9에서 x 약 ±8.9). JudgeLine은 2D 패키지 기본 Square 스프라이트, 위치 (0, −3), 크기 (4, 0.05), sortingOrder 0. Global Light 2D 있음.
- **Note.prefab**: Square 스프라이트, 몸통 sortingOrder −1, 머리 0. → 레인 배경·구분선·빛 기둥은 −1보다 뒤(−10 이하)에 둬야 노트를 가리지 않는다.
- **GameplayManager**: laneXs {−1.5, −0.5, 0.5, 1.5}, spawnY 6, judgeLineY −3, fallSpeed 5. 이벤트 `OnJudged(Judgement, lane)`(일반 노트, 롱노트 시작·끝 판정, Miss 포함), `OnTick(Judgement, lane)`. `IsPaused`, `SongTime`(오프셋 적용), `IsMusicFinished`는 public. `audioSource`, `chartNotes`, `IsHeld(lane, songTime)`(AUTO면 AutoPlayer, 아니면 키보드)는 private.
- **ScoreComboState**: OnJudged/OnTick 구독, `Score`/`Combo`/`MaxCombo` public get. `RefreshTexts()`가 `scoreText.text = Score.ToString()`, `comboText.text = Combo + " COMBO"`, 콤보 0이면 ComboText 비활성. 점수: Perfect 1000 / Great 700 / Good 300, 틱은 30%.
- **ResultTransition**: `gameplay.IsFinished && IsMusicFinished`이면 FadeOverlay 후 결과 씬. **PauseController**: Restart는 씬 다시 불러오기 → 신규 HUD 상태는 자동 초기화.
- **Judgement**: enum Perfect / Great / Good / Miss.
- **판정 이미지**: note_*.png 4장 모두 RGBA(투명 배경), 400~444×125. import 설정은 이미 Sprite(textureType 8)지만 spriteMode 2(Multiple) → Single로 바꿔야 Image에 바로 쓸 수 있다.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| Assets/Scripts/Gameplay/IngameHud.cs | 신규 | 콤보 확대·이펙트·반투명, 판정 이미지 표시, 진행 바/시간, 레인 입력 빛 |
| Assets/Scripts/Gameplay/GameplayManager.cs | 수정 (읽기 전용 추가만) | `MusicPosition`, `MusicDuration`, `ChartDuration`, `IsMusicPlaying`, `IsLaneHeld(lane)` |
| Assets/Scripts/Gameplay/ScoreComboState.cs | 수정 (표시 문자열 2줄) | 콤보 숫자만, 점수 쉼표 |
| Assets/Scenes/IngameScene.unity | 수정 | 레인 배경·구분선·판정선·바닥·빛 기둥, HUD UI, ScoreText·AutoTestPanel 위치 |
| Assets/Sprites/UI/ (신규 폴더) | 이동·신규 | 판정 이미지 4장 이동 + 생성 스프라이트 (4.8) |
| Assets/Fonts/ (신규 폴더) | 신규 (조건부) | 콤보·점수용 굵은 기울임 폰트 (4.8, 실행 전 확인) |
| Assets/Editor/UiSpriteGenerator.cs | 신규 | 생성 스프라이트를 PNG로 만드는 에디터 전용 메뉴 (게임 빌드에 포함 안 됨) |

수정하지 않는 파일: Note.cs, Note.prefab, AutoPlayer.cs, PauseController.cs, ResultTransition.cs, KeyBindings.cs, NoteSpeed.cs, OffsetSetting.cs, OffsetCalibration.cs와 그 씬.

### 중요 — GameplayManager / ScoreComboState 수정 (승인 필요)
- GameplayManager: 표시용 읽기 전용 속성 4개만 추가. 판정·SongTime·오디오 재생·채보 처리는 그대로.
- ScoreComboState: 계산은 그대로, `RefreshTexts()`의 문자열 2개만 변경. 결과 화면은 `MaxCombo` 값을 쓰므로 영향 없음.

## 4. 상세 구현 방법
좌표·색·크기 수치는 예시 이미지 기준 초기값이며, 스크린샷으로 확인하며 조정한다.

### 4.1 화면 배치 (1920×1080 기준)
| 요소 | 위치 |
|---|---|
| 진행 바 | 위 가운데, y −16, 폭 1620, 높이 12 (왼쪽 20 ~ 시간 글자 앞) |
| 진행 시간 | 오른쪽 위 (−30, −10), 36, "01:24 / 02:35" |
| 점수 패널 | 오른쪽 위 (−30, −70), 약 420×130 (ScoreText를 패널 안으로 이동) |
| AUTO TEST | 왼쪽 위 (30, −30) → (30, −50) (진행 바와 겹침 방지, 위치만 이동) |
| 콤보 숫자 | 가운데 (0, 120) — 노트가 지나가는 레인 위, 반투명 (사용자 요청으로 60에서 위로 이동) |
| 판정 이미지 | 가운데 (0, −150) 부근 — 콤보 아래, 판정선(화면 y −3 ≈ −324) 위 |

Canvas 순서: 진행 바·시간·점수 패널·콤보·판정 → AutoTestPanel → PausePanel / CountdownText → FadeOverlay (일시정지 메뉴와 페이드가 항상 위). 신규 UI는 전부 Raycast Target 끔.

### 4.2 콤보 (숫자만, 확대 + 이펙트 + 반투명)
- ScoreComboState: `comboText.text = Combo.ToString();` (숨김 규칙은 그대로)
- ComboText: 위치 (0, 120), 크기 420×140, 글자 105, 기울임 굵은 폰트(4.8), 흰색 알파 0.45, Outline(검정 알파 0.6, 3/−3). (사용자 피드백 2026-11-16: 처음 값 600×200·150·0.75에서 크기 70%, 투명도 낮춤)
- ComboBurst(Image, 생성 스프라이트 combo_burst, 294×294, 위치 (0, 120)): ComboText 뒤, 콤보가 오를 때 알파 0.3 → 0, 크기 0.8 → 1.3 (0.25초). (피드백 반영: 420×420·0.5에서 콤보와 같이 70%, 투명도 낮춤)
- 확대: 콤보가 오를 때마다 기본 크기의 1.25배에서 0.18초 동안 1.0으로(ease-out). 항상 기본 크기 기준으로 계산해 누적되지 않음. 틱으로 오를 때도 재생. 같은 프레임 여러 번 올라도 1회.
- 콤보 0(Miss)이면 이펙트 즉시 숨김.
- 정지·카운트다운(`gameplay.IsPaused`) 중에는 애니메이션 타이머 정지. 타이머는 `Time.unscaledDeltaTime`(곡 시간과 무관한 표시용).

```csharp
// 콤보가 오른 프레임에 확대와 이펙트를 다시 시작한다 (LateUpdate에서 호출)
private void UpdateCombo(float dt)
{
    int combo = scoreCombo.Combo;
    if (combo > previousCombo) comboElapsed = 0f;           // 증가: 처음부터 재생
    else if (combo == 0) comboElapsed = BurstDuration;      // Miss: 이펙트 끝 상태로
    previousCombo = combo;
    float p = Mathf.Clamp01(comboElapsed / PulseDuration);
    comboRect.localScale = comboBaseScale * (1f + (PulseScale - 1f) * (1f - p) * (1f - p));
    float b = Mathf.Clamp01(comboElapsed / BurstDuration);
    burst.color = new Color(1f, 1f, 1f, BurstAlpha * (1f - b));
    burstRect.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.3f, b);
    comboElapsed = Mathf.Min(BurstDuration, comboElapsed + dt);
}
```

### 4.3 점수 패널
- ScorePanel(Image, 생성 스프라이트 score_panel: 사선 평행사변형 테두리 + 어두운 반투명 안쪽 + 왼쪽 사선 2줄 장식).
- 안에 "SCORE" 라벨(Text, 24, 자간 넓게 보이도록 "S C O R E", 연보라)과 기존 ScoreText(위치만 패널 안으로, 글자 64, 기울임 폰트).
- ScoreComboState: `scoreText.text = Score.ToString("N0");` (253,400 형식. 쉼표는 문화권과 무관하게 나오도록 `CultureInfo.InvariantCulture` 사용)

### 4.4 판정 이미지
- 판정 이미지 4장을 `Assets/Sprites/UI/Judgement/`로 이동(.meta 함께 이동해 GUID 유지), spriteMode Single로 변경.
- JudgementImage(Image, Preserve Aspect) 1개를 두고 스프라이트를 바꿔 끼운다.
  - (피드백 반영 2026-11-16) 크기: 가운데 두 레인(F·J, 월드 x −1 ~ 1) 안에 들어오도록 판정마다 월드 폭 1.7유닛을 카메라로 화면 크기로 바꿔 맞춘다(`judgementWorldWidth`, 높이는 스프라이트 비율). 등장 확대 1.15배에도 1.96유닛으로 두 레인(2유닛) 안. 해상도가 달라도 레인 기준이라 같은 비율.
  - (피드백 반영) 최대 투명도 0.7 (`JudgementAlpha`), 끝 0.1초 동안 0으로.
- `OnJudged` 구독: 판정마다 해당 스프라이트로 바꾸고 0.4초 표시. 처음 0.08초 동안 1.15 → 1.0 확대, 마지막 0.1초 알파 감소.
- 롱노트: 시작(head)과 끝(tail) 판정 모두 OnJudged로 오므로 **둘 다 표시**. 틱(OnTick)은 판정 이미지 표시 안 함(콤보 애니메이션만).
- 같은 프레임 여러 판정 → 이벤트 순서상 마지막 판정 표시.
- 정지 중 타이머 정지, 재시작은 씬 재로딩으로 초기화.

```csharp
// 판정 1회마다 해당 등급 이미지로 바꾸고 표시 시간을 처음부터 센다
private void ShowJudgement(Judgement judgement, int lane)
{
    judgementImage.sprite = judgementSprites[(int)judgement]; // Perfect, Great, Good, Miss 순서
    judgementImage.gameObject.SetActive(true);
    judgementElapsed = 0f;
}
```

### 4.5 레인 / 판정선 / 원근 바닥 / 입력 빛 (월드 스프라이트, 씬에 배치)
`LaneStage` 빈 오브젝트 아래에 SpriteRenderer로 배치. 좌표는 laneXs와 judgeLineY(−3) 기준이며 값은 바꾸지 않는다.

| 오브젝트 | 스프라이트 | 위치·크기 | sortingOrder |
|---|---|---|---|
| LaneBackground ×4 | Square | 레인 중심 x, y 1.5, 크기 (0.98, 9) — 판정선 위 화면 끝까지, 짙은 남색 알파 0.6 | −20 |
| LaneDivider ×5 | Square | x −2, −1, 0, 1, 2, 판정선~화면 위, 폭 0.02, 하늘색 알파 0.5 | −15 |
| LaneGlow ×4 | lane_glow(아래 밝고 위로 투명) | 레인 중심, 판정선에서 위로 높이 6 | −12 |
| JudgeLineSegment ×4 | judge_glow(가운데 밝고 위아래 번짐) | 레인 중심, y −3, 폭 0.9(사이 간격 0.1), 높이 0.25 | −5 |
| LaneFloor | lane_floor(원근 사다리꼴 + 퍼지는 구분선) | 판정선 아래 y −3 ~ −5.2, 위쪽 폭 4 → 아래쪽 폭 약 5.2 | −20 |

- 판정선: 기존 JudgeLine을 이름으로 찾는 스크립트는 없다(Scripts 폴더 검색, 2026-11-16). 씬 안 참조는 구현 전 다시 확인한다. **SpriteRenderer만 끄고 오브젝트는 남긴다**(되돌리기 쉽게). 레인별 판정선 4개가 대신 보인다. 판정선은 노트(−1, 0)보다 뒤라 노트 머리를 가리지 않는다.
- 입력 빛: IngameHud가 매 프레임 `gameplay.IsLaneHeld(lane)`이면 해당 LaneGlow 알파를 0.45로, 떼면 0.1초 동안 0으로. 정지·카운트다운 중에는 꺼진다.
  - (구현 중 보완) AUTO의 일반 노트는 AutoPlayer가 "누르고 있음" 상태를 거의 만들지 않아 빛이 보이지 않는다. 그래서 Miss가 아닌 판정이 나면 그 레인을 0.45로 켰다가 0.1초 동안 끈다. 키보드 입력도 같은 방식으로 판정 순간 한 번 더 켜진다.
- 노트가 지나가는 레인 배경은 알파 0.6 이하로 두어 흰 노트·롱노트가 잘 보이게 한다.

```csharp
// GameplayManager: 레인을 누르고 있는지 (표시용, AUTO 1~3이면 자동 입력 기준). 정지 중에는 false
public bool IsLaneHeld(int lane) => !IsPaused && laneNotes != null && IsHeld(lane, SongTime);
```

### 4.6 진행 시간
- 시간 기준:
  - 음악 있는 곡: 현재 `audioSource.time`, 전체 `clip.length` (오프셋 적용 전 실제 음악 위치 → 오프셋·배속을 바꿔도 진행 시간은 그대로).
  - 음악 없는 곡(Test Song 1, 2): 현재 `SongTime`, 전체 = 채보 노트의 Time/EndTime 최댓값. 빈 채보는 00:00 / 00:00, 진행률 0.
- 음악 종료: 음악이 끝나면 `audioSource.time`이 0으로 돌아가므로, 한 번이라도 재생된 뒤(`IsMusicPlaying`) 멈추고 재생 위치가 0이면 100%로 고정. 시작 직후(재생 전)는 0%.
  - (구현 중 변경) 계획의 "재생된 뒤 멈추면 100%"는 에디터가 일시정지되었을 때(음악도 멈춤) 100%로 잘못 보여서, "재생 위치가 0으로 돌아간 경우"를 조건에 더했다. 이를 위해 `IsMusicPlaying`(읽기 전용)을 추가했다.
- 정지·카운트다운 중에는 GameplayManager가 음악을 Pause하므로 값이 그대로 유지된다.
- 표시: 초 단위 내림 mm:ss, 현재 시간은 0~전체로 제한. 진행 바는 Image Filled(Horizontal), 초록. 바 배경은 회색 반투명. 클릭 기능 없음.

```csharp
// GameplayManager: 진행 시간 표시용 (판정용 SongTime과 별개)
public double MusicPosition => UseAudioClock ? audioSource.time : SongTime;
public double MusicDuration => UseAudioClock ? audioSource.clip.length : ChartDuration;
public bool IsMusicPlaying => UseAudioClock && audioSource.isPlaying; // (구현 중 추가) 음악 종료 판단용
public double ChartDuration { get; private set; } // Start에서 ChartLoader.Load 직후 계산
```

### 4.7 기존 기능 보호
- 콤보·판정·HUD는 ScoreComboState/GameplayManager 값을 읽기만 한다.
- 신규 UI Raycast Target 끔 → AUTO TEST 버튼·일시정지 메뉴 클릭 방해 없음.
- 해상도: CanvasScaler 설정은 그대로. 1920×1080, 2560×1440, 3840×2160, 21:9, 4:3(16:10)에서 겹침·잘림 확인. 월드 레인은 카메라 높이 기준이라 비율이 좁아져도 레인은 잘리지 않음(±2 < 4:3 화면 반폭 6.7).

### 4.8 신규 에셋 목록 (사용자 확인 대상)
| 에셋 | 방법 | 용도 |
|---|---|---|
| Sprites/UI/Judgement/note_perfect, great, good, miss.png | ex_image에서 **이동** (새로 만들지 않음) | 판정 표시 |
| Sprites/UI/combo_burst.png (256×256) | UiSpriteGenerator로 생성 (방사형 광선 + 둥근 빛) | 콤보 이펙트 |
| Sprites/UI/score_panel.png (512×160) | 생성 (사선 테두리 패널) | 점수 패널 |
| Sprites/Stage/lane_glow.png (64×256) | 생성 (세로 그라데이션) | 레인 입력 빛 |
| Sprites/Stage/judge_glow.png (64×32) | 생성 (가운데 밝은 가로 빛) | 레인별 판정선 |
| Sprites/Stage/lane_floor.png (512×256) | 생성 (원근 사다리꼴 + 구분선) | 원근 바닥 |
| Fonts/(굵은 기울임 폰트).ttf | **외부 다운로드 — 실행 직전 파일명·출처·크기 확인 후 진행** | 콤보·점수 숫자 |

- 폰트 후보: Google Fonts의 OFL(무료·게임 포함 가능) 라이선스 폰트, 예: Exo 2 Black Italic. Windows 기본 폰트(Arial 등)는 라이선스상 게임에 넣지 않는다.
- 폰트를 쓰지 않기로 하면 내장 LegacyRuntime + Bold Italic 스타일로 대체(에셋 추가 없음).
- 생성 스프라이트는 에디터 메뉴로 한 번 만든 PNG를 커밋하며, 생성 스크립트는 Assets/Editor라 게임 빌드에 들어가지 않는다.

## 5. 구현 순서 및 진행 상태
- [x] GPT 초안 검토, 로컬 소스·씬·이미지 대조
- [x] 사용자 결정 반영 (2026-11-16)
- [x] 사용자 구현 승인 (2026-11-16)
- [x] 1단계: 판정 이미지 4장 Assets/Sprites/UI/Judgement/로 이동(.meta 함께, Single), UiSpriteGenerator로 스프라이트 5장 생성
- [x] 2단계: 폰트 추가 — 사용자 확인 후 Kanit-ExtraBoldItalic.ttf + OFL.txt를 Google Fonts 공식 GitHub(google/fonts, ofl/kanit)에서 받아 Assets/Fonts/에 추가
- [x] 3단계: GameplayManager 읽기 전용 속성, ScoreComboState 문자열 → 컴파일 오류 0
- [x] 4단계: 레인 스테이지(LaneStage: 배경·구분선·빛 기둥·레인별 판정선·바닥) 배치, 기존 JudgeLine은 SpriteRenderer만 끔
- [x] 5단계: IngameHud + UI 배치 (ProgressBg/TimeText/ScorePanel/ComboBurst/ComboText/JudgementImage, AUTO TEST 패널 y −30 → −50)
- [x] 6단계: 일시정지/재시작, AUTO 1~3 회귀 (해상도는 미검증, 7절)
- [ ] 사용자 실제 플레이 확인
- [x] 검증 결과 기록, PLAN.md 2-4 #1 상태 갱신(미작성 → 미확인)

## 6. 테스트 및 검증 방법
- C: Claude (컴파일, Play Mode 직접 호출, AUTO TEST, 스크린샷) / U: 사용자 (실제 키 입력, 화면 가독성, 청취)
- AUTO TEST만으로 실제 키 입력·가독성·음악 싱크를 통과 처리하지 않는다.

| ID | 테스트 | 기대 결과 | 주체 | 상태 |
|---|---|---|---|---|
| IU-01 | 컴파일·Play Mode | 신규 오류·참조 누락·Missing Script 없음 | C | 통과 — 컴파일 오류 0, Play 중 Error/Warning 0, IngameHud 참조 전부 연결 |
| IU-02 | 콤보 숫자만 표시 | "124"처럼 숫자만, 콤보 0이면 숨김 | C | 통과 |
| IU-03 | 콤보 증가 확대·이펙트 | 1.25배 → 1.0, 이펙트 페이드, 연속·틱에도 크기 누적 없음 | C/U | C 통과 (최대 1.250, 최소 1.000, 이펙트 최대 알파 0.50) / U 미검증 |
| IU-04 | 콤보 반투명 | 콤보 뒤로 내려오는 노트·롱노트가 보임 | C/U | C 통과 (스크린샷에서 콤보 뒤 노트·롱노트 보임) / U 미검증 |
| IU-05 | 점수 패널 | SCORE 라벨, 253,400 형식 쉼표, 패널 안에 맞음 | C | 통과 (72,000 / 263,200 표시, 스크린샷) |
| IU-06 | 판정 이미지 4종 | 실제 판정과 이미지 일치, 0.4초 후 사라짐 | C/U | C 통과 (AUTO 2에서 4종 모두 표시, 판정 로그 순서와 일치) / U 미검증 |
| IU-07 | 롱노트 판정 표시 | 시작·끝 판정 이미지, 틱은 이미지 없음 | C | 통과 (롱노트 head·tail 판정 이미지 표시, 틱은 이미지 변화 없음) |
| IU-08 | 연속·동시 판정 | 마지막 판정 이미지, 표시 시간 초기화 | C | 통과 (같은 프레임 Miss 2번 → 1번 표시, 마지막 판정) |
| IU-09 | 레인 배경·구분선·판정선 | 노트 중심과 레인 중심·판정선 위치 일치, 노트가 가려지지 않음 | C/U | C 통과 (스크린샷에서 노트가 레인 중앙·판정선에 맞음) / U 미검증 |
| IU-10 | 원근 바닥 | 판정선 아래에서 구분선이 바깥으로 퍼짐, 위쪽 레인과 이어짐 | C/U | C 통과 (스크린샷) / U 미검증 |
| IU-11 | 레인 입력 빛 | 누르는 레인만 빛남, 떼면 사라짐, AUTO에서도 동작, 정지 중 꺼짐 | C/U | C 통과 (AUTO 롱노트 유지 중 0.45, 판정 순간 켜졌다 0.1초 꺼짐, 정지 중 0) / U 통과 (2026-11-16 사용자 확인: D F J K 누를 때 빛남) |
| IU-12 | kick 진행 시간 | 실제 음악 위치·길이(01:30 부근) 표시, 끝나면 100% | C/U | C 통과 (음악 시계를 직접 진행: 30초에 00:29 / 01:30, 음악 정지·위치 0 → 01:30 / 01:30, 100%) / 실시간·U 미검증 |
| IU-13 | 오프셋·배속 변경 | 진행 시간 영향 없음 | C | 통과 (오프셋 −15ms에서 MusicPosition − SongTime = −0.015, 진행 시간은 음악 위치 기준). 배속 변경 실행 확인은 안 함 (진행 시간 계산에 배속을 쓰지 않음) |
| IU-14 | 음악 없는 곡 | 채보 마지막 노트 시각을 전체 길이로 표시 | C | 통과 (Test Song 1 00:12, Test Song 2 00:05) |
| IU-15 | 일시정지·카운트다운 | 시간·진행·애니메이션 정지, 재개 후 이어짐 | C/U | C 통과 (정지 20프레임·카운트다운 30프레임 동안 표시 값 그대로, 재개 후 이어짐, AUTO 3 끝 점수 35,800 유지) / U 미검증 |
| IU-16 | Restart / 결과 화면 다시하기 | 00:00, 콤보·판정 표시 초기화 | C | 통과 (메뉴 Restart → 00:00, 점수 0, 콤보·판정 숨김). 결과 화면 다시하기는 실행 안 함 (같은 씬 재로딩) |
| IU-17 | 결과 전환·버튼 | 페이드 정상, AUTO TEST·일시정지 버튼 클릭 정상 | C/U | C 통과 (Test Song 2 끝 → ResultScene 전환, AUTO TEST 버튼 위치 UI 레이캐스트 최상단 = 버튼, 신규 UI Raycast Target 모두 꺼짐) / U 미검증 |
| IU-18 | AUTO 3 회귀 | Test Song 1: 35800 / 61 / Perfect 25 · Tick 36, Test Song 2: 9800 / 14 / Perfect 8 · Tick 6, kick: 263200 / 364 / Perfect 220 · Tick 144 | C | 통과 (3곡 모두 기준값과 같음. kick은 에디터 일시정지 상태에서 음악 시계를 1/30초씩 직접 진행한 결과) |
| IU-19 | 해상도·비율 | 1920×1080, 2560×1440, 3840×2160, 21:9, 4:3에서 겹침·잘림 없음 | C | 미검증 — 도구로 Game 뷰 해상도를 바꿀 수 없어 확인하지 못함 (7절) |
| IU-20 | 자동 싱크 화면 | 노트 프리팹 그대로라 기존과 동일 (회귀 확인) | C | 통과 (정적 확인: Note.prefab, OffsetCalibration.cs, OffsetCalibrationScene 변경 없음) |

검증 시 주의: kick은 실시간(일시정지 해제)으로 돌리고, 재생 중 처음 보내는 MCP 코드는 인트로 구간에서 미리 한 번 보내 컴파일 멈춤을 피한다.

## 7. 검증 결과 및 미해결 사항
### 7.1 검증 결과 (2026-11-16)
- 검증 방식: Unity 창에 포커스가 없으면 실시간 Play에서 프레임이 진행되지 않아(Time.frameCount 고정 확인), 에디터 일시정지 + EditorApplication.Step()으로 1프레임씩 진행하며 HUD 값을 매 프레임 읽었다. kick은 일시정지 중 음악이 멈추므로 매 프레임 AudioSource.time을 1/30초씩 직접 올렸다.
- 컴파일: 오류 0. Play 중 Error/Warning 0.
- AUTO 3 회귀: Test Song 1 35,800 / 61, Test Song 2 9,800 / 14, kick 263,200 / 364 (Perfect 220, Tick 144) — 기준값과 같음.
- 6절 표대로 C 항목은 통과. 실제 키보드 입력, 눈으로 본 가독성, kick 실시간 재생 중 진행 시간, 해상도별 화면은 미검증(사용자 확인 필요).

### 7.2 구현 중 계획과 달라진 점
- GameplayManager에 `IsMusicPlaying` 추가 (음악 종료 판단, 4.6).
- AUTO 일반 노트도 레인 빛이 보이도록 판정 순간 빛 켜기 추가 (4.5).
- 정지 중 레인 빛: 다른 표시처럼 멈추지 않고 꺼지게 함 (계획 IU-11 기대 결과에 맞춤).

### 7.3 미해결
- 해상도별 화면(IU-19): CanvasScaler가 폭 기준(Match Width)이라 가로 배치는 해상도와 무관하고, 세로만 화면 비율에 따라 줄어든다. 계산상 21:9(캔버스 높이 810)에서도 판정 이미지(y −150)가 판정선(약 y −243) 위에 있지만, 실제 화면은 확인하지 못했다.
- 기타:
  - 폰트: 해결 (Kanit-ExtraBoldItalic, OFL 라이선스 파일 함께 보관).
  - 예시 이미지 대비 레인 폭이 좁다(레인 좌표 유지 결정). 더 넓게 보이게 하려면 별도 작업(레인 좌표·카메라 변경)으로 다룬다.

### 변경 이력
| 날짜 | 내용 |
|---|---|
| 2026-11-15 | GPT 초안 작성 |
| 2026-11-16 | 로컬 소스 대조, 사용자 결정 반영(판정 이미지 사용, 콤보 숫자만·반투명, 점수 패널·입력 빛·원근 바닥 포함, 신규 에셋은 사전 확인) |
| 2026-11-16 | 구현 완료, Kanit 폰트 추가(사용자 확인), 검증 결과 기록, 구현 중 변경 3건(7.2) 반영 |
| 2026-11-16 | 사용자 플레이 피드백 반영: 레인 입력 빛 실제 키 확인(IU-11 U 통과), 콤보 크기 70%·알파 0.45, 콤보 이펙트 70%·알파 0.3, 판정 이미지 F·J 레인 폭 안(1.7유닛)·알파 0.7. 컴파일 오류 0, Test Song 1 AUTO 3 35,800 / 61 유지, 스크린샷에서 판정 이미지 화면 x 868~1052 (F·J 레인 852~1068 안) 확인 |
| 2026-11-16 | 사용자 요청: 콤보 숫자·이펙트 위치 y 60 → 120. Test Song 1 AUTO 3 35,800 / 61 유지, 스크린샷 확인 |
