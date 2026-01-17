# Unity Game Framework

> Unity 게임 개발에서 반복적으로 구현되는 시스템들을  
> **프레임워크 단위로 정리한 공통 게임 개발 기반**입니다.
>
> 특정 게임에 종속되지 않으며,  
> 여러 프로젝트에서 재사용·확장하는 것을 목표로 합니다.

---

## 📦 Frameworks

현재 포함된 프레임워크는 다음과 같습니다.

- **Data Parsing Framework**  
  Google Sheet 기반 게임 데이터 파이프라인

- **Pooling Framework**  
  Type 기반 공용 오브젝트 풀링 시스템

- **UI System Framework**  
  우선순위 / 선점 기반 UI 흐름 관리 시스템

- **Audio System Framework**
  Addressables + Sheet 기반 사운드 재생/관리 시스템

> 새로운 프레임워크는 지속적으로 추가될 예정입니다.

---

## 1️⃣ Data Parsing Framework

### 기능
- Google Sheet → ScriptableObject 자동 변환
- Sheet Tab 선택 후 SO 생성
- SO 갱신(Update) 지원
- 런타임 Dictionary 캐싱

### 사용 방법

#### 런타임 데이터 접근
```cs
ItemData item = ItemTable.Instance.Get(1001);
```

---

## 2️⃣ Pooling Framework

### 기능
- Type 기반 풀링
- Dictionary + Stack 구조
- Instantiate / Destroy 최소화
- 상태 초기화 훅 제공

### 사용 방법

```cs
// 풀에 남아있는 같은 타입 인스턴스를 재사용하거나,
// 없으면 prefab으로 새로 생성해서 반환한다.
MyObject obj = Pool.Get<MyObject>(myObjectPrefab);

// 사용 (위치, 데이터 등은 사용자가 초기화)
obj.transform.position = spawnPosition;
obj.gameObject.SetActive(true);

// 사용이 끝나면 Destroy하지 않고 비활성화 후 풀에 반환한다.
Pool.Return(obj);
```

- **`Get<T>(prefab)`**
  -> 재사용(있으면) / 생성(없으면)  
- **`Return(obj)`**
  -> 비활성화 후 풀에 보관, 다음 요청 시 재사용

---

## (Optional) Pool Settings ScriptableObject
Pooling Framework는 선택적으로
**ScriptableObject 기반 풀 설정을 사용할 수 있습니다.**

|SO|
|-|
|<img width="392" height="249" alt="image" src="https://github.com/user-attachments/assets/2b25c199-f671-493f-9a68-f04054997782" />|



### Pool Settings 항목
- Prefab : 풀링 대상 오브젝트
- Prewarm Count : 시작 시 미리 생성할 개수
- Max Count : 풀 최대 개수
- Auto Expand : 최대 개수 초과 시 자동 생성 여부
- Default Parent : 풀링 오브젝트의 기본 부모 Transform

> 단순한 풀링이 필요한 경우에는 설정 없이 사용 가능하며,
> 대량 생성·성능 관리가 필요한 경우에만 PoolingSettings를 사용하면 됩니다.

---

## 3️⃣ UI System Framework

### 기능
- 단일 팝업 표시 (Single Active Popup)
- 우선순위 처리 (Low / Normal / High / Critical)
- 선점 / 대기 / 교체 정책
- Suspend / Resume 흐름
- 닫힘 연출 대응 (비동기 Close)
- Model 입력 차단
- Pooling 연계

### 사용 방법

#### 팝업 열기
```cs
UIManager.Instance.RequestPopup(
    popupPrefab,
    EPopupPriority.High
);
```

#### 정책 지정
```cs
UIManager.Instance.RequestPopup(
    popupPrefab,
    EPopupPriority.High,
    policy: EPopupPolicy.ReplaceCurrent
);
```

#### 팝업 닫기
```cs
UIManager.Instance.CloseTopPopup();
```

---

## 4️⃣ Audio System Framework

### 기능
- Sound Sheet 기반 사운드 관리
  * Channel(BGM/SFX/UI/Vocie), Volume, Loop, MaxConcurrent 등을 Sheet에서 관리
- ESound 자동 생성
  * Sheet의 `FileName`을 기반으로 `ESound` enum 자동 생성
- Addressables 자동 등록
  * Audio 폴더 스캔 후 Addressables 그룹에 자동 등록
  * Addressables address = fileName 규칙 강제
- 사운드 재생 통합 API
  * `SoundManager.Instance.PlaySound(ESound.xxx)` 형태로 단순 사용
- BGM 크로스페이드
- 동시 재생 제한
  * 사운드별 MaxConcurrent 설정 지원

### 사용 방법

#### 1) 사운드 파일 추가
오디오 파일을 아래 주소 폴더에 추가

`Assets/Audio/`

예)
* Assets/Audio/SFX_Test.wav
* Assets/Audio//BGM_Test.mp3

#### 2) Google Sheet(Sound 탭)에 Row 추가
`FileName`은 확장자 제외 파일명과 반드시 동일해야 합니다.

예)
|Google Sheet|
|-|
|<img width="521" height="104" alt="image" src="https://github.com/user-attachments/assets/3908d0a7-2312-4e49-9d7b-13bbabb55319" />|

#### 3) ESound 생성
SoundSO가 준비되면 `FileName`을 기반으로 enum을 자동 생성합니다.

Unity Ediotor에서 아래 버튼을 누릅니다.

`Framework/Audio/Generate/ESound From SoundTable`

* 동작:
  * `SoundSO`를 읽어 `FileName` 목록 수집
  * `ESound.cs`를 자동 생성
* 생성 위치:
  * `Assets/Scripts/Audio/ESound.cs`

이제 런타임에서 다음처럼 바로 사용 가능합니다.

`SoundManager.Instance.PlaySound(ESound.UI_Click);`

#### 4) SoundDatabase 빌드 + Addressables 자동 등록
SoundDatabaseSO를 갱신하고 AudioClip을 Addressables에 자동 등록합니다.

`Framework/Audio/Build Sound Database From Sheet + Folder`

* 입력:
  * `SoundSO` (시트 파싱 결과)
  * `Assets/Aduio/` 폴더의 AudioClip들
* 출력:
  * `SoundDatabaseSO` entries 자동 갱신
  * Addressables 그룹에 자동 등록 + address 통일(fileName)

런타임에서 자동 로드되도록 `SoundDatabaseSO`는 아래 위치를 사용합니다.

>런타임에서 자동 로드되도록 `SoundDataSO`는 아래 위치를 사용합니다.
* "Assets/Resources/SoundDatabase.asset
  (또는 Assets/Resources/Audio/SoundDatabase.asset)"

---

#### 5) 런타임 사용

```cs
>
// SFX/UI/Voice
SoundManager.Instance.PlaySound(ESound.UI_Click);
SoundManager.Instance.PlaySound(ESound.SFX_Merge);

// BGM
SoundManager.Instance.PlaySound(ESound.BGM_Main);

// BGM 정지
SoundManager.Instance.StopBgm();

// 모든 원샷 정지
SoundManager.Instance.StopAllOneShots();

// 마스터 볼륨 설정
SoundManager.Instance.SetMasterVolume(0.0f);

// 채널별 볼륨 설정
SoundManager.Instance.SetChannelVolume(EAudioChannel.BGM, 0.6f);
SoundManager.Instance.SetChannelVolume(EAudioChannel.SFX, 1.0f);
SoundManager.Instance.SetChannelVolume(EAudioChannel.UI, 0.8f);
SoundManager.Instance.SetChannelVolume(EAudioChannel.Voice, 1.0f);
>
```



