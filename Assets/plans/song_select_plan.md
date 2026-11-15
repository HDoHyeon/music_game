# 곡 선택 화면 구현 계획서

## 1. 작업 목적 및 요구사항
- 프로젝트: music_game (Unity 6.6, C#, URP, Mono, Input System)
- 목적: 등록된 곡을 선택하고 해당 곡의 플레이를 시작한다.
- 상태: 구현 완료 / 사용자 확인 완료 (2026-11-15, PLAN.md 확인)
- 이 문서는 개발 진행에 따라 수정한다. 작업 범위 확대는 사용자 확인 후 반영한다.

### 요구사항
- 등록된 곡을 선택하고 해당 곡의 플레이를 시작한다.
- 기존 기능을 보존하고 요청 범위에 필요한 변경만 수행한다.
- 확정 사항 (2026-11-15 사용자 승인):

| 항목 | 확정값 |
|---|---|
| 곡 데이터 | 곡마다 `SongData` 에셋(제목, 아티스트, 채보, 음악[선택]), 곡 목록 에셋 `SongList`에 등록. 인스펙터에서 곡 추가·수정 |
| 테스트 곡 | 3곡. Test Song 1(기존 `test_chart`), Test Song 2·3(새 짧은 채보, 롱노트 포함). 아티스트 "Test", 음악 없음 |
| 추가 곡 (2026-11-15, 사용자 요청) | `kick` / 아티스트 `claude_ai`. 140 BPM 일렉트로닉, 90.6초, 파이썬으로 직접 합성한 WAV. 채보·SongData를 만들어 `SongList` 4번째에 등록 |
| 화면 흐름 | `SongSelectScene`(새 씬, 게임 시작 씬) → `IngameScene`(플레이) → `ResultScene`(RETRY / SONG SELECT) |
| 화면 배치 | 왼쪽 곡 목록, 오른쪽 선택한 곡 정보(제목, 아티스트, 노트 수, 길이), START 버튼 |
| 조작 | 마우스: 목록 클릭으로 선택, START로 시작. 키보드: ↑↓로 선택, Enter로 시작 |
| 시작 불가 | 곡이 없거나, 채보가 비었거나, 노트가 0개면 START 비활성 + "CANNOT PLAY" 표시 |
| 선택 기억 | 결과 화면에서 SONG SELECT로 돌아오면 마지막으로 고른 곡이 선택된 상태 |
| 키 설정 버튼 (2026-11-15, `key_setting_plan.md`) | KEY SETTING 버튼으로 설정 패널을 열고, 패널이 열린 동안 ↑↓/Enter 무시 |
| 단독 실행 | `IngameScene`을 바로 Play하면 인스펙터에 지정한 채보로 플레이 (AUTO 검증용). 2026-11-15부터 에디터 Play는 항상 SongSelectScene에서 시작하므로 에디터에서는 이 경로를 쓰지 않음(코드는 유지) |

## 2. 기존 코드 분석
- `GameplayManager`는 인스펙터의 `chart`(TextAsset)와 `audioSource`(clip이 있으면 곡 시간 기준)로 플레이한다. 곡을 고르는 구조는 없다.
- `ResultScreen`에는 RETRY 버튼만 있다.
- Build Settings: `IngameScene`(0), `ResultScene`(1).
- 입력은 Input System(`Keyboard.current`), UI는 uGUI + `InputSystemUIInputModule`.

## 3. 작업 범위 및 수정 대상 파일
| 파일 | 구분 | 내용 |
|---|---|---|
| `Assets/Scripts/Song/SongData.cs` | 신규 | 곡 에셋 (제목, 아티스트, 채보, 음악) |
| `Assets/Scripts/Song/SongList.cs` | 신규 | 곡 목록 에셋 |
| `Assets/Scripts/Song/SongSelection.cs` | 신규 | 고른 곡을 씬 사이에 넘겨 주는 `SongSelection.Selected` |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 신규 | 목록 버튼 생성, 선택 강조, ↑↓/Enter, 곡 정보 표시, START, 시작 가능 여부 확인 |
| `Assets/Scripts/Gameplay/GameplayManager.cs` | 수정 | 시작 시 `SongSelection.Selected`가 있으면 그 곡의 채보·음악 사용, 없으면 인스펙터 값 사용 |
| `Assets/Scripts/Result/ResultScreen.cs` | 수정 | SONG SELECT 버튼 연결 (`SongSelectScene` 불러오기) |
| `Assets/Scenes/SongSelectScene.unity` | 신규 | 카메라, Canvas(목록·정보·START), EventSystem, `SongSelectScreen` |
| `Assets/Scenes/ResultScene.unity` | 수정 | SONG SELECT 버튼 추가 (RETRY 옆) |
| `Assets/Charts/test_chart_2.txt`, `test_chart_3.txt` | 신규 | 테스트 채보 2개 |
| `Assets/Songs/TestSong1~3.asset`, `Assets/Songs/SongList.asset` | 신규 | 곡 에셋 3개, 목록 에셋 |
| `ProjectSettings/EditorBuildSettings.asset` | 수정 | SongSelectScene(0, 시작) → IngameScene(1) → ResultScene(2) (사용자 승인) |
| `Assets/Editor/PlayModeStartScene.cs` | 신규 (2026-11-15) | 에디터에서 Play 시 열려 있는 씬과 상관없이 SongSelectScene부터 시작 (`EditorSceneManager.playModeStartScene`) |
| `Assets/Scenes/SampleScene.unity` → `IngameScene.unity` | 이름 변경 (2026-11-15) | 씬 이름 변경, `SongSelectScreen`·`ResultScreen`의 `playSceneName`과 Build Settings 경로 갱신 |
| `Assets/Scripts/Song/SongSelectScreen.cs` | 수정 (2026-11-15, 키 설정) | 키 설정 패널이 열려 있으면 ↑↓/Enter 무시 |
| `Assets/Scenes/SongSelectScene.unity` | 수정 (2026-11-15, 키 설정) | KEY SETTING 버튼, 설정 패널 추가 |
| `Assets/Music/kick.wav` | 신규 (2026-11-15, kick) | 곡 음원 (44.1kHz 모노 16bit, 90.6초) |
| `Assets/Charts/kick_chart.txt` | 신규 (2026-11-15, kick) | 노트 200개(롱노트 20), 첫 노트 6.86초. 곡과 같은 악보에서 생성 |
| `Assets/Songs/Kick.asset`, `Assets/Songs/SongList.asset` | 신규·수정 (2026-11-15, kick) | 곡 에셋 생성, 목록 4번째에 추가 |
| `Assets/Scenes/IngameScene.unity` | 수정 (2026-11-15, kick) | `Music` 오브젝트(AudioSource, Play On Awake 끔, 반복 없음) 추가 → `GameplayManager.audioSource` 연결. 그전에는 연결이 비어 있어 음악이 재생되지 않았음 |

- 목록 버튼의 키보드 이동은 UI 기본 내비게이션을 끄고 `SongSelectScreen`에서만 처리한다(↑↓ 한 번에 두 번 움직이는 것 방지).
- 제외 범위: 곡 미리듣기, 앨범 이미지, 난이도 구분, 정렬·검색, 최고 기록 표시.

## 4. 상세 구현 방법 및 필요한 코드 스니펫
```csharp
// GameplayManager.Start: 곡 선택 화면에서 고른 곡이 있으면 그 곡으로 플레이
SongData song = SongSelection.Selected;
if (song != null)
{
    chart = song.Chart;
    if (audioSource != null) audioSource.clip = song.Music; // 음악이 없으면 null → 시작 후 경과 시간 사용
}
```
- 노트 수·길이는 곡 선택 화면에서 `ChartLoader.Load`로 채보를 읽어 계산한다. 노트 수는 일반·롱노트 각 1개, 길이는 마지막 노트 종료 시각을 `m:ss`로 표시.

## 5. 구현 순서 및 진행 상태
- [x] 곡 데이터 스크립트·테스트 채보·에셋 (`Assets/Songs/TestSong1~3.asset`, `SongList.asset`, `test_chart_2/3.txt`)
- [x] 곡 선택 화면 스크립트, `GameplayManager` 연결, 결과 화면 버튼 → 컴파일 확인 (에러 0건)
- [x] `SongSelectScene` 생성, `ResultScene` 버튼 추가(RETRY 왼쪽 -220, SONG SELECT 오른쪽 220), Build Settings 변경 (SongSelectScene 0 / IngameScene 1 / ResultScene 2)
- [x] 검증
- 계획에 없던 수정 (검증 중 발견, 2026-11-15):
  - 새 씬을 만들 때 `SongSelectScreen.songList` 참조가 비어 저장됨 → 씬을 다시 열어 연결 후 저장.
  - 프로젝트에 Enter Play Mode Options가 켜져 있어 Play를 다시 시작해도 static 값이 남음 → `IngameScene` 단독 실행 시 이전 세션에서 고른 곡(Test Song 2)으로 플레이되는 문제 발견. `SongSelection.Selected`와 `PlayResult.Last`를 Play 시작 시 비우도록 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 추가.

- 완료: 계획, 구현, Claude 자동 검증
- 진행 중: 없음
- 남은 작업: 사용자 확인 (↑↓/Enter 키, 마우스 클릭, 화면 배치)

## 6. 테스트 및 검증 방법
| 테스트 항목 | 기대 결과 | 상태 |
|---|---|---|
| 곡 목록 | 3곡 표시 | 통과 |
| 곡 선택 | 강조 표시와 곡 정보(제목·아티스트·노트 수·길이) 갱신 | 통과 (Test Song 1: NOTES 20 / 0:13, 2: 7 / 0:05, 3: 8 / 0:06) |
| 다른 곡 선택 | 선택 상태 갱신 | 통과 (범위 밖 `Select(99)`은 마지막 곡으로 맞춰짐) |
| 선택 없이 시작 / 빈 채보 | START 비활성 + CANNOT PLAY | 통과 (빈 채보: 메모리에서만 잠시 비우고 확인 후 복원, 에셋 변경 없음) |
| 곡 시작 | 고른 곡의 채보로 플레이 (AUTO 3단계 결과 점수 = 채보 계산값) | 통과 (Test Song 2: SS / 9800 / MAX COMBO 14 = 판정 8×1000 + 틱 6×300) |
| RETRY | 같은 곡으로 다시 시작 | 통과 (채보 test_chart_2, 점수 0부터) |
| SONG SELECT | 곡 선택 화면으로 돌아오고 마지막 곡이 선택된 상태 | 통과 (Test Song 2 강조) |
| 단독 실행 | `IngameScene` 바로 Play 시 AUTO 3단계 35800 유지 | 통과 (static 초기화 수정 후. 수정 전에는 9800으로 이전 곡이 남아 있었음) |
| 키보드·마우스 | ↑↓/Enter, 클릭 조작 | 미검증 (사용자 확인) |
| kick 곡 (2026-11-15) | 목록 4번째 표시, 음악 시간 기준 플레이, AUTO 3단계 Miss 0 | 통과 (NOTES 200 / 1:29, SS / 263200 / MAX COMBO 364) |
| 음악 없는 곡 (AudioSource 연결 후) | 기존처럼 시작 후 경과 시간으로 진행 | 통과 (Test Song 1 AUTO 3단계 35800) |

- [x] Unity 컴파일 오류 확인
- [x] 실제 플레이에서 기능 검증 (Claude가 SongSelectScene부터 Play 모드로 전체 흐름 실행, 버튼은 `onClick` 호출)
- [x] 관련 기존 기능 회귀 테스트 (IngameScene 단독 AUTO 3단계 35800, ResultScene 단독 실행 시 NO RESULT)

## 7. 검증 결과 및 미해결 사항
- 검증 결과 (2026-11-15, Play 모드를 `EditorApplication.Step()`으로 진행, AUTO TEST 3단계):
  - 컴파일 에러 0건, Play 모드 중 에러·경고 0건.
  - SongSelectScene 시작: 3곡 목록, Test Song 1 선택·강조, START 활성.
  - Test Song 2 클릭 → START → SampleScene이 `test_chart_2`로 플레이 → ResultScene `SS / 9800 / MAX COMBO 14 / 100.0% / PERFECT 8`.
  - RETRY → 같은 채보로 재시작, 같은 결과. SONG SELECT → 곡 선택 화면에서 Test Song 2가 선택된 상태.
  - 빈 채보 → `CANNOT PLAY`, START 비활성, `StartSong()` 호출해도 씬 전환 없음.
  - static 초기화 수정 후 SampleScene 단독 AUTO 3단계 35800, ResultScene 단독 실행 시 `NO RESULT`.
- 미해결: ↑↓/Enter 키 조작과 실제 마우스 클릭은 Claude가 입력을 넣을 수 없어 미검증 (사용자 확인 필요). 화면 배치 육안 확인 필요
- 변경 이력: 초안 작성 → 2026-11-15 미확정 사항 확정 및 상세 설계 반영 → 구현 및 검증 (참조 누락·static 초기화 문제 수정 포함) → 2026-11-15 씬 이름 변경(SampleScene → IngameScene), 에디터 Play 시작 씬을 SongSelectScene으로 고정
- 씬 이름 변경·Play 시작 씬 검증 (2026-11-15): IngameScene을 연 채 Play → SongSelectScene부터 시작. Test Song 1 START → IngameScene AUTO 3단계 35800(SS) → RETRY → AUTO 1단계 5000(D) → SONG SELECT → Test Song 3 START → AUTO 2단계 요약 로그와 결과 일치. 에러·경고 0건, Play 종료 후 씬 변경 없음.
- kick 곡 추가·AudioSource 연결 검증 (2026-11-15):
  - 곡 선택 화면: `kick / claude_ai / NOTES 200 / LENGTH 1:29`(길이는 마지막 노트 종료 기준), START 활성.
  - 음악 없이(연결 전) AUTO 3단계: SS / 263200 / MAX COMBO 364 / PERFECT 220(롱노트 시작·끝 2회 판정 포함) / MISS 0.
  - AudioSource 연결 후 실시간 진행(에디터 일시정지 중에는 오디오가 멈춰 Step 방식 불가 → 일시정지 해제 후 진행): 곡 시간 = `audioSource.time`, 음악 종료 후 ResultScene 전환, 결과 동일(263200).
  - Test Song 1(음악 없음) AUTO 3단계 35800 유지. 에러 0건(경고 1건은 검증 스크립트가 음악 없는 곡의 `time`을 읽어서 생긴 것), 씬 변경 남지 않음.
  - 관찰: 시작 직후 약 30초 동안 오디오가 2.2초 위치에서 멈췄다가 재개됨(노트도 함께 멈춰 판정 영향 없음). 에디터 포커스가 없는 상태 때문으로 추정, 미확인.
  - 미해결: 소리 품질과 출력 지연까지 포함한 실제 싱크, 시작 부분 끊김 여부는 사용자 확인 필요(출력 지연 보정은 오프셋 기능 범위).
