# PLAN.md

## 1. 문서 목적

- 프로젝트의 기능별 구현 계획서 목록을 관리한다.
- 기능별 구현 순서 및 진행 상태를 관리한다.
- 상세 구현 방법은 각 기능별 plan.md를 참조한다.

## 2. 기능별 계획서 목록

### 2-1. 핵심 플레이
| 순서 | 기능 | 계획서 | 상태 |
|---|---|---|---|
| 1 | 핵심 플레이 | core_gameplay_plan.md | 확인 |
| 2 | 키 설정 및 변경 | key_setting_plan.md | 확인 |
| 3 | 노트 배속 설정 | note_speed_plan.md | 확인 |
| 4 | 점수 및 콤보 | score_combo_plan.md | 확인 |
| 5 | 롱노트 | long_note_plan.md | 확인 |
| 6 | 게임 결과 화면 | result_screen_plan.md | 확인 |
| 7 | 곡 선택 화면 | song_select_plan.md | 확인 |
| 8 | 자동 플레이(AUTO TEST) | auto_play_plan.md | 확인 |

### 2-2. 플레이 안정성 및 데이터 구조
| 순서 | 기능 | 계획서 | 상태 |
|---|---|---|---|
| 1 | 오프셋(싱크 보정) 설정 | offset_setting_plan.md | 확인 |
| 2 | 일시정지 및 재시작 | pause_restart_plan.md | 확인 |
| 3 | 곡 및 난이도 데이터 관리 | song_data_plan.md | 확인 |
| 4 | 채보 데이터 관리 | chart_data_plan.md | 확인 |

### 2-3. 사용자 편의성 및 기록 관리
| 순서 | 기능 | 계획서 | 상태 |
|---|---|---|---|
| 1 | 플레이 기록 저장 | record_save_plan.md | 확인 |
| 2 | 사운드 및 볼륨 설정 | audio_setting_plan.md | 확인 |
| 3 | 설정 화면 통합 | settings_save_plan.md | 확인 |

### 2-4. UI/UX
| 순서 | 기능 | 계획서 | 상태 |
|---|---|---|---|
| 1 | 인게임 UI/UX | ingame_ui_ux_plan.md | 확인 |
| 2 | 곡 선택 UI/UX | song_select_ui_ux_plan.md | 확인 |
| 3 | 결과 화면 UI/UX | result_ui_ux_plan.md | 확인 |

## 3. 관리 규칙

- 신규 기능은 사용자 승인 후 목록에 추가한다.
- 구현 순서는 개발 진행에 따라 변경할 수 있다.
- 진행 상태는 실제 구현 상태를 기준으로 갱신한다.
- 구현이 완료 되었다고 판단이 된다면 작성자에게 보고후 상태를 변경한다.
ex) 
| 1 | 핵심 플레이 | core_gameplay_plan.md | 미확인 | ->
| 1 | 핵심 플레이 | core_gameplay_plan.md | 확인 |
- 상태 값:
  - 미작성: 상세 계획서가 아직 없음
  - 미확인: 계획서 작성 후 구현·검증 중이거나 작성자 확인 전
  - 확인: 작성자 확인 완료
- 2. 기능별 계획서 목록 내용을 수정하는 것 이외의 내용을 수정하려고 한다면은 작성자에게 허락을 요구한다.
- 상세 구현 내용은 기능별 계획서에서 관리한다.