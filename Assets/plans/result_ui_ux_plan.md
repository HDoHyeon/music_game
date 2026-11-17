# 결과 화면 UI/UX 개선 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System), 기준 해상도 1920×1080
- 목적: 기존 결과 화면(`result_screen_plan.md`, 확인 완료)의 기능은 그대로 두고, 화면 배치·색·폰트를 다듬어 보기 좋고 정보가 잘 읽히게 한다. DJMAX·EZ2ON 결과 화면을 참고하되 그대로 따라 하지 않는다.
- 상태: 계획 확정 (2026-11-17 사용자 승인, 추천안) / 구현·Claude 검증 완료 (2026-11-17) / 사용자 확인 대기

### 확정 사항 (2026-11-17 사용자 승인)
| 항목 | 확정값 |
|---|---|
| 레이아웃 | 4.1 — 왼쪽 랭크 패널 / 오른쪽 정보 패널(점수 → 콤보·정확도 → 판정 개수) |
| 버튼 순서 | SONG SELECT 왼쪽(보조: 어두운 채움 + 주황 테두리), RETRY 오른쪽(기본: 주황 채움) |
| 4.7 선택 항목 | A 곡 정보, B AUTO 표시, C NEW BEST 배지·애니메이션 — 모두 넣지 않음 |

### 요구사항
- 유지할 정보·기능 (13개): 랭크(SS~D), SCORE, MAX COMBO, ACCURACY(소수 첫째 자리), PERFECT·GREAT·GOOD·MISS 개수, 저장된 BEST, NEW BEST 표시/숨김, RETRY, SONG SELECT, 결과가 없을 때 NO RESULT
- 바꾸지 않는 것: 판정 집계, 정확도·랭크·점수 계산(`PlayResult`), 기록 저장 시점·규칙(`ResultTransition`, `PlayRecordStore`), RETRY·SONG SELECT 동작과 씬 이동, AUTO TEST 결과를 기록하지 않는 규칙
- 롱노트 틱은 판정 개수에 넣지 않는다 (기존과 같음).
- 새 폰트·스프라이트·배경 이미지·애니메이션·시각 효과는 넣지 않는다. 기존 리소스(Kanit 폰트, 내장 UISprite, 곡 선택 화면 색)만 쓴다.

## 2. 기존 코드 분석
### 2.1 스크립트
- `ResultScreen.cs`: `Start`에서 `PlayResult.Last`를 읽어 Text 7개를 채운다.
  - 결과 없음: 랭크 "-", 점수 칸 "NO RESULT", 나머지 빈 문자열, NEW BEST 숨김
  - 결과 있음: `"SCORE  " + Score`(쉼표 없음), `"MAX COMBO  " + MaxCombo`, `"ACCURACY  " + Accuracy.ToString("F1") + "%"`, 판정 4줄을 한 Text에 (`"PERFECT  n\nGREAT  n\n..."`), `newBestText` = `IsNewBest`, `"BEST  " + BestScore(N0)` 또는 `"BEST  -"`
  - `Retry()` → `IngameScene`, `GoToSongSelect()` → `SongSelectScene`
- `PlayResult.cs`: 값은 생성 후 바뀌지 않음. `Accuracy`, `Rank`는 계산 속성. 표시 쪽은 읽기만 한다.
- `ResultTransition.cs`: 결과 생성·기록 저장. AUTO 사용 시 저장하지 않고 `IsNewBest=false`, `HasBest`·`BestScore`는 기존 저장값.
- 텍스트를 직접 읽어 판단하는 다른 스크립트는 없다 (`Scripts` 폴더 확인). 표시 문자열 형식을 바꿔도 다른 기능에 영향이 없다.

### 2.2 현재 씬 구조 (`ResultScene`, 2026-11-17 Play 모드에서 덤프)
- Canvas: Screen Space Overlay, CanvasScaler Scale With Screen Size 1920×1080, match 0. 카메라 배경 RGBA(0.08, 0.08, 0.12) ≈ #141420
- 모든 오브젝트가 화면 가운데 기준(anchor 0.5), 폰트 LegacyRuntime, 흰색, 가운데 정렬

