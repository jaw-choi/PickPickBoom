# PickPickBoom 프로토타입 가이드

## 개요

이 프로젝트는 "3장 중 1장을 고르는 타워 등반형 카드 로그라이크" 프로토타입입니다.

입력 시스템은 반드시 **새 Input System만 사용**합니다.

- `InputSystemUIInputModule` 사용
- 구형 `StandaloneInputModule` 사용 금지
- 구형 `Input Manager` / `UnityEngine.Input` 기준 설계 금지
- 프로젝트 `Active Input Handling`은 새 Input System 전용 유지

핵심 흐름은 단순합니다.

- 각 행마다 카드 3장이 주어집니다.
- 라운드가 올라갈수록 카드 행 수가 `1행, 2행, 3행...` 으로 늘어납니다.
- 플레이어는 뒷면만 보이는 카드 중 1장을 선택합니다.
- 카드 결과에 따라 다음 층으로 올라가거나, 아이템을 얻거나, 저주를 받거나, 몬스터에게 죽습니다.
- 한 층은 `계단`을 찾거나 `몬스터`에게 죽을 때까지 계속됩니다.

중요한 점은 이 프로젝트가 **런타임에 UI를 자동 생성하지 않는 구조**라는 것입니다.

즉, 아래 항목은 모두 Unity 에디터에서 직접 배치하고 연결해야 합니다.

- Canvas
- HUD 텍스트
- 상태 메시지 영역
- 게임 오버 패널
- Restart 버튼
- 카드 프리팹
- 카드 프리팹 원본
- EventSystem

이 구조의 목적은 명확합니다.

- 디자이너가 직접 배치와 스타일을 수정할 수 있음
- 코드가 씬 레이아웃을 강제로 만들지 않음
- 카드 프리팹을 교체하거나 외형을 자유롭게 바꿀 수 있음

---

## 현재 구현된 게임 규칙

### 카드 종류

- `Stair`
  다음 층으로 이동합니다.

- `Monster`
  기본적으로 즉시 게임 오버입니다.
  단, 방패가 있으면 방패를 소모하고 살아남습니다.

- `GoodItem`
  현재 구현된 아이템은 `Shield` 1종입니다.

- `BadItem`
  현재 구현된 나쁜 효과는 `Curse` 1종입니다.

- `Empty`
  아무 일도 일어나지 않습니다.

### 층 진행 규칙

- 매 라운드는 여러 행으로 시작하며, 각 행은 카드 3장으로 구성됩니다.
- 각 행에서는 카드 3장 중 정확히 1장만 선택합니다.
- `Stair`, `GoodItem`, `BadItem`, `Empty`를 고르면 그 행 선택이 끝나고 다음 층(다음 행)으로 자동 이동합니다.
- `Monster`를 고르면 방패가 없을 경우 게임 오버입니다.

즉, 한 행은 카드 한 장을 뽑는 순간 종료됩니다.

### 방패 규칙

- 방패는 최대 1개만 보유할 수 있습니다.
- 이미 방패가 있는 상태에서 다시 방패를 얻어도 중첩되지 않습니다.
- 몬스터 카드를 골랐을 때 방패가 있으면:
  - 방패를 소모합니다.
  - 죽지 않습니다.
  - 그 몬스터 카드는 사용 완료 상태가 됩니다.
  - 현재 행 선택이 끝나고 다음 행으로 이동합니다.

### 저주 규칙

현재 프로토타입의 저주는 단순한 형태로 구현되어 있습니다.

- 저주 카드를 선택하면 `다음 카드 결과 메시지 숨김` 상태가 걸립니다.
- 다음 선택의 실제 효과는 정상적으로 적용됩니다.
- 다만 상태 메시지 영역에는 상세 결과 대신 가려진 문구가 출력됩니다.
- 저주는 1회 소모형입니다.

이 방식은 구조가 단순하고, 나중에 다른 BadItem을 추가하기 쉽도록 설계되어 있습니다.

---

## 층 생성 방식

이제 층 생성은 모든 층에서 같은 규칙을 사용합니다.

