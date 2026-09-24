# 지침 C · 뷰와 연출

**먼저 읽을 것:** `Docs/작업_계약.md` → `Docs/Unity_구조.md` §4 트레일 절 §6 → `Docs/좋은_피자_위대한_리듬_기획서.md` §7 §10

한 줄 요약: **화면을 만든다.** 원판, 노트 아이콘, 트레일, 롱노트 꼬리, 배경 전환, 피자 오븐 인·아웃, 최소 HUD, 그리고 이 전부를 담은 트레일러 씬. 판정과 박자 계산은 하지 않는다.

이 씬이 최종 산출물이다. 영상 편집이 여기서 찍은 화면을 받는다.

---

## 1. 내가 만드는 파일

```
Assets/GPGR/View/
    GPGR.View.asmdef
    WheelView.cs
    NoteView.cs
    NoteViewPool.cs
    TailView.cs
    TrailView.cs
    PhaseThemeView.cs
    PizzaOvenView.cs
    HudView.cs
    TrailerBootstrap.cs
Assets/GPGR/Scenes/
    Trailer.unity
Assets/GPGR/Prefabs/
    Note.prefab
    Wheel.prefab
Assets/GPGR/Art/
    (플레이스홀더 스프라이트, 머티리얼)
AgentScripts/
    C_BuildTrailerScene.cs       (Assets 밖. 씬을 코드로 조립하는 빌더. 12절)
```

`GPGR.View.asmdef`는 `GPGR.Charting`과 `GPGR.Runtime`을 참조한다.

**씬과 프리팹은 나만 만든다.** A·B가 씬에 뭘 올려 달라고 하면 내가 올린다.

---

## 2. B의 공개 면만 읽는다

`Docs/작업_계약.md` §5가 내가 쓸 전부다. 정리하면:

| 필요한 것 | 어디서 |
| --- | --- |
| 노트가 생겼다 | `NoteWheel.Spawned(NoteInstance)` |
| 노트가 사라졌다 | `NoteWheel.Cleared(NoteInstance)` |
| 판정이 났다 | `NoteWheel.Judged(NoteInstance, Judgement, double 오차ms)` |
| 지금 각도 | `NoteInstance.AngleDeg` |
| 지금 살아 있는 노트 | `NoteWheel.Active` |
| 원판 중심·반지름 | `NoteWheel.Center`, `NoteWheel.radius` |
| 페이즈가 바뀌었다 | `TimelineDirector.PhaseEntered(PhaseDefinition, int)` |
| 정확도 | `AccuracyGauge.Changed(float)` |
| 현재 박 | `ChartClock.Beat` |
| 과거·미래 각도 | `MotionPath.AngleAt(row.path, localBeat)` |

노트 위치는 `MotionPath.Position(note.AngleDeg, wheel.Center, wheel.radius)`로 구한다. 직접 삼각함수를 다시 쓰지 않는다.

뷰 갱신은 `LateUpdate`에서 한다. B가 `Update`에서 각도를 갱신하므로 한 프레임 밀리지 않으려면 그 뒤여야 한다.

---

## 3. `WheelView` — 고정된 것들

피자 그림, 링, 하단 히트 표시는 **움직이지 않는다.** 원판이 도는 게 아니라 노트가 경로를 따라 도는 것이다. 어느 페이즈에서도 원판 위치와 하단 히트 라인은 그대로다. 오븐 페이즈에서도 같다.

- 피자: 원 스프라이트. 중앙
- 링: 반지름 위치의 얇은 원. `LineRenderer`로 원을 그리거나 도넛 스프라이트
- 하단 히트 표시: 270° 위치의 짧은 눈금이나 밝은 점
- 원판 틴트: `PhaseDefinition.overrideWheelTint`가 켜져 있을 때만 색을 바꾼다. 꺼져 있으면 직전 색 유지. 데모는 전부 꺼져 있어서 안 바뀐다

반지름 잠정 3.0 유닛, 중심 (0,0). 카메라는 원판과 HUD가 다 들어오게 잡는다.

---

## 4. `NoteView` — 아이콘

- `NoteWheel.Spawned`에서 풀에서 하나 꺼내 붙이고, `Cleared`에서 돌려보낸다. 매번 `Instantiate`하지 않는다
- 스프라이트는 `NoteRow.icon`, 색은 `NoteRow.color`
- 매 프레임 위치를 `AngleDeg`에서 구한다. **아이콘은 궤도를 따라 움직이지만 회전하지 않는다.** 세로를 유지한다. 페퍼로니가 굴러가면 안 된다
- 롱노트 머리도 같은 `NoteView`다. 머리는 하단에 도착한 뒤 그 자리에 머문다

