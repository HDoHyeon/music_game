# FULL COMBO / ALL PERFECT 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System), 기준 해상도 1920×1080
- 목적: 플레이 결과로 FULL COMBO / ALL PERFECT 달성 여부를 판별해 **결과 화면**과 **곡 선택 화면 기록 카드**에 표시한다.
- 상태: 계획 확정 (2026-11-17 사용자 승인, 추천안) / 구현·Claude 검증 완료 (2026-11-17) / 사용자 확인 대기

### 확정 사항 (2026-11-17 사용자 승인)
| 항목 | 확정값 |
|---|---|
| 기록 필드 | `PlayRecord.fullCombo` 추가 승인 (기존 기록은 false로 읽힘, 소급 없음) |
| ALL PERFECT 기록 판단 | 별도 필드 없이 `bestRank == "SS"` |
| AUTO 결과 화면 | 달성 상태 표시함 (기록 저장은 기존대로 안 함) |
| 색 | ALL PERFECT #4FD8FF / FULL COMBO #3DDC6A |
| 표시 우선순위 | ALL PERFECT 우선 (한 줄만) |

### 요구사항
- FULL COMBO: MISS 없이 곡을 정상 완료.
- ALL PERFECT: 모든 판정이 PERFECT로 곡을 정상 완료.
- 점수·정확도·랭크·콤보 계산은 바꾸지 않는다.
- 곡·난이도별로 달성 여부를 저장하고, 더 낮은 성적을 내도 이미 달성한 기록은 유지한다. 기록이 없으면 기존 NO RECORD 그대로.
- AUTO TEST 플레이는 기존대로 기록에 저장하지 않는다(달성 여부 포함).
- 기존 기록 데이터는 삭제·초기화하지 않는다.
- 새 이미지·폰트·애니메이션 없음. 기존 Kanit·LegacyRuntime·색만 사용.

## 2. 기존 코드 분석 (2026-11-17, 로컬 소스 확인)
### 2.1 판정 구조 (`GameplayManager`, `Judgement`)
- 판정 등급: Perfect(±50ms) / Great(±100ms) / Good(±150ms) / Miss. `OnJudged(Judgement, lane)`로 알린다.
- **일반 노트**: 판정 1회 (입력 등급, 또는 Good 범위를 지나면 Miss).
- **롱노트**: 판정 2회 + 틱.
  | 상황 | 시작(head) | 끝(tail) | 틱 |
  |---|---|---|---|
  | 끝까지 누름 | 입력 등급 | **항상 Perfect** (종료 150ms 이내에 떼도 Perfect) | 0.15초마다, 시작 등급으로 점수 |
  | 중간에 뗌 (종료 150ms 전보다 일찍) | 입력 등급 | **Miss** (`HandleHolds`) | 뗀 뒤 남은 틱 버림 |
  | 시작을 놓침 | Miss | Miss (`HandleMisses`, 2회) | 없음 |
- **틱**: `OnTick`으로 따로 알리고 판정 개수에 넣지 않는다. 틱 자체는 Miss가 없다(떼면 틱이 멈추고 끝 Miss가 대신 난다).
- 결과: 롱노트 중간 이탈·시작 놓침은 모두 판정 개수의 Miss로 잡힌다.

### 2.2 콤보 (`ScoreComboState`)
- 판정이 Miss면 콤보 0, 그 외(Perfect/Great/Good) +1. 틱마다 +1. `MaxCombo`는 틱 포함 최고값.
- 콤보가 끊기는 경우는 **Miss 판정 하나뿐**이다 → "Miss 0개" = "콤보가 한 번도 끊기지 않음".

### 2.3 결과 데이터 (`PlayResult`, `ResultTransition`)
- `PlayResult`에 판정별 개수(Perfect/Great/Good/Miss, 틱 제외)와 `Total`, `Accuracy`, `Rank`가 있다. 개수는 `ResultTransition`이 `OnJudged`로 센다.
- `Rank`가 "SS"인 조건은 `Total > 0 && Perfect == Total` — **ALL PERFECT 조건과 똑같다**.
- 결과는 정상 완료(`ResultTransition.LoadResult`) 때만 만들어진다. 일시정지 RESTART·곡 선택 이탈은 결과·기록 없음.
- AUTO 사용 여부: `GameplayManager.AutoPlayUsed` → `PlayResult.AutoPlayUsed`. AUTO면 기록 저장 안 함(`record_save_plan.md`).