### 모든 층 공통 규칙

각 행의 카드 3장은 항상 아래 구성입니다.

- `Stair` 1장 고정
- `Monster` 1장 고정
- 마지막 1장은 설정한 후보 카드 중 가중치 랜덤

즉, 매 층은 반드시 다음 구조를 가집니다.

- 계단 카드 1장
- 몬스터 카드 1장
- 추가 카드 1장

추가 카드 가중치는 현재 아래처럼 설정됩니다.

- `Empty`: 60
- `BadItem`: 20
- `GoodItem`: 20

중요한 점은 마지막 1장만 랜덤이라는 것입니다.
즉, 이제는 모든 층에 몬스터가 반드시 하나 있고, 계단도 반드시 하나 있습니다.

카드 위치는 매 층 셔플되므로, 어떤 버튼에 무엇이 들어갈지는 매번 달라질 수 있습니다.

---

## 스크립트 구조

현재 구현된 핵심 스크립트는 아래와 같습니다.

### `Assets/Script/CardTypes.cs`

포함 내용:

- `CardType`
- `GoodItemType`
- `BadItemType`
- `StatusTone`
- `CardData`

역할:

- 카드 종류와 아이템 종류 정의
- 카드 1장의 데이터 표현
- 카드 공개 시 표시할 제목/설명 문자열 제공

### `Assets/Script/PlayerState.cs`

역할:

- 방패 보유 여부 관리
- 다음 결과 숨김 저주 상태 관리
- 세션 리셋 처리

### `Assets/Script/WeightedRandomUtility.cs`

역할:

- 가중치 배열을 받아 랜덤 인덱스를 반환
- 마지막 한 장의 랜덤 카드 선택에 사용

### `Assets/Script/FloorGenerator.cs`

역할:

- 층 번호를 받아 현재 라운드 전체 카드 목록을 생성
- 모든 층에서 `Stair 1장 + Monster 1장 + 랜덤 추가 카드 1장` 생성
- 마지막 1장의 가중치 랜덤 처리
- 카드 순서 셔플 처리

### `Assets/Script/CardView.cs`

역할:

- 카드 프리팹 1장의 UI 표현 담당
- 카드 뒷면 상태 표시
- 카드 공개 애니메이션
- 카드 사용 완료 상태 표시
- 버튼 클릭 이벤트 전달

중요:

- 이 스크립트는 **카드 프리팹에 붙여야 합니다**
- 카드 프리팹 원본을 `UIManager`에 연결해 필요한 수만큼 자동 배치합니다

### `Assets/Script/UIManager.cs`

역할:

- HUD 텍스트 갱신
- 카드 프리팹 풀 관리와 동적 배치
- `ScrollRect` 보드 스크롤 제어
- 상태 메시지 색상 관리
- 게임 오버 패널 표시
- Restart 버튼 이벤트 연결

### `Assets/Script/GameManager.cs`

역할:

- 게임 전체 흐름 제어
- 현재 라운드 / 최고 라운드 관리
- 라운드 시작 시 보드 미리보기와 카메라 워킹 연출 제어
- 카드 선택 처리
- 아이템/저주/몬스터/계단 효과 적용
- 게임 오버 처리
- 재시작 처리

---

## 에디터에서 해야 하는 작업

이 섹션이 가장 중요합니다.

아래 순서대로 진행하면 바로 플레이 가능한 상태를 만들 수 있습니다.

---

## 1. 씬 기본 구조 만들기

추천 Hierarchy 구조는 아래와 같습니다.

```text
SampleScene
├─ Main Camera
├─ EventSystem
├─ GameRoot
│  ├─ FloorGenerator
│  ├─ UIManager
│  └─ GameManager
└─ Canvas
   ├─ TopBar
   │  ├─ CurrentFloorText
   │  ├─ BestFloorText
   │  ├─ ShieldStatusText
   │  └─ CurseStatusText
   ├─ StatusPanel
   │  └─ StatusMessageText
   ├─ BoardScrollView
   │  ├─ Viewport
   │  │  └─ CardContent
   └─ GameOverPanel
      ├─ GameOverText
      └─ RestartButton
```