### 재료 구분은 아이콘과 색이 한다

배경이 밝음/어두움 두 장뿐이라, 반죽·소스·치즈·페퍼로니 네 페이즈의 연출 값이 전부 같다. 이 네 개를 화면에서 구분해 주는 건 노트 아이콘과 색밖에 없다. 플레이스홀더라도 색은 확실히 다르게 한다.

계약 §7의 색: 반죽 `#E8D4A8`, 소스 `#C0392B`, 치즈 `#F1C40F`, 페퍼로니 `#8E2B20`, 오븐 `#E67E22`.

---

## 5. `TailView` — 롱노트 꼬리

꼬리는 **노트 몸통**이다. 잔상이 아니다.

머리가 하단에 도착한 뒤 `tailBeats`만큼 노트가 살아 있고, 그동안 꼬리가 머리가 지나온 원호를 채운다. 덮는 원호는 따로 저장된 값이 아니라 이렇게 구한다.

```
localBeat   = clock.Beat - note.OriginBeat
꼬리 뒤끝 각도 = MotionPath.AngleAt(note.Row.path, localBeat - note.Row.tailBeats)
꼬리 앞끝 각도 = note.AngleDeg
```

두 각도 사이의 원호를 몸통으로 그린다. 머리가 멈춰 있으니 이 구간은 히트 순간에 가장 길고, 박이 지날수록 하단 쪽으로 줄어들다가 0이 되어 노트가 사라진다. 꼬리 길이가 접근 박과 같으면 히트 순간의 꼬리가 스폰 각까지 닿는다.

`AngleAt`이 첫 키보다 이른 박에 대해 첫 키 각도를 돌려주므로, 스폰 직후에는 꼬리가 자연히 짧다. 따로 처리하지 않아도 된다.

원호는 `LineRenderer`에 각도를 잘게 쪼개 점을 넣거나 메시를 만든다. 두께가 있어야 몸통처럼 보인다.

`kind == Long`인 노트에만 붙인다.

---

## 6. `TrailView` — 트레일

트레일은 보이는 쪽이고 패턴이 아니다. 노트 **뒤를 따르는 잔상**이다.

- 노트가 원판에 있는 동안 켜 둔다
- **각도가 되돌아가도 실제 지나온 점을 따라간다.** 치즈가 왼쪽으로 튕겼다가 돌아올 때 트레일도 같이 갔다 와야 한다. 그래서 각도를 계산해 그리지 말고 프레임마다 실제 위치를 기록해 쓴다. Unity `TrailRenderer`를 쓰면 거의 공짜로 된다
- 노트가 사라지면 트레일도 같이 사라진다. 풀로 돌려보낼 때 `Clear()`를 잊지 않는다
- 색은 `NoteRow.color`를 그대로 쓴다
- 길이·두께·페이드는 인스펙터에 노출한다. 잠정 0.5박 / 반지름의 0.06 / 0.2초

꼬리와 트레일은 다른 것이다. 꼬리는 롱노트 몸통이고 길이가 `tailBeats`로 정해진다. 트레일은 모든 노트에 붙는 잔상이고 길이가 연출 값이다.

---

## 7. `PhaseThemeView` — 배경과 틴트

`TimelineDirector.PhaseEntered`를 받아 `PhaseDefinition`의 필드만 적용한다.

- `backdrop == Bright` → 밝은 주방 배경
- `backdrop == Dark` → 어두운 오븐 배경
- `overrideWheelTint`가 켜져 있으면 원판 틴트를 그 색으로

배경은 두 장뿐이다. 0.3초쯤 크로스페이드한다. 뚝 끊기면 트레일러에서 튄다.

**페이즈 이름을 보고 분기하지 않는다.** `if (phase.phaseName == "오븐")` 같은 코드를 쓰면 안 된다. 필드만 읽는다. 나중에 재료가 늘어도 이 컴포넌트는 그대로여야 한다.

---

## 8. `PizzaOvenView` — 피자 오븐 인·아웃

`PhaseDefinition.pizza`만 읽는다.