### 2.4 기록 (`PlayRecord`, `PlayRecordStore`)
- PlayerPrefs `Record.<songId>.<Easy|Normal|Hard|Single>`에 JSON(JsonUtility). 최고값은 항목별로 따로 올라가기만 한다. 자세한 규칙은 `record_save_plan.md`.
- 저장 필드: `playCount`, `bestScore`, `bestCombo`, `bestAccuracy`, `bestRank`, `last*`(마지막 결과·판정 개수).
- `bestRank`는 랭크 순서로만 올라가므로, **`bestRank == "SS"`이면 그 곡·난이도에서 ALL PERFECT를 한 번 이상 달성했다**는 뜻이다.
- FULL COMBO는 기존 필드로 알 수 없다 (`lastMiss`는 마지막 1회만, 최고 콤보는 노트 수와 비교할 기준이 저장되어 있지 않음).
- JsonUtility는 JSON에 없는 필드를 기본값(false/0)으로 읽는다 → 필드를 추가해도 기존 기록이 그대로 읽힌다.

### 2.5 화면 (현재 배치)
- 결과 화면(`result_ui_ux_plan.md`): 왼쪽 RankPanel(80, 160, 720×700) 안에 RANK 라벨·RankText(100, 260, 680×480, Kanit 300). **RankText 아래 y 760~850이 비어 있다.**
- 곡 선택 화면(`song_select_ui_ux_plan.md`): BestCard(950, 670, 840×160) 안에 PERSONAL BEST 라벨(970, 680), BestValuesGroup(BEST SCORE / 랭크 / BEST COMBO), 기록 없으면 RecordText "NO RECORD". **카드 오른쪽 위(x 1560~1770, y 678~708)가 비어 있다.**

### 2.6 결론: 기존 데이터만으로 판별 가능한가
- 이번 플레이: **가능**. `PlayResult`의 판정 개수만으로 정확히 판별된다(틱은 Miss가 없고 판정 개수에도 없으므로 제외해도 정확).
- 저장된 기록: ALL PERFECT는 `bestRank == "SS"`로 알 수 있지만, **FULL COMBO는 새 필드가 필요하다** → 4.3 [중요 — 사용자 승인 필요].

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Gameplay/PlayResult.cs` | 수정 | 계산 속성 `IsFullCombo`, `IsAllPerfect` 추가 (저장 값·생성자 변경 없음) |
| `Assets/Scripts/Song/PlayRecord.cs` | 수정 | 필드 `fullCombo` 추가, `Apply`에서 달성 시 true로만 바꿈, 표시용 `IsAllPerfect`·`IsFullCombo` 속성 |
| `Assets/Scripts/Result/ResultScreen.cs` | 수정 | 달성 표시 Text 1개 (ALL PERFECT 우선) |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 | 기록 카드에 달성 표시 Text 1개 |
| `Assets/Scenes/ResultScene.unity` | 수정 | RankPanel 안 `AchievementText` 추가·연결 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 | BestValuesGroup 안 `BestAchievementText` 추가·연결 |

- 수정하지 않음: `GameplayManager`, `ScoreComboState`, `Judgement`, `ResultTransition`(저장 흐름 그대로 — `Apply` 안에서 처리), `PlayRecordStore`(키·읽기·쓰기 그대로), 점수·랭크·정확도 공식, 채보·음원, 다른 씬.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
### 4.1 판별 (`PlayResult`)
```csharp
// FULL COMBO: 판정이 1개 이상이고 Miss가 없음 (콤보는 Miss로만 끊긴다. 롱노트 중간 이탈·시작 놓침도 Miss로 집계됨)
public bool IsFullCombo => Total > 0 && Miss == 0;
// ALL PERFECT: 판정이 1개 이상이고 모두 Perfect (Rank "SS" 조건과 같음). 틱은 판정 개수가 아니므로 제외
public bool IsAllPerfect => Total > 0 && Perfect == Total;
```
- ALL PERFECT면 FULL COMBO도 항상 참이다.
- 판정 0개: 둘 다 false. (곡 선택 화면은 노트 0개 채보를 CANNOT PLAY로 막으므로 일반 흐름에서는 생기지 않는다.)
- 롱노트 끝(tail)은 끝까지 누르면 항상 Perfect라, ALL PERFECT 여부는 사실상 시작 판정이 정한다. 틱 등급도 시작 등급을 따르므로 따로 볼 필요 없다.

### 4.2 결과 화면 표시
- 한 줄만 표시, **ALL PERFECT 우선** (둘 다면 ALL PERFECT만). 둘 다 아니면 숨김. 결과 없음(NO RESULT)도 숨김.
- 위치: RankPanel 안, 랭크 글자 아래 가운데.

| 오브젝트 | x, y, 폭×높이 | 폰트 / 크기 | 색 |
|---|---|---|---|
| AchievementText (신규) | 100, 760, 680×70 | Kanit 52, 가운데, 넘침 허용 | ALL PERFECT #4FD8FF(PERFECT 판정 색) / FULL COMBO #3DDC6A(GREAT 판정 색), Outline 검정 알파 0.5 |

```csharp
[SerializeField] private Text achievementText; // ALL PERFECT / FULL COMBO (랭크 아래)
[SerializeField] private Color allPerfectColor = new Color(0.31f, 0.85f, 1f);  // #4FD8FF
[SerializeField] private Color fullComboColor = new Color(0.24f, 0.86f, 0.42f); // #3DDC6A