오브젝트 이름은 꼭 같을 필요는 없습니다.
하지만 연결 실수를 줄이기 위해 비슷하게 맞추는 것을 권장합니다.

---

## 2. EventSystem 설정

버튼 입력을 받으려면 `EventSystem`이 필요합니다.

### 해야 할 일

1. Hierarchy에서 `UI > Event System`을 생성합니다.
2. 생성된 오브젝트를 선택합니다.
3. 인스펙터에서 입력 모듈을 확인합니다.

### 반드시 확인할 것

- `EventSystem` 컴포넌트가 있어야 합니다.
- `InputSystemUIInputModule`를 사용해야 합니다.
- 구형 `StandaloneInputModule`는 제거하는 것을 권장합니다.

이 프로젝트는 **새 Input System 전용** 기준으로 맞춰져 있습니다.

### Project Settings에서도 확인할 것

`Edit > Project Settings > Player > Other Settings > Active Input Handling` 에서 아래처럼 맞춰 주세요.

- `Input System Package (New)` 만 사용
- `Both` 사용 금지
- `Input Manager (Old)` 사용 금지

---

## 3. Canvas 만들기

### 해야 할 일

1. Hierarchy에서 `UI > Canvas`를 생성합니다.
2. `Canvas Scaler` 설정을 확인합니다.

### 권장 설정

- `Render Mode`: `Screen Space - Overlay`
- `UI Scale Mode`: `Scale With Screen Size`
- `Reference Resolution`: `1080 x 1920`
- `Screen Match Mode`: `Match Width Or Height`
- `Match`: `0.5` 전후

이 값은 모바일/세로형 카드 UI를 기준으로 무난합니다.
원하면 프로젝트 방향에 맞게 바꿔도 됩니다.

---

## 4. HUD 텍스트 만들기

`UIManager`가 갱신할 텍스트들을 직접 만들어야 합니다.

### 필요한 텍스트

- 현재 층 텍스트
- 최고 층 텍스트
- 방패 상태 텍스트
- 저주 상태 텍스트
- 상태 메시지 텍스트
- 게임 오버 텍스트

### 만드는 방법

각 텍스트는 `UI > Text - TextMeshPro`로 생성하는 것을 권장합니다.

예시:

- `CurrentFloorText`
- `BestFloorText`
- `ShieldStatusText`
- `CurseStatusText`
- `StatusMessageText`
- `GameOverText`

폰트는 프로젝트에 있는 TextMeshPro 폰트를 사용해도 되고, 원하는 폰트 자산을 새로 연결해도 됩니다.

---

## 5. 카드 프리팹 만들기

이 프로젝트의 핵심은 카드 1장을 프리팹으로 만들고, 그 프리팹을 `UIManager`가 필요한 수만큼 런타임에 배치하는 방식입니다.
즉, 에디터에서는 카드 프리팹 원본 1개만 준비하고, 보드에 들어갈 카드 수는 라운드에 따라 자동으로 늘어납니다.

### 카드 프리팹 추천 구조

```text
CardPrefab
├─ Background (Image)
├─ TitleText (Optional TMP_Text)
└─ DetailText (Optional TMP_Text)
```

실제로는 버튼이 루트에 붙는 구성이 가장 편합니다.
중요한 점은 `TitleText`, `DetailText`가 이제 필수가 아니라는 것입니다.
버튼 이미지나 카드 내부 그래픽에 이미 정보가 들어 있다면 텍스트 없이 사용해도 됩니다.
카드 표시는 색상 변경이 아니라 스프라이트 교체 방식으로 동작합니다.

### 권장 구성

`CardPrefab` 루트에 아래 컴포넌트를 추가합니다.

- `RectTransform`
- `Image`
- `Button`
- `CanvasGroup`
- `CardView`

그리고 자식으로 텍스트 2개를 둡니다.

- `TitleText`
- `DetailText`

이 두 텍스트는 선택 사항입니다.
이미지 안에 카드 이름이나 장식이 포함되어 있다면 만들지 않아도 됩니다.