| 값 | 연출 |
| --- | --- |
| `None` | 피자 원래 크기, 원래 자리 |
| `IntoOven` | 오븐 쪽으로 이동하며 작아진다. 스케일 1.0 → 0.35, 0.6초 |
| `OutOfOven` | 다시 나오며 원래 크기로 |

**원판과 히트 라인은 따라 움직이지 않는다.** 어두운 배경에서도 노트는 같은 하단에서 친다. 피자 그림만 움직인다. 원판 링과 히트 표시를 피자와 같은 부모에 두지 않도록 계층을 짠다. 이걸 틀리면 오븐 페이즈에서 노트가 안 맞물린다.

피자에 소스·치즈·토핑이 붙는 그림은 만들지 않는다. 영상 편집이 한다.

화덕 불꽃 이펙트 자리는 비워 두고, 아트가 오면 붙일 수 있게 빈 `Transform` 하나를 둔다.

---

## 9. `HudView` — 최소 HUD

세 개만이다.

- **현재 페이즈 이름:** `PhaseEntered`의 `phase.phaseName`을 그대로 띄운다
- **정확도:** `AccuracyGauge.Changed`를 받아 `%`로. 소수점 없이
- **판정 팝업:** `NoteWheel.Judged`를 받아 `PERFECT` / `GOOD` / `BAD` / `MISS`. 히트 라인 근처에서 짧게 뜨고 사라진다. 트레일러에서는 앞의 두 개만 나온다

체크리스트, 손님 말풍선, 재료 트레이, 주문 UI, 진행 바는 **만들지 않는다.** 영상 편집이 오버레이로 붙인다.

`Judged`의 오차는 부호가 살아 있다. 지금은 안 쓰지만 빠름/느림 표시를 붙이려면 여기 있다.

---

## 10. `Trailer.unity`와 `TrailerBootstrap`

씬에 올릴 것:

- 카메라 (URP 2D)
- 배경 두 장 (`PhaseThemeView`)
- 피자 (`PizzaOvenView`)
- 원판 (`WheelView`, `NoteWheel`)
- `ChartClock`, `TimelineDirector`, `AudioSource`
- `AutoPlayHitSource`와 `PlayerHitSource`
- HUD 캔버스 (`HudView`)
- `TrailerBootstrap`

`TrailerBootstrap`은 인스펙터에서 `SongChart`를 받아 `TimelineDirector.Begin(chart)`를 부르고, `chart.playback`에 따라 `AutoPlayHitSource`와 `PlayerHitSource` 중 하나만 켠다. A가 만든 `Assets/GPGR/Charts/DemoChart.asset`을 기본으로 넣는다.

R 키로 처음부터 다시 돌리는 정도는 넣어도 좋다. 재촬영이 편해진다.

### 촬영을 생각한 것들

- 게임 뷰 해상도를 1920×1080으로 고정한다
- HUD를 끄고 켜는 토글을 하나 둔다. 편집이 자기 오버레이를 쓰고 싶어 할 수 있다
- 배경을 단색으로 바꾸는 토글도 있으면 좋다. 합성이 편해진다

---

## 11. 플레이스홀더 아트

아트가 오기 전까지 쓸 것들이다. `Assets/GPGR/Art/`에 둔다.

- 피자: 베이지 원
- 링: 회색 얇은 원
- 히트 표시: 흰 짧은 눈금
- 노트: 색만 다른 원 5종. 계약 §7 색
- 배경: 밝은 회색 면, 어두운 갈색 면

교체 가능하게 만든다. `SpriteRenderer.sprite`만 바꿔서 끝나야 한다. 스프라이트를 코드에서 `Resources.Load`로 잡지 말고 인스펙터로 받는다.

노트 아이콘은 `NoteRow.icon`으로 들어가므로, 플레이스홀더를 만든 뒤 A에게 "데모 에셋 행에 이 스프라이트를 넣어 달라"고 요청한다. 내가 에셋을 고치지 않는다.

---

## 12. CLI로 확인하는 법 — 나에게는 이게 필수다

`Docs/작업_계약.md` §3가 전체 규칙이다. **에이전트는 Unity Editor를 클릭할 수 없다.** 씬을 손으로 조립할 방법이 없으니 코드로 만든다.

### 씬은 빌더 스크립트로 만든다

`AgentScripts/C_BuildTrailerScene.cs`에 `public static class TrailerSceneBuilder { public static int Build() { ... } }`를 쓰고 돌린다. `Assets/` 밖이라 에셋 임포트도 도메인 리로드도 안 일어난다.