// Start(): 결과 없음 → ShowAchievement(false, false), 결과 있음 → ShowAchievement(result.IsAllPerfect, result.IsFullCombo)
private void ShowAchievement(bool allPerfect, bool fullCombo)
{
    if (achievementText == null) return;
    achievementText.gameObject.SetActive(allPerfect || fullCombo);
    achievementText.text = allPerfect ? "ALL PERFECT" : "FULL COMBO";
    achievementText.color = allPerfect ? allPerfectColor : fullComboColor;
}
```
- AUTO TEST 결과: **결정 필요(5절 결정 3)**. 추천은 표시함 — 이번 플레이를 그대로 보여 주는 정보이고, 기록에는 저장되지 않는다(NEW BEST가 안 뜨는 것과 같은 기존 규칙).

### 4.3 기록 저장 — [중요 — 사용자 승인 필요] `PlayRecord` 필드 추가
- **변경 이유**: FULL COMBO 달성 여부는 기존 기록 필드로 알 수 없다(2.4).
- **변경 대상**: `PlayRecord.cs` (필드 1개 + `Apply` 2줄 + 속성 2개). 저장 키·저장 시점·`PlayRecordStore`는 그대로.
- **기존 기능 영향**:
  - 기존 JSON에는 `fullCombo`가 없어 false로 읽힌다. 다른 값은 그대로이고, 다음 정상 완료 때 필드가 추가되어 저장된다. 기존 기록을 지우거나 다시 쓰지 않는다.
  - 점수·NEW BEST 판정(`Apply` 반환값)은 그대로.
- **ALL PERFECT는 새 필드 없이 `bestRank == "SS"`로 판단**한다(2.4, 4.1 — 조건이 같음). 그래서 기존 SS 기록(예: Test Song 1 35,800 SS)은 바로 ALL PERFECT로 표시된다.
```csharp
public bool fullCombo; // FULL COMBO를 한 번이라도 달성했는지 (한 번 true면 계속 true)

// Apply(result) 안 (최고값 갱신 부분에 추가)
if (result.IsFullCombo) fullCombo = true;