### 카드 프리팹 제작 순서

1. `Canvas` 아래 임시 카드 오브젝트를 하나 만듭니다.
2. 루트에 `Image`, `Button`, `CanvasGroup`, `CardView`를 추가합니다.
3. 필요하다면 자식으로 `TitleText`, `DetailText`를 만듭니다.
4. 버튼 이미지, 아이콘, 카드 프레임, 크기, 라운드 느낌, 그림자 등 원하는 스타일을 적용합니다.
5. 이 오브젝트를 Project 창으로 드래그해 프리팹으로 만듭니다.

### CardView 인스펙터 연결

`CardView` 컴포넌트에는 아래 레퍼런스를 연결해야 합니다.

- `Button`
  카드 루트의 `Button`

- `Background Image`
  카드 루트의 `Image`

- `Title Text`
  선택 사항입니다. 텍스트를 쓸 때만 연결합니다.

- `Detail Text`
  선택 사항입니다. 텍스트를 쓸 때만 연결합니다.

- `Canvas Group`
  카드 루트의 `CanvasGroup`

실제 필수 레퍼런스는 아래 두 개입니다.

- `Button`
- `Background Image`

그리고 카드 표시용 스프라이트를 아래처럼 연결해야 합니다.

- `Face Down Sprite`
  카드 선택 전 공통 뒷면 이미지

- `Stair Sprite`
  계단 카드 공개 이미지

- `Monster Sprite`
  몬스터 카드 공개 이미지

- `Shield Sprite`
  방패 카드 공개 이미지

- `Curse Sprite`
  저주 카드 공개 이미지

- `Empty Sprite`
  빈 카드 공개 이미지

- `Fallback Good Item Sprite`
  나중에 다른 GoodItem이 추가될 때 쓸 선택용 공통 이미지

- `Fallback Bad Item Sprite`
  나중에 다른 BadItem이 추가될 때 쓸 선택용 공통 이미지

### CardView 이미지 매핑

`CardView`는 카드 상태에 따라 `Background Image.sprite`를 교체합니다.

- 카드 선택 전: `Face Down Sprite`
- 계단 공개 시: `Stair Sprite`
- 몬스터 공개 시: `Monster Sprite`
- 방패 공개 시: `Shield Sprite`
- 저주 공개 시: `Curse Sprite`
- 빈 카드 공개 시: `Empty Sprite`

즉, 카드 외형은 색이 아니라 이미지 자산으로 구분됩니다.
사용자가 각 카드 종류에 맞는 이미지를 직접 연결하면, 공개 시점에 그 이미지가 정확히 매칭됩니다.

---

## 6. BoardScrollView 만들기

중요:
이제 카드는 3장 고정이 아니라 라운드가 올라갈수록 `1행, 2행, 3행...` 으로 늘어납니다.
그래서 씬에 카드 인스턴스 3개를 미리 배치하는 방식이 아니라, `ScrollRect` 안에 카드 프리팹을 필요한 수만큼 자동 배치하는 구조를 사용합니다.

### 해야 할 일

1. `Canvas` 아래에 `UI > Scroll View`를 만듭니다.
2. 이름을 `BoardScrollView`로 바꾸는 것을 권장합니다.
3. `Viewport`와 `Content` 오브젝트가 자동으로 만들어졌는지 확인합니다.
4. `Content` 이름을 `CardContent`로 바꾸는 것을 권장합니다.
5. `CardContent`에는 카드가 3열로 배치되도록 레이아웃 그룹을 붙입니다.

권장 설정:

- `Grid Layout Group`
- `Constraint`: `Fixed Column Count`
- `Constraint Count`: `3`

즉, 라운드 1이면 1행 3장, 라운드 2면 2행 6장, 라운드 3이면 3행 9장 식으로 자동 배치됩니다.

### ScrollRect 역할

- 행이 적을 때는 그냥 한 화면에 보입니다.
- 행이 많아지면 `BoardScrollView`가 위에서 아래로 훑는 연출에 사용됩니다.
- 연출이 끝난 뒤에는 가장 아래 행이 현재 진행 행으로 고정되고, 해당 행의 카드 3장 중 하나만 선택할 수 있습니다.