```bash
unity command run_script --file AgentScripts/C_BuildTrailerScene.cs --dry_run true    # 컴파일만
unity command run_script --file AgentScripts/C_BuildTrailerScene.cs --entry TrailerSceneBuilder.Build
```

빌더 안에서 `EditorSceneManager`로 씬을 새로 만들고, 오브젝트를 붙이고, 컴포넌트 필드를 채우고, `Assets/GPGR/Scenes/Trailer.unity`로 저장한다. 프리팹도 `PrefabUtility`로 같이 만든다.

**씬을 한 번 손으로 만들고 끝내지 말고, 빌더를 계속 진실로 유지한다.** 씬 파일이 깨지거나 값을 바꿔야 할 때 빌더를 고쳐 다시 돌리면 된다. 씬 `.unity`를 직접 편집하는 것보다 안전하고, 무엇이 왜 그렇게 놓였는지가 코드로 남는다.

오브젝트가 몇 개뿐인 소소한 수정은 개별 명령으로도 된다.

```bash
unity command open_scene --path GPGR/Scenes/Trailer
unity command get_scene_hierarchy
unity command find_gameobjects --type NoteWheel
unity command set_component_properties --target /Wheel --type NoteWheel --properties '{"radius": 3.0}'
unity command save_scene
```

### 눈으로 확인한다

이게 내 작업의 확인 수단이다. 안 찍어 보면 맞는지 알 수 없다.

```bash
unity command capture_scene_view --save_path Screenshots/scene.png

unity command editor_play
unity command capture_game_view --save_path Screenshots/play_01.png --source screen   # 오버레이 UI까지
unity command editor_stop
```

`--source screen`은 Play Mode에서만 되고, Screen Space - Overlay 캔버스(HUD)가 들어온다. `camera`는 HUD를 놓친다.

페이즈별로 한 장씩 찍어 두면 좋다. 오븐 페이즈에서 **원판과 히트 라인이 안 움직였는지**는 스크린샷 두 장을 비교하는 게 가장 확실하다.

### 플레이 모드는 내 것이다

`editor_play` / `editor_stop` / `editor_pause`는 나만 쓴다. A와 B는 테스트로 확인한다. 다 끝나면 `editor_stop`으로 반드시 빠져나온다. 켜 둔 채 두면 A·B의 `recompile`이 막힌다.

`unity command console`로 런타임 로그를 읽는다.

`simulate_key`는 개발용 Player 빌드가 있어야 하는 Runtime 명령이라 Editor Play Mode에서는 안 된다. 키보드 입력 확인은 사람이 해야 한다. 자동 재생 경로만 CLI로 확인한다.

---

## 13. 완료 기준

- [ ] 데모 채보가 자동 재생으로 끝까지 돌고, 여섯 페이즈가 화면에서 순서대로 보인다
- [ ] 노트가 왼쪽에서 하단으로 반시계로 오고, 히트 박에 하단 히트 표시와 맞물린다
- [ ] 치즈가 첫 타 뒤 왼쪽으로 튕기고 다시 하단에 돌아와 두 번째 타를 맞는다. 트레일도 같이 갔다 온다
- [ ] 롱노트 머리가 하단에 닿은 뒤 꼬리가 원호에 걸리고, 꼬리 길이가 지나면 사라진다
- [ ] 오븐 페이즈에서 배경이 어두워지고 피자가 작아져 들어가는데, **원판과 히트 라인은 그대로**다
- [ ] 완성 페이즈에서 배경이 밝아지고 피자가 나온다
- [ ] HUD에 페이즈 이름·정확도·판정 팝업이 나오고, Bad·Miss가 한 번도 안 뜬다
- [ ] 아이콘이 궤도를 따라가지만 회전하지 않는다
- [ ] 코드 어디에도 재료 이름이나 페이즈 이름으로 분기하는 곳이 없다

---

## 14. 하지 않는 것

- 판정, 오차 계산, 정확도 감점 → B
- 박자·페이즈 경계 계산 → B (`ChartLayout`은 A)
- `SongChart` 필드 추가, 데모 에셋 수정 → A
- 손님 말풍선, 주문 UI, 스테이지 선택, 경영 화면, 타격 이펙트, 피자에 재료가 붙는 그림 → 영상 편집이 한다. 만들지 않는다
- `Docs/*.md` 수정. 모순을 찾으면 사람에게 보고한다