// 표시용: ALL PERFECT는 최고 랭크 SS와 같은 조건, FULL COMBO는 ALL PERFECT도 포함
public bool IsAllPerfect => bestRank == "SS";
public bool IsFullCombo => fullCombo || IsAllPerfect;
```
- **사용자 결정 사항**: (결정 1) 이 필드 추가 승인 / (결정 2) ALL PERFECT를 `bestRank`로 판단할지, 별도 필드 `allPerfect`도 저장할지 — 추천: `bestRank`로 판단(중복 저장 없음, 기존 SS 기록이 바로 반영). 단점: 나중에 SS 랭크 기준을 바꾸면 이 판단도 같이 고쳐야 한다(계획서·코드 주석에 남김).
- 기존 기록의 FULL COMBO 소급: 하지 않는다(추천). 마지막 결과(`lastMiss == 0`)로 일부 추정할 수는 있지만 마지막 1회뿐이라 정확하지 않다. 단, SS 기록은 위 규칙으로 FULL COMBO도 표시된다.
- AUTO: `ResultTransition`이 AUTO면 `Apply`를 부르지 않으므로 달성 여부도 저장되지 않는다(코드 변경 없음).

### 4.4 곡 선택 화면 표시
- 기록 카드 오른쪽 위, PERSONAL BEST 라벨과 같은 줄. BestValuesGroup 안에 두어 기록이 없으면(NO RECORD) 함께 숨는다.

| 오브젝트 | x, y, 폭×높이 | 폰트 / 크기 | 색 |
|---|---|---|---|
| BestAchievementText (신규, BestValuesGroup 자식) | 1560, 678, 210×30 | Kanit 24, 오른쪽 정렬, 넘침 허용 | 결과 화면과 같은 두 색 |

```csharp
[SerializeField] private Text bestAchievementText; // 기록 카드: ALL PERFECT / FULL COMBO (없으면 숨김)