---

## 7. GameOverPanel 만들기

게임 오버 시 보여줄 패널을 직접 만들어야 합니다.

### 권장 구조

```text
GameOverPanel
├─ Background
├─ GameOverText
└─ RestartButton
```

### 해야 할 일

1. `Canvas` 아래에 `GameOverPanel`을 만듭니다.
2. 배경용 `Image`를 넣어 어둡게 깔아도 됩니다.
3. `GameOverText`를 넣습니다.
4. `RestartButton`을 넣습니다.
5. 시작 시 이 패널은 꺼져 있어도 되고 켜져 있어도 됩니다.

`UIManager`가 시작할 때 이 패널을 숨깁니다.

### RestartButton

`RestartButton`은 일반 `Button - TextMeshPro`로 만들어도 충분합니다.

버튼의 `OnClick`에 직접 함수를 넣을 필요는 없습니다.
`UIManager`가 코드에서 연결합니다.

---

## 8. GameRoot 만들기

게임 로직 스크립트를 붙일 오브젝트를 분리하는 것이 좋습니다.

### 추천 구조

`GameRoot` 아래에 빈 오브젝트 3개를 두는 방식이 가장 명확합니다.

- `FloorGenerator`
- `UIManager`
- `GameManager`

### 각 오브젝트에 붙일 컴포넌트

- `FloorGenerator` 오브젝트
  - `FloorGenerator`

- `UIManager` 오브젝트
  - `UIManager`

- `GameManager` 오브젝트
  - `GameManager`

원하면 한 오브젝트에 몰아 넣을 수도 있지만, 역할 분리를 위해 나눠 두는 편이 좋습니다.

---

## 9. UIManager 인스펙터 연결

`UIManager` 오브젝트를 선택하고 아래를 연결합니다.

### HUD

- `Current Floor Text` -> `CurrentFloorText`
- `Best Floor Text` -> `BestFloorText`
- `Shield Status Text` -> `ShieldStatusText`
- `Curse Status Text` -> `CurseStatusText`
- `Status Message Text` -> `StatusMessageText`

### Board

- `Board Scroll Rect` -> `BoardScrollView`의 `ScrollRect`
- `Card Content Root` -> `BoardScrollView/Viewport/CardContent`
- `Card View Prefab` -> 만든 카드 프리팹 원본의 `CardView`

중요:

- 더 이상 `Card Views` 배열에 3개를 수동으로 넣지 않습니다.
- `UIManager`가 라운드마다 필요한 수만큼 카드 프리팹을 자동 생성합니다.
- `CardContent` 레이아웃은 3열 기준으로 맞춰 두어야 행 단위로 잘 보입니다.

### Game Over

- `Game Over Panel` -> `GameOverPanel`
- `Game Over Text` -> `GameOverText`
- `Restart Button` -> `RestartButton`

### Status Colors

아래 색은 인스펙터에서 원하는 톤으로 바꿔도 됩니다.

- Neutral
- Good
- Bad
- Warning

---

## 10. GameManager 인스펙터 연결

`GameManager` 오브젝트를 선택하고 아래를 연결합니다.

- `Floor Generator` -> `FloorGenerator` 컴포넌트
- `UI Manager` -> `UIManager` 컴포넌트

### Timing 항목

- `Board Pan Duration Per Row`
  행이 많아졌을 때 위에서 아래로 훑는 카메라 워킹 속도

- `Floor Preview Duration`
  전체 보드를 보여준 뒤 잠깐 멈추는 시간

- `Floor Preview Flip Duration`
  모든 카드가 앞면에서 뒷면으로 뒤집히는 시간

- `Reveal Animation Duration`
  카드 공개 애니메이션 시간

- `Post Reveal Delay`
  카드 공개 후 결과 적용 전 잠깐 멈추는 시간

- `Floor Transition Delay`
  계단 선택 후 다음 층으로 넘어가기 전 대기 시간

권장 시작값:

- `Board Pan Duration Per Row`: `0.4`
- `Floor Preview Duration`: `1.25`
- `Floor Preview Flip Duration`: `0.22`
- `Reveal Animation Duration`: `0.18`
- `Post Reveal Delay`: `0.18`
- `Floor Transition Delay`: `0.45`

---

## 11. FloorGenerator 인스펙터 설정

`FloorGenerator`는 마지막 한 장의 후보 가중치를 인스펙터에서 조정할 수 있습니다.

기본 권장값:

- `Empty Weight`: `60`
- `Bad Item Weight`: `20`
- `Good Item Weight`: `20`

이 값들은 마지막 1장을 생성할 때 사용됩니다.

즉, 한 층의 구성은 항상 다음과 같습니다.

- 계단 1장 고정
- 몬스터 1장 고정
- 마지막 1장은 위 확률 비율 기반

그리고 라운드 번호가 곧 행 수가 됩니다.

- 1라운드: 1행
- 2라운드: 2행
- 3라운드: 3행

즉, 전체 카드 수는 `행 수 x 3` 입니다.

---

## 12. 플레이 전 체크리스트

플레이 전에 반드시 확인하세요.

- `EventSystem`이 씬에 있다
- `InputSystemUIInputModule`를 사용한다
- `Canvas`가 있다
- `CurrentFloorText`가 연결되어 있다
- `BestFloorText`가 연결되어 있다
- `ShieldStatusText`가 연결되어 있다
- `CurseStatusText`가 연결되어 있다
- `StatusMessageText`가 연결되어 있다
- `GameOverPanel`이 연결되어 있다
- `GameOverText`가 연결되어 있다
- `RestartButton`이 연결되어 있다
- `BoardScrollView`가 씬에 있다
- `BoardScrollView`의 `Viewport/CardContent`가 있다
- 카드 프리팹 원본에 `CardView`가 붙어 있다
- `UIManager`에 `Board Scroll Rect`, `Card Content Root`, `Card View Prefab`가 연결되어 있다
- `GameManager`에 `FloorGenerator`와 `UIManager`가 연결되어 있다

---

## 실제 플레이 흐름

### 게임 시작

- `GameManager`가 세션을 초기화합니다.
- 현재 라운드는 1로 시작합니다.
- 최고 라운드도 1로 시작합니다.
- 방패와 저주 상태를 초기화합니다.
- 1라운드이므로 1행 3장의 카드가 생성됩니다.
- 행 수가 많아질수록 카드 총 개수도 함께 늘어납니다.
- 모든 카드의 실제 내용을 먼저 앞면으로 보여줍니다.
- 행이 여러 개면 `BoardScrollView`가 위에서 아래로 훑으며 전체 보드를 보여줍니다.
- 그 다음 모든 카드가 공통 뒷면 이미지로 뒤집힙니다.
- 연출이 끝나면 가장 아래 행이 현재 층으로 고정됩니다.
- 뒤집기 연출이 끝난 뒤에만 플레이어가 현재 행의 카드 3장 중 하나를 선택할 수 있습니다.

### 카드 선택

- 라운드 시작 연출이 끝난 뒤 현재 행 카드만 누를 수 있습니다.
- 카드 버튼을 누르면 해당 카드가 뒤집히며 공개됩니다.
- 잠깐의 공개 연출 후 카드 효과가 적용됩니다.
- 선택이 끝나면 다음 행으로 자동 스크롤 이동해 다음 카드 선택을 유도합니다.

### 계단 선택 시

- 현재 층이 종료됩니다.
- 층 번호가 1 증가합니다.
- 새 라운드의 전체 카드 보드를 생성합니다.
- UI가 갱신됩니다.

### 몬스터 선택 시

- 방패가 있으면:
  - 방패 소모
  - 죽지 않음
  - 몬스터 카드 사용 완료
  - 같은 층 계속 진행

- 방패가 없으면:
  - 즉시 게임 오버
  - 게임 오버 패널 표시

### 방패 선택 시

- 방패가 없으면 획득
- 방패가 이미 있으면 중첩되지 않음
- 카드 사용 완료
- 같은 층 계속 진행