| 오브젝트 | 위치(가운데 기준) | 크기 | 글자 | 비고 |
|---|---|---|---|---|
| Title | (0, 450) | 800×100 | 72 | "RESULT" |
| RankText | (0, 250) | 600×240 | 200 | 랭크, 흰색 |
| ScoreText | (0, 70) | 1000×80 | 56 | "SCORE  1174000" |
| MaxComboText | (0, 0) | 1000×70 | 48 | |
| AccuracyText | (0, −70) | 1000×70 | 48 | |
| JudgementText | (0, −250) | 800×240 | 40 | 4줄 |
| NewBestText | (620, 70) | 400×80 | 48 Bold | 주황 #F7941F |
| BestText | (620, 0) | 400×70 | 40 | 연회색 |
| RetryButton (+Text) | (−220, −440) | 320×90 | 48 | 주황 채움, 검정 글자 |
| SongSelectButton (+Text) | (220, −440) | 360×90 | 44 | 주황 채움, 검정 글자 |
| EventSystem, ResultScreen, Main Camera | | | | |

### 2.3 현재 화면의 문제점 (개선 전 캡처: SS·NEW BEST / D / AUTO / NO RESULT)
1. 모든 정보가 가운데 한 줄로 세로로 쌓여 있고 좌우가 비어 있다. NEW BEST·BEST만 오른쪽에 떠 있어 균형이 맞지 않는다.
2. 랭크가 크지만 흰색 기본 폰트라 등급별 차이가 느껴지지 않는다 (SS와 D가 같은 모양).
3. 점수에 쉼표가 없어(1174000) 자릿수가 잘 읽히지 않고, 인게임 점수(Kanit, 쉼표)와 모양이 다르다.
4. 라벨과 값이 같은 크기·색·폰트로 이어 붙어 있어("SCORE  1174000") 구분이 약하다.
5. 판정 4줄이 가운데 정렬이라 숫자 자리가 맞지 않고, 판정별 색이 없다.
6. RETRY와 SONG SELECT가 똑같은 주황 버튼이라 어느 쪽이 기본 동작인지 드러나지 않는다.
7. 영역 구분(패널·카드)이 없어 주요 결과와 세부 결과의 경계가 없다.
8. 이번 점수와 BEST가 같은 높이에 비슷한 크기로 있어 구분이 약하다.