// RefreshInfo()의 hasRecord 분기 안
bool ap = record.IsAllPerfect, fc = record.IsFullCombo;
if (bestAchievementText != null)
{
    bestAchievementText.gameObject.SetActive(ap || fc);
    bestAchievementText.text = ap ? "ALL PERFECT" : "FULL COMBO";
    bestAchievementText.color = ap ? allPerfectColor : fullComboColor;
}
```
- 곡·난이도를 바꿀 때마다 그 키의 기록으로 다시 표시된다(기존 `RefreshInfo` 흐름).

### 4.5 예상 문제 및 해결
| 문제 | 해결 |
|---|---|
| ALL PERFECT 글자가 SS 랭크와 같은 정보를 반복 | 의도된 표시(달성 배지). 색을 랭크 금색과 다르게(PERFECT 하늘색) 해 구분 |
| Kanit 글자가 칸보다 커서 잘림 (곡 선택 화면에서 겪음) | 넘침 허용 + 스크린샷 확인 |
| 곡 선택 카드에서 BestRankValue(1330~1530)와 겹침 | x 1560부터 배치, preferredWidth로 확인 |
| 기존 JSON 읽기 | JsonUtility 기본값 false — 기존 기록으로 테스트(읽기 전후 다른 값 변화 없음 확인) |

## 5. 구현 순서 및 진행 상태
- [x] 계획 확정 (2026-11-17, 추천안): 필드 추가 승인, ALL PERFECT = `bestRank == "SS"`, AUTO 결과 화면 표시, 색 #4FD8FF / #3DDC6A
- [x] 1단계: `PlayResult`·`PlayRecord` 수정 → 컴파일 확인 (에러 0건)
- [x] 2단계: `ResultScreen`·`SongSelectScreen` 표시 추가 → 컴파일 확인 (에러 0건)
- [x] 3단계: 두 씬에 Text 추가·연결 (ResultScene `AchievementText` RankText 바로 뒤 / SongSelectScene `BestValuesGroup/BestAchievementText`, 참조 누락 0)
- [x] 4단계: 6절 테스트
- [x] 5단계: 결과 기록, 테스트 기록·스크린샷 정리
- 완료: 계획, 구현, Claude 검증 / 진행 중: 없음 / 남은 작업: 사용자 확인 (7절)

## 6. 테스트 및 검증 방법
C = Claude가 Play 모드에서 직접 확인, U = 사용자 확인. "결과 로드"는 `PlayResult`를 판정 개수로 직접 만들어 `PlayResult.Last`에 넣고 ResultScene을 불러오는 방식.

| # | 항목 | 방법 | 기대 결과 | 담당 | 결과 |
|---|---|---|---|---|---|
| FC-01 | MISS 없는 완료 | 결과 로드 (P·G·Good 섞임, Miss 0) | FULL COMBO 표시 | C | 통과 (P10 G2 Good1 M0 → FULL COMBO #3DDB6B) |
| FC-02 | 전부 PERFECT | AUTO 3 (Test Song 1·kick) 실제 플레이 | ALL PERFECT 표시 (FULL COMBO 대신) | C | 통과 (Test Song 1·2 AUTO 3 실제 플레이 → ALL PERFECT #4FD9FF, 결과 로드 P20도 같음) |
| FC-03 | GREAT 포함 | 결과 로드 (Great 1, Miss 0) | FULL COMBO, ALL PERFECT 아님 | C | 통과 (Great 1 → FULL COMBO, 랭크 S) |
| FC-04 | GOOD 포함 | 결과 로드 (Good 1, Miss 0) | FULL COMBO | C | 통과 (Good 1 → FULL COMBO) |
| FC-05 | MISS 포함 | AUTO 2 실제 플레이(Miss 있는 결과) + 결과 로드 (Miss 1) | 표시 없음 | C | 통과 (결과 로드 Miss 1 → 숨김 / Test Song 2 AUTO 2 실제 플레이 1·4·1·2 → 숨김) |
| FC-06 | 롱노트 중간 이탈 | AUTO 1 실제 플레이(롱노트 머리 Perfect 후 바로 뗌 → 끝 Miss) | Miss 집계, 표시 없음 | C (실제 키 U) | C 통과 (AUTO 1: 롱노트 머리 Perfect 후 바로 뗌 → 끝 Miss 집계, 5·0·0·20, 숨김) / 실제 키 U 미검증 |
| FC-07 | 롱노트 정상 완료 | AUTO 3 (롱노트 포함 곡) | 끝 Perfect, ALL PERFECT | C | 통과 (Test Song 1 AUTO 3: 롱노트 포함 25판정 모두 Perfect → ALL PERFECT) |
| FC-08 | 판정 0개 | 결과 로드 (0,0,0,0) | 표시 없음, 오류 없음 | C | 통과 (0판정 → 숨김, 오류 없음. PlayResult·PlayRecord 모두 false) |
| FC-09 | RETRY 후 | AUTO 3 완료 → RETRY → AUTO 1 완료 | 두 번째 결과는 표시 없음 (이전 결과가 남지 않음) | C | 통과 (AUTO 3 ALL PERFECT → RETRY → AUTO 1 결과는 숨김, 이전 결과 안 남음) |
| FC-10 | 곡·난이도별 독립 | 기록 직접 저장(임시) 후 곡 선택 화면에서 난이도·곡 전환 | 각 키의 달성 여부만 표시 | C | 통과 (임시 기록: kick Easy fullCombo → FULL COMBO / Normal SS → ALL PERFECT / Hard B → 숨김 / Test Song 3 기록 없음 → NO RECORD, 그룹째 숨김) |
| FC-11 | 재실행 후 유지 | Play 종료 → 다시 Play → 곡 선택 화면 | 저장된 달성 표시 유지 | C (게임 빌드 재실행은 U) | C 통과 (Play 종료 후 다시 Play: kick Easy FULL COMBO, Normal ALL PERFECT 유지) / 빌드 재실행 U 미검증 |
| FC-12 | AUTO 저장 제외 | AUTO 3 완료 전후 기록 키 비교 | 기록·`fullCombo` 변화 없음 | C | 통과 (AUTO 3·1·2·3 실행 전후 기록 키 6개 값 같음) |
| FC-13 | 결과 화면 UI | 스크린샷 (ALL PERFECT / FULL COMBO / 없음 / NO RESULT) | 랭크 아래 표시, 겹침·잘림 없음 | C, 느낌 U | C 통과 (스크린샷: ALL PERFECT·FULL COMBO·숨김, 랭크 아래 패널 안, 겹침 없음. NO RESULT 숨김) / 느낌 U 미검증 |
| FC-14 | 곡 선택 UI | 스크린샷 (ALL PERFECT / FULL COMBO / 표시 없음 / NO RECORD) | 카드 오른쪽 위, 겹침 없음 | C, 느낌 U | C 통과 (스크린샷: 카드 오른쪽 위, preferredWidth 145~146 < 칸 210, BEST COMBO 라벨·랭크와 겹침 없음) / 느낌 U 미검증 |
| FC-15 | 회귀 | AUTO 3 기준값(Test Song 1 35,800/61, Test Song 2 9,800/14, kick Normal 263,200/364), `Apply` 반환값(NEW BEST), 기존 기록 읽기, 기존 SS 기록이 ALL PERFECT로 보임 | 기존 값 그대로 | C | 통과 (Test Song 1 35,800 / Test Song 2 9,800 그대로, `Apply` NEW BEST 반환값 그대로, 예전 형식 JSON 읽기 정상(fullCombo=false), 사용자 기록 Test Song 1 SS → ALL PERFECT 표시) |

- 추가 확인: 일반 플레이로 FULL COMBO를 달성했을 때 기록에 `fullCombo:true`가 저장되고, 이후 Miss가 있는 플레이를 해도 유지되는지 — 기록 저장은 AUTO가 아닌 플레이만 되므로 `PlayRecord.Apply`를 직접 호출하는 방식(C)과 실제 키보드 플레이(U)로 확인.
- 테스트로 만든 임시 기록은 끝나면 원래 값으로 되돌린다(사용자 기록 보존).

## 7. 검증 결과 및 미해결 사항
### 7.1 검증 결과 (2026-11-17, Claude가 Play 모드에서 직접 실행)
- 컴파일 에러 0건, Play 모드 에러·경고 0건, 두 화면 참조 누락 0개.
- 기록 로직 (`PlayRecord.Apply` 직접 호출):
  - 새 기록 → Great 1 FULL COMBO 적용: `fullCombo=true`, 랭크 S
  - 이어서 Miss 1(점수는 더 높음) 적용: `fullCombo` true 유지, 최고 점수만 갱신, NEW BEST 반환 true (기존 규칙 그대로)
  - 이어서 ALL PERFECT 적용: 랭크 SS → `IsAllPerfect` true
  - 저장 JSON에 `"fullCombo":true` 포함. 예전 형식 JSON(필드 없음) 읽기: 랭크 A면 `fullCombo=false`, 랭크 SS면 표시상 ALL PERFECT·FULL COMBO
- 6절 FC-01~15: C 항목 모두 통과 (표 참고).
- 테스트 중 kick Easy/Normal/Hard 기록 키에 임시 기록을 넣었다가, 끝난 뒤 원래 값으로 되돌림(복원 확인 true). 테스트 스크린샷 삭제.
- 계획과 다른 점: FC-15 회귀에서 kick Normal AUTO 3은 다시 돌리지 않음 (판정·점수 코드 변경 없음, Test Song 1·2 기준값 확인).

### 7.2 알아 둘 점
- 기존 기록에는 FULL COMBO를 소급하지 않는다(확정 사항). 예: 사용자 기록 kick Normal(랭크 S, 마지막 플레이 Miss 0)은 다음에 FULL COMBO로 완료할 때까지 표시가 없다. SS 기록(Test Song 1)은 바로 ALL PERFECT로 표시된다.

### 7.3 사용자 확인 필요 (U)
- 실제 키보드 플레이로 FULL COMBO 달성 → 결과 화면 표시와 곡 선택 화면 기록 저장·유지
- 롱노트를 실제로 중간에 뗐을 때 표시 없음
- 게임(빌드) 재실행 후 기록 유지
- 표시 위치·색(하늘색 ALL PERFECT / 초록 FULL COMBO) 느낌
- 변경 이력: 2026-11-17 초안 작성 (로컬 소스 분석 기반) → 2026-11-17 추천안으로 확정 → 2026-11-17 구현·검증