### 저주 선택 시

- 다음 카드의 결과 메시지를 숨기는 상태 적용
- 카드 사용 완료
- 같은 층 계속 진행

### 빈 카드 선택 시

- 아무 일도 일어나지 않음
- 카드 사용 완료
- 같은 층 계속 진행

---

## 카드 프리팹 디자인 팁

코드는 카드의 동작만 제어합니다.
외형은 전부 프리팹에서 결정할 수 있습니다.

추천하는 단순한 표현:

- 카드 루트에 세로형 직사각형 `Image`
- 제목 텍스트 크게
- 설명 텍스트 작게
- 뒷면일 때는 어두운 색
- 공개 후에는 종류에 따라 배경색 변경

예를 들어:

- 계단: 파랑
- 몬스터: 빨강
- 좋은 아이템: 초록
- 나쁜 아이템: 보라/주황
- 빈 카드: 회색

이 정도만으로도 플레이 테스트용 프로토타입은 충분합니다.

---

## 카드 프리팹을 바꾸고 싶을 때

가능합니다.

다만 아래 조건은 유지해야 합니다.

- 루트에 `Button`이 있어야 함
- 루트에 `Image`가 있어야 함
- 루트에 `CanvasGroup`이 있으면 사용 완료 연출이 더 자연스러움
- `CardView`가 연결되어 있어야 함
- `TitleText`, `DetailText`는 필요할 때만 연결하면 됨
- `Face Down Sprite`, `Stair Sprite`, `Monster Sprite`, `Shield Sprite`, `Curse Sprite`, `Empty Sprite`가 연결되어 있어야 함

즉, 프리팹의 시각 구조는 바꿔도 되지만 `CardView`가 최소한 `Button`, `Background Image`, 그리고 카드 표시용 스프라이트들은 참조할 수 있어야 합니다.

---

## 스크립트별 연결 요약

### CardView

직접 연결해야 하는 것:

- Button
- Background Image
- Title Text
  텍스트를 쓸 때만 연결
- Detail Text
  텍스트를 쓸 때만 연결
- Canvas Group
- Face Down Sprite
- Stair Sprite
- Monster Sprite
- Shield Sprite
- Curse Sprite
- Empty Sprite

### UIManager

직접 연결해야 하는 것:

- 현재 층 텍스트
- 최고 층 텍스트
- 방패 상태 텍스트
- 저주 상태 텍스트
- 상태 메시지 텍스트
- 카드 3개 `CardView`
- 게임 오버 패널
- 게임 오버 텍스트
- 재시작 버튼

### GameManager

직접 연결해야 하는 것:

- FloorGenerator
- UIManager

### FloorGenerator

직접 연결해야 하는 것:

- 없음

가중치 숫자만 조정하면 됩니다.

---

## 구현 상세 설명

### FloorGenerator가 층을 만드는 방식

`FloorGenerator`는 `GenerateFloor(int floorNumber)`를 통해 현재 라운드에 맞는 전체 카드 목록을 반환합니다.

동작 방식:

1. 라운드 번호를 기준으로 행 수를 계산
2. 각 행마다 `Stair` 1장 추가
3. 각 행마다 `Monster` 1장 추가
4. 각 행마다 마지막 1장을 `Empty / BadItem / GoodItem` 가중치 랜덤으로 선택
5. 각 행 내부 카드 순서를 셔플
6. 모든 행 카드를 한 리스트로 합쳐 UI에 전달

즉, 이제는 모든 행에 계단과 몬스터가 반드시 함께 등장합니다.

### Shield가 작동하는 방식

`PlayerState`가 `HasShield`를 관리합니다.

흐름:

1. 플레이어가 `Shield` 카드 선택
2. 이미 방패가 없으면 `HasShield = true`
3. 이후 몬스터 선택 시 `ConsumeShield()` 시도
4. 방패가 있으면:
   - `HasShield = false`
   - 사망 무효
   - 현재 층 계속 진행

즉, 방패는 "즉사 1회 방어권"입니다.

### Curse가 작동하는 방식