### 2.4 참고할 기존 디자인
- 곡 선택 화면(`song_select_ui_ux_plan.md`): 왼쪽 위 굵은 제목 + 주황 강조선, 짙은 패널(#0F1322, Outline #2A3350 / #151A2E), 카드 라벨(회색 작은 글자) + Kanit 값, 기본 버튼 주황 채움 / 보조 버튼 어두운 채움 + 주황 테두리
- 랭크 색 (`SongSelectScreen.RankColor`): SS #FFD64A(금), S #F7941F(주황), A #5CBFFF(하늘), B #80E080(초록), C #BFC4D1(밝은 회색), D #8A8FA1(회색)
- 인게임 점수: Kanit 기울임, `ToString("N0", InvariantCulture)` (1,174,000)
- 판정 이미지 색(`Sprites/UI/Judgement`): PERFECT 하늘~보라, GREAT 초록, GOOD 노랑, MISS 빨강

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Result/ResultScreen.cs` | 수정 (표시만) | 값만 표시(라벨은 씬에 고정), 점수·콤보 쉼표, 랭크 색, 판정별 개수 Text 4개 연결 |
| `Assets/Scenes/ResultScene.unity` | 수정 | 패널·카드·라벨 추가, 기존 오브젝트 위치·크기·폰트·색 변경 |

- 수정하지 않음: `PlayResult.cs`, `ResultTransition.cs`, `PlayRecord*.cs`, `GameplayManager.cs`, 다른 씬, 음원·채보, 프로젝트 설정, 패키지
- 새 에셋 없음 (Kanit-ExtraBoldItalic, LegacyRuntime, 내장 UISprite만 사용)
- 기존 오브젝트 삭제·이름 변경 없음. `JudgementText`는 지우지 않고 비활성화(4.4 참고)

## 4. 상세 구현 방법 및 필요한 코드 스니펫
좌표는 **화면 왼쪽 위 기준 px (x, y, 폭×높이)** 로 적는다. 구현할 때는 새 오브젝트를 왼쪽 위 anchor로 만들고, 기존 오브젝트는 anchor를 왼쪽 위로 바꿔 같은 값을 넣는다(이름·컴포넌트는 그대로).

### 4.1 레이아웃 (현재 → 개선)
```
현재                                    개선
┌──────────────────────────┐          ┌─────────────────────────────────────────────┐
│         RESULT           │          │ RESULT                         (곡 정보*)   │
│           SS             │          │ ▬▬                                          │
│   SCORE 1174000  NEW BEST│          │ ┌───────────────┐ ┌───────────────────────┐ │
│   MAX COMBO 1510  BEST ..│          │ │ RANK          │ │ SCORE        NEW BEST │ │
│   ACCURACY 100.0%        │          │ │               │ │ 1,174,000             │ │
│      PERFECT 1030        │          │ │      SS       │ │        BEST 1,174,000 │ │
│      GREAT 0             │          │ │   (랭크 색)    │ ├──────────┬────────────┤ │
│      GOOD 0              │          │ │               │ │MAX COMBO │ ACCURACY   │ │
│      MISS 0              │          │ │               │ │ 1,510    │ 100.0%     │ │
│  [RETRY] [SONG SELECT]   │          │ │               │ │▌PERFECT        1,030  │ │
└──────────────────────────┘          │ │               │ │▌GREAT              0  │ │
                                      │ └───────────────┘ │▌GOOD               0  │ │
                                      │                   │▌MISS               0  │ │
                                      │                   └───────────────────────┘ │
                                      │                 [SONG SELECT]  [ RETRY ]    │
                                      └─────────────────────────────────────────────┘
```
`*` 곡 정보 표시는 넣지 않기로 결정 (4.7)

- 왼쪽: 랭크 전용 패널 (가장 큰 요소). 오른쪽: 위에서부터 점수 → 콤보·정확도 → 판정 개수. 아래 오른쪽: 버튼.
- 곡 선택 화면과 같은 틀(왼쪽 위 제목, 왼쪽·오른쪽 패널, 오른쪽 아래 주 버튼)이라 화면을 넘나들 때 일관된다.

### 4.2 UI 요소별 위치·크기·폰트·색
| 오브젝트 | 구분 | x, y, 폭×높이 | 폰트 / 크기 | 색 / 기타 |
|---|---|---|---|---|
| Title | 기존 | 80, 50, 600×80 | LegacyRuntime 56 Bold, 왼쪽 정렬 | 흰색 |
| TitleAccent | 신규 Image | 80, 132, 120×6 | | 주황 #F7931E |
| RankPanel | 신규 Image | 80, 160, 720×700 | | #0F1322, Outline #2A3350 (2, −2) |
| RankLabel | 신규 Text | 120, 190, 300×40 | LegacyRuntime 24 Bold | 회색 #8A8FA1, "RANK" |
| RankText | 기존 | 100, 260, 680×480 | **Kanit 300**, 가운데 | 랭크 색(2.4), Outline 검정 알파 0.5 (4, −4), 넘침 허용 |
| InfoPanel | 신규 Image | 860, 160, 980×700 | | #151A2E, Outline #2A3350 |
| ScoreLabel | 신규 Text | 900, 190, 300×40 | LegacyRuntime 24 Bold | 회색, "SCORE" |
| NewBestText | 기존 | 1560, 186, 240×48 | Kanit 36, 오른쪽 정렬 | 주황 #F7931E, Outline 검정 알파 0.5 |
| ScoreText | 기존 | 900, 230, 900×120 | **Kanit 104**, 왼쪽 정렬 | 흰색, "1,174,000" |
| BestText | 기존 | 1400, 350, 400×40 | LegacyRuntime 28, 오른쪽 정렬 | 연회색 #CCD1E6, "BEST  1,174,000" |
| ScoreDivider | 신규 Image | 900, 410, 900×2 | | #2A3350 |
| ComboCard | 신규 Image | 900, 430, 430×130 | | #1A2033 |
| ComboLabel | 신규 Text | 925, 445, 380×34 | LegacyRuntime 22 Bold | 회색, "MAX COMBO" |
| MaxComboText | 기존 | 925, 480, 380×70 | Kanit 56, 왼쪽 정렬 | 흰색, "1,510" |
| AccuracyCard | 신규 Image | 1370, 430, 430×130 | | #1A2033 |
| AccuracyLabel | 신규 Text | 1395, 445, 380×34 | LegacyRuntime 22 Bold | 회색, "ACCURACY" |
| AccuracyText | 기존 | 1395, 480, 380×70 | Kanit 56, 왼쪽 정렬 | 흰색, "100.0%" |
| JudgementGroup | 신규 빈 오브젝트 | 900, 590, 900×240 | | 판정 4줄 묶음 |
| └ PerfectRow 등 ×4 | 신규 | 줄 높이 52, 간격 60 (y 590/650/710/770) | | |
| ─ 색 막대 | Image | 0, 6, 8×40 | | 판정 색 |
| ─ 라벨 | Text | 28, 0, 400×52 | LegacyRuntime 32 Bold, 왼쪽 | 판정 색 |
| ─ 개수 (`PerfectCountText` 등) | Text | 500, 0, 400×52 | Kanit 40, 오른쪽 | 흰색 |
| JudgementText | 기존 | 비활성 | | 4.4 참고 |
| SongSelectButton | 기존 | 1110, 900, 340×100 | 글자 36 Bold | 보조: 채움 #1A2033 + Outline 주황(2, −2), 글자 주황 |
| RetryButton | 기존 | 1500, 900, 340×100 | 글자 40 Bold | 기본: 채움 주황 #F7931E, 글자 짙은 남색 #141420 |

- 판정 색: PERFECT #4FD8FF(하늘), GREAT #3DDC6A(초록), GOOD #F2C230(노랑), MISS #FF4D6A(빨강) — 판정 이미지 색에 맞춤
- 패널·카드·막대·라벨은 모두 Raycast Target 끔 (버튼 클릭을 막지 않게)
- 그리기 순서: 패널 → 카드 → 텍스트 → 버튼 (버튼이 맨 위)
- 버튼 위치: 오른쪽 끝 RETRY가 기본 동작(곡 선택 화면의 START와 같은 자리·색). 기존 순서(RETRY 왼쪽, SONG SELECT 오른쪽)를 바꾼다 (2026-11-17 사용자 승인).

### 4.3 랭크 강조
- Kanit 300(굵은 기울임) + 랭크 색 + 어두운 Outline. "SS" 두 글자도 680 폭 안에 들어감 (구현 시 `preferredWidth`로 확인).
- 랭크 색은 곡 선택 화면과 같은 값. `SongSelectScreen.RankColor`는 private이고 그 파일은 이번 범위가 아니므로, `ResultScreen`에 같은 값의 `RankColor`를 둔다(값 6개 중복). 공용 클래스로 빼는 것은 곡 선택 스크립트까지 고쳐야 해서 하지 않는다.
- 결과 없음: "-"를 회색(D 색)으로.

### 4.4 판정 개수와 JudgementText
- 개수 Text 4개(`perfectCountText`, `greatCountText`, `goodCountText`, `missCountText`)를 새 필드로 연결해 숫자만 넣는다. 라벨·색 막대는 씬에 고정.
- 기존 `JudgementText`는 **지우지 않고 비활성화**한다. 새 Text가 연결되지 않은 경우에만 예전처럼 4줄 문자열을 넣는다(곡 선택 화면의 `recordText`와 같은 방식).

### 4.5 스크립트 수정 (`ResultScreen.cs`, 표시 부분만)
```csharp
[SerializeField] private Text perfectCountText; // 판정별 개수 (숫자만, 라벨은 씬에 고정)
[SerializeField] private Text greatCountText;
[SerializeField] private Text goodCountText;
[SerializeField] private Text missCountText;

private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

// 결과 있음 (계산은 PlayResult 그대로, 문자열 형식만 바꿈)
SetText(rankText, result.Rank);
if (rankText != null) rankText.color = RankColor(result.Rank);
SetText(scoreText, result.Score.ToString("N0", Inv));          // 1,174,000 (라벨 SCORE는 씬)
SetText(maxComboText, result.MaxCombo.ToString("N0", Inv));    // 1,510
SetText(accuracyText, result.Accuracy.ToString("F1", Inv) + "%"); // 100.0% (소수 첫째 자리 유지)
if (perfectCountText != null)
{
    SetText(perfectCountText, result.Perfect.ToString("N0", Inv));
    SetText(greatCountText, result.Great.ToString("N0", Inv));
    SetText(goodCountText, result.Good.ToString("N0", Inv));
    SetText(missCountText, result.Miss.ToString("N0", Inv));
}
else
{
    SetText(judgementText, "PERFECT  " + result.Perfect + "\nGREAT  " + result.Great + "\nGOOD  " + result.Good + "\nMISS  " + result.Miss);
}
// NEW BEST·BEST: 기존 줄 그대로
```
- 결과 없음: 기존과 같이 랭크 "-"(회색), 점수 칸 "NO RESULT", 나머지 값과 개수 4개는 빈 문자열, NEW BEST 숨김. 라벨(SCORE, MAX COMBO 등)은 남는다.
- AUTO TEST: 표시 방식은 일반 플레이와 같다. 저장하지 않으므로 NEW BEST는 나오지 않고 BEST는 기존 저장값(기존 규칙 그대로).
- `Retry`, `GoToSongSelect`, 버튼 연결, 씬 이름은 그대로.
- 정확도 `ToString("F1")`에 `InvariantCulture`를 붙인다. 표시 결과는 지금 환경과 같고, 소수점이 쉼표로 바뀌는 문화권을 막는 것뿐이다.

### 4.6 예상 문제 및 해결
| 문제 | 해결 |
|---|---|
| Kanit은 줄 높이가 커서 칸 높이보다 크면 글자가 잘림 (곡 선택 화면에서 겪음) | Kanit 값 Text는 세로·가로 넘침 허용(Overflow), 스크린샷으로 확인 |
| 기울임 글꼴 끝이 칸 밖으로 나가 잘림 | 가로 넘침 허용, 오른쪽 정렬 값은 오른쪽 여백 20 이상 |
| 큰 값(점수 9,999,999, 콤보 9,999)이 칸을 넘음 | 최대 자릿수로 `preferredWidth` 측정해 칸 폭 안인지 확인 |
| 패널이 버튼 클릭을 가로챔 | 신규 Image·Text 전부 Raycast Target 끔, 버튼 위치 레이캐스트 검사 |
| NEW BEST와 SCORE 라벨·점수 겹침 | NEW BEST는 카드 오른쪽 위, 점수는 왼쪽 정렬 — 최대 점수로 겹침 확인 |
| 기존 오브젝트 anchor 변경으로 위치가 틀어짐 | 변경 후 덤프로 좌표 확인, 스크린샷 비교 |
| `NO RESULT`가 Kanit 104에서 칸을 넘음 | 폭 측정, 넘으면 결과 없음일 때만 글자 크기 줄임 |

### 4.7 선택 항목 — 2026-11-17 결정: 모두 넣지 않음 (기록용)
| 항목 | 내용 | 이유 | 영향 범위 |
|---|---|---|---|
| A. 곡 정보 표시 | 오른쪽 위에 "곡명 · 아티스트 · 난이도" (예: 희망 · GrandChace · HARD) | DJMAX·EZ2ON 결과 화면에는 어떤 곡의 결과인지 나온다. 지금은 화면에 곡 정보가 없다 | `ResultScreen`이 `SongSelection.Selected`·`SelectedDifficulty`를 읽기만 함. Text 1개 추가. 곡 선택 없이 실행하면 숨김. 요청서의 "새 결과 통계 항목 추가 금지"에 걸리는지 판단 필요(통계가 아닌 곡 정보) |
| B. AUTO 표시 | AUTO TEST를 쓴 결과면 "AUTO PLAY · NOT SAVED" 작은 문구 | 기록이 저장되지 않은 이유가 화면에 드러남 | `PlayResult.AutoPlayUsed`(이미 있음)를 읽기만 함. Text 1개 추가 |
| C. NEW BEST 배지·애니메이션 | 주황 배지 바탕, 깜빡임 등 | 강조 | 배지는 Image 추가와 계층 변경, 애니메이션은 요청서상 승인 전 제외 |

## 5. 구현 순서 및 진행 상태
- [x] 계획 확정 (2026-11-17): 레이아웃 4.1, 버튼 SONG SELECT 왼쪽·RETRY 오른쪽, 4.7 A·B·C 제외
- [x] 1단계: `ResultScreen.cs` 표시 수정 → 컴파일 확인 (에러 0건)
- [x] 2단계: `ResultScene` 패널·카드·라벨 추가, 기존 오브젝트 위치·폰트·색 변경, 새 필드 연결, `JudgementText` 비활성화
  - 신규: RankPanel, InfoPanel, ScoreDivider, ComboCard, AccuracyCard, TitleAccent, RankLabel, ScoreLabel, ComboLabel, AccuracyLabel, JudgementGroup(PerfectRow·GreatRow·GoodRow·MissRow, 각 Bar·Label·CountText)
  - 기존 오브젝트: 삭제·이름 변경 없음. anchor를 왼쪽 위로 바꾸고 4.2 표대로 위치·크기·폰트·색 지정. 버튼은 SetAsLastSibling으로 맨 위
- [x] 3단계: 6절 테스트 (레이캐스트·스크린샷·AUTO)
- [x] 4단계: 결과 기록, 스크린샷 파일 정리

- 완료: 계획, 구현, Claude 검증
- 진행 중: 없음
- 남은 작업: 사용자 확인 (7.2)

## 6. 테스트 및 검증 방법
C = Claude가 Play 모드에서 직접 확인, U = 사용자 확인

| # | 항목 | 방법 | 기대 결과 | 담당 | 결과 (C / U) |
|---|---|---|---|---|---|
| 1 | 결과 화면 정상 진입 | AUTO 3으로 Test Song 2를 끝까지 플레이 | 페이드 후 ResultScene, 에러·경고 0 | C | C 통과 |
| 2 | 랭크 SS~D 표시 | `PlayResult`를 6가지 판정 조합으로 만들어 `Last`에 넣고 결과 씬 로드 | 각 랭크 글자와 색이 2.4 표와 같음, 잘림 없음 | C (색 느낌 U) | C 통과 / U 미검증 |
| 3 | 점수 | AUTO 3 희망 Hard 기준값 등 | 1,174,000처럼 쉼표, 값은 기존과 같음 | C | C 통과 |
| 4 | 최대 콤보 | 같은 실행 | 1,510 (기존 값과 같음) | C | C 통과 |
| 5 | 정확도 | AUTO 2 (기존 54.4%) | "54.4%" 소수 첫째 자리 | C | C 통과 |
| 6 | 판정 4개 | AUTO 1·2·3 | 개수가 AUTO 요약 로그와 같음, 숫자 오른쪽 정렬 | C | C 통과 |
| 7 | BEST | 기록 있음/없음 | "BEST  35,800" / "BEST  -" | C | C 통과 |
| 8 | NEW BEST 표시·숨김 | `IsNewBest` true/false 결과로 각각 로드 | true면 보임, false면 숨김 | C | C 통과 |
| 9 | AUTO TEST 결과 | AUTO 3 kick Normal | 263,200 등 정상 표시, NEW BEST 없음, 기록 키 값 변화 없음 | C | C 통과 |
| 10 | RETRY | 버튼 위치 레이캐스트 + `onClick` | RetryButton이 맞고 IngameScene 로드 | C, 실제 마우스 클릭 U | C 통과 / U 미검증 |
| 11 | SONG SELECT | 같은 방법 | SongSelectScene, 곡·난이도 유지 | C, 실제 마우스 클릭 U | C 통과 / U 미검증 |
| 12 | NO RESULT | `Last = null`로 결과 씬 로드 | 랭크 "-", "NO RESULT", 값 빈칸, NEW BEST 숨김 | C | C 통과 |
| 13 | 겹침·잘림 | 덤프로 사각형 겹침 검사, 최대값(9,999,999 / 9,999 / 100.0% / 판정 9,999)으로 `preferredWidth` 측정, 1920×1080 스크린샷 | 겹침·잘림 없음 | C, 전체 느낌 U | C 통과 / U 미검증 |
| 14 | 회귀 | AUTO 3 기준값 (Test Song 1 35,800/61, Test Song 2 9,800/14, kick Normal 263,200/364), 기록 저장 동작 | 기존 값과 같음, 저장 규칙 변화 없음 | C | C 통과 |

- 컴파일 에러 0, Play 모드 에러·경고 0, Missing 참조 0 확인
- 실제 키보드·마우스 입력과 화면 느낌은 AUTO로 판단하지 않고 U로 남긴다.
- 테스트로 만든 임시 기록은 끝나면 원래 값으로 되돌린다.

## 7. 검증 결과 및 미해결 사항
### 7.1 검증 결과 (2026-11-17, Claude가 Play 모드에서 직접 실행)
- 컴파일 에러 0건. Play 모드 에러·경고 0건. `ResultScreen` 참조 누락 0개 (씬 저장 시 SerializedObject로 확인).
- RU-02 랭크: 판정 조합 6가지로 SS #FFD64A / S #F7941F / A #5CBFFF / B #80E080 / C #BFC4D1 / D #8A8FA1 확인. "SS"의 preferredWidth 334, 칸 680×480 안.
- RU-03·04·07·08 (`PlayResult` 직접 생성):
  - NEW BEST 결과: `1,174,000` / `1,510` / `100.0%` / `BEST  1,174,000` / 판정 1,030·0·0·0 / NEW BEST 보임
  - 기록 있고 갱신 아님: `19,180` / `22` / `54.4%` / `BEST  35,800` / 8·5·7·5 / NEW BEST 숨김
  - 기록 없음: `BEST  -`
- RU-01·05·06·09·14 (곡 선택 → AUTO → 결과 화면, 실제 흐름):

  | 플레이 | 결과 화면 값 |
  |---|---|
  | Test Song 1 AUTO 3 | SS / 35,800 / 61 / 100.0% / 25·0·0·0 / BEST 35,800 (기존 사용자 기록) |
  | Test Song 1 AUTO 1 (RETRY 후) | D / 5,000 / 2 / 20.0% / 5·0·0·20 (`result_screen_plan.md` 기존 값과 같음) |
  | Test Song 2 AUTO 2 | D / 6,560 / 11 / 66.3% / 4·1·2·1 (정확도 (400+70+60)/8 = 66.25 → F1 반올림) |
  | Test Song 2 AUTO 3 | SS / 9,800 / 14 / 100.0% / 8·0·0·0 / BEST - |
  | kick Normal AUTO 3 | SS / 263,200 / 364 / 100.0% / 220·0·0·0 / BEST - |

  - AUTO 결과에서 NEW BEST는 모두 숨김. 테스트 전후 `Record.test_song_1.Single`, `Record.test_song_2.Single`, `Record.kick.Normal` 값이 같음 (AUTO 결과 저장 안 함 유지).
- RU-10·11: RETRY·SONG SELECT 버튼의 가운데와 두 모서리 근처 3지점 레이캐스트가 모두 해당 버튼에 닿음. RETRY → IngameScene, SONG SELECT → SongSelectScene (곡 선택 유지: Test Song 1, 난이도 값 유지).
- RU-12: 랭크 "-"(회색), `NO RESULT`, 나머지 값 빈칸, NEW BEST 숨김.
- RU-13: 최대값(9,999,999 / 9,999 / 판정 9,999)에서 점수 preferredWidth 486 (칸 900), 콤보·정확도 146·162 (칸 380), 판정 개수 106 (칸 400), NEW BEST 168 (칸 240). `NO RESULT` 535 (칸 900). 스크린샷 4장(SS·NEW BEST / D / 최대값 / NO RESULT)에서 겹침·잘림 없음.
- 계획과 다른 점:
  - RU-03·04는 희망 Hard AUTO를 다시 돌리지 않고, 같은 값(1,174,000 / 1,510)으로 `PlayResult`를 직접 만들어 확인함.
  - RU-13은 사각형 겹침 자동 검사 대신 preferredWidth 측정 + 스크린샷 확인으로 함.
  - kick Normal은 에디터가 포커스를 잃으면 오디오가 진행되지 않아, 기존 회귀 스크립트(오디오 시간을 1/30초씩 직접 진행)로 실행함.
- 결과 없음일 때 랭크 "-"는 Kanit 글꼴에서 기울어진 짧은 막대 모양으로 보인다 (의도한 표시, 회색).
- 테스트 스크린샷(`Assets/Screenshots`)은 확인 후 삭제함.

### 7.2 미해결 / 사용자 확인 필요 (U)
- 실제 마우스로 RETRY·SONG SELECT 클릭
- 랭크 색, 전체 배치와 색의 느낌 (1920×1080 실제 화면)

- 변경 이력: 2026-11-17 초안 작성 (현재 화면 덤프·캡처 기반) → 2026-11-17 추천안으로 확정 → 2026-11-17 구현·검증 완료