`PlayerState`가 `HideNextResultMessage`를 관리합니다.

흐름:

1. 플레이어가 저주 카드 선택
2. `HideNextResultMessage = true`
3. 다음 카드 선택 시 `GameManager`가 저주를 먼저 소모
4. 실제 효과는 적용되지만 상태 메시지는 가려진 문구로 출력
5. 이후 저주는 해제

---

## 자주 발생하는 문제

### 카드가 눌리지 않는 경우

다음을 확인하세요.

- `EventSystem`이 있는지
- `InputSystemUIInputModule`가 붙어 있는지
- 카드 루트에 `Button`이 있는지
- 카드 위를 덮는 다른 UI가 없는지
- `GraphicRaycaster`가 Canvas에 있는지

### 카드가 공개되지 않는 경우

다음을 확인하세요.

- `CardView`의 `Button`, `Background Image` 연결 여부
- `CardView`의 `Face Down Sprite`, `Stair Sprite`, `Monster Sprite`, `Shield Sprite`, `Curse Sprite`, `Empty Sprite` 연결 여부
- 텍스트를 사용하는 프리팹이라면 `Title Text`, `Detail Text` 연결 여부
- `UIManager`의 `Board Scroll Rect`, `Card Content Root`, `Card View Prefab` 연결 여부

### 게임 시작 직후 에러가 나는 경우

대부분 인스펙터 레퍼런스 누락입니다.

특히 아래를 확인하세요.

- `GameManager`의 `FloorGenerator`, `UIManager`
- `UIManager`의 각 텍스트
- `UIManager`의 `Board Scroll Rect`, `Card Content Root`, `Card View Prefab`
- `UIManager`의 게임 오버 패널 / 텍스트 / 버튼

### 방패가 있는데 몬스터에서 죽는 경우

다음을 확인하세요.

- 실제 선택한 카드가 몬스터인지
- 방패 상태 HUD가 `보유 중`으로 표시되는지
- 방패 카드를 얻었을 때 이미 보유 중이어서 중첩되지 않은 것은 아닌지

---

## 권장 작업 순서

처음 세팅할 때는 아래 순서가 가장 안전합니다.

1. EventSystem 준비
2. Canvas와 HUD 텍스트 만들기
3. BoardScrollView와 CardContent 만들기
4. GameOverPanel 만들기
5. 카드 1장 프리팹 만들기
6. GameRoot와 각 매니저 오브젝트 만들기
7. `CardView` 연결
8. `UIManager` 연결
9. `GameManager` 연결
10. 플레이 테스트

---

## 이 구조가 좋은 이유

이 프로젝트는 일부러 "코드가 씬을 만들지 않는 방식"으로 작성했습니다.

장점:

- UI를 코드 수정 없이 재배치 가능
- 카드 프리팹 디자인을 자유롭게 수정 가능
- 씬 작업과 코드 작업의 책임이 분리됨
- 나중에 애니메이션, 아이콘, 사운드, 프리팹 변형 추가가 쉬움

즉, 프로토타입인데도 나중에 확장하기 좋은 형태입니다.

---

## 다음에 확장하기 쉬운 항목

현재 구조는 아래 확장에 유리합니다.

- GoodItem 추가
  예: 힐, 예지, 몬스터 무시, 재선택

- BadItem 추가
  예: 다음 층 카드 수 제한, 몬스터 확률 증가, 계단 위치 힌트 제거

- 카드 아이콘 추가

- 층별 테마색 변경

- 카드 공개 애니메이션 강화

- 카드 설명 툴팁 추가

- 층 진행 로그 추가

---

## 현재 구현 파일 목록

- `Assets/Script/CardTypes.cs`
- `Assets/Script/PlayerState.cs`
- `Assets/Script/WeightedRandomUtility.cs`
- `Assets/Script/FloorGenerator.cs`
- `Assets/Script/CardView.cs`
- `Assets/Script/UIManager.cs`
- `Assets/Script/GameManager.cs`

이 파일들만으로 프로토타입 핵심 로직은 동작하도록 구성되어 있습니다.
