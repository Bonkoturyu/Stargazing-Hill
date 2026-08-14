# 별보기 언덕 / Stargazing Hill

[日本語](README.md) | [English](README.en.md) | [繁體中文](README.zh-Hant.md) | [简体中文](README.zh-Hans.md) | [한국어](README.ko.md)

현재 시각과 모두가 공유하는 20개 관측 지점 중 하나에 맞춰 실제 별과 달을 보여 주는 VRChat용 조용한 초원 월드입니다.

## 주요 특징

- HYG v4.1의 실제 항성 데이터를 UTC와 전역으로 선택한 20개 지점의 위도·동경에 따라 회전
- 현재 달의 겉보기 위치 표시
- 매시 정각에 시작하는 유성 이벤트와 IMO 2026 달력의 주요 유성우 11종
- 넓은 초원, 작은 언덕, 랜드마크 나무 한 그루, CC0 피크닉 공간
- YamaPlayer, QvPen, UnyStylus 공간
- 일본어·영어·번체중국어·간체중국어·한국어 전환이 가능한 월드 안내, 관측 지점, 디버그 패널
- 현재 인스턴스 인원과 로컬 입퇴장 기록
- Windows, Android／독립형 VR, iOS를 고려한 구성

12,495개의 별을 하나의 Mesh, 하나의 Renderer, 하나의 Additive Unlit Material로 미리 베이크합니다. 런타임에는 별을 하나씩 갱신하지 않고 천구 전체만 회전합니다. 그림이 포함된 [별하늘 구현 가이드(일본어)](docs/STARFIELD_IMPLEMENTATION_GUIDE.md)와 기준 기술 문서인 [Real Sky System 사양(일본어)](docs/REAL_SKY_SYSTEM.md)을 참고하세요.

## 프로젝트 상태

환경, 실제 별하늘, 대기 소산, 전역 공유 20개 지점 선택기, 달 계산, 유성 시스템, YamaPlayer 표준 재생 목록 작업 흐름, 드로잉 도구, 5개 언어 패널, 인원 및 입퇴장 기록, 디버그 조작, 모바일용 Shader를 구현했습니다. ClientSim 멀티플레이와 Windows／Android／iOS 실제 기기 최종 검증은 아직 진행 중입니다.

## 새 Clone 열기

Unity `2022.3.22f1`을 사용합니다. Clone을 VRChat Creator Companion에 추가하고 잠긴 VPM 의존성을 복원한 뒤, 문서에 지정된 YamaPlayer patch를 적용합니다. 정식 구매한 UnyStylus v1.3을 import하고 호환성 patch도 적용하세요. 그다음 `Assets/StargazingHill/Scenes/StargazingHill.unity`를 열고 다음 메뉴를 실행합니다.

`Stargazing Hill/Validate Saved Scene`

일반적인 사용에는 Scene 전체 재생성이 필요하지 않습니다. 전체 순서는 [설정 및 복원 문서(일본어)](docs/SETUP_AND_RESTORE.md)를 참고하세요.

## Unity 메뉴

- `Stargazing Hill/Content`: 안내 패널 선택·검증·저장 배치 적용과 버전 관리되는 피크닉 배치 유지보수
- `Stargazing Hill/Preview & Debug`: 별하늘과 유성을 로컬에서 미리 보기
- `Stargazing Hill/Build & Export`: 재배포용 unitypackage 생성
- `Stargazing Hill/Advanced`: 생성 콘텐츠 교체, 전체 Scene 재생성, SDK 복구. 영향을 이해하는 유지보수자용

`Advanced/Generated Content/Rebuild Complete World (Destructive)...`는 의도적으로 전체를 다시 만들 때만 사용하세요. 피크닉 생성기는 `Assets/StargazingHill/Editor/Data/PicnicLayout.json`을 읽습니다. Scene에서 피크닉을 수동 조정한 뒤에는 `Content/Picnic/Save Current Scene Layout to Generator...`를 실행하고 Scene과 JSON을 함께 commit하세요.

재생 목록은 YamaPlayer Inspector의 편집 버튼 또는 `YamaPlayer/Edit Playlist`에서 편집합니다. 이 프로젝트는 별도의 재생 목록 설정 파일이나 자동 동기화 경로를 유지하지 않습니다. 전체 재생성 시에는 저장된 Scene에서 표준 편집기로 설정한 YamaPlayer를 그대로 이어받습니다.

## 재배포

`Stargazing Hill/Build & Export/Redistributable UnityPackage...`로 배포 패키지를 만드세요. 이 경로는 프로젝트 소유 자산과 재배포 가능한 CC0／CC BY-SA 콘텐츠만 내보내며, YamaPlayer, QvPen, 유료 UnyStylus 파일은 의도적으로 제외합니다. 재배포 패키지에는 Unity의 일반 **Include dependencies** 옵션을 사용하지 마세요.

## 문서

- [문서 색인(일본어)](docs/README.md)
- [프로젝트 사양(일본어)](docs/PROJECT_SPEC.md)
- [별하늘 구현 가이드(일본어)](docs/STARFIELD_IMPLEMENTATION_GUIDE.md)
- [Real Sky System 기술 사양(일본어)](docs/REAL_SKY_SYSTEM.md)
- [설정 및 복원(일본어)](docs/SETUP_AND_RESTORE.md)
- [서드파티 의존성(일본어)](docs/legal/THIRD_PARTY_DEPENDENCIES.md)
- [서드파티 자산 및 라이선스(일본어)](docs/legal/THIRD_PARTY_ASSETS.md)

## GitHub Release

`v*` 태그를 푸시하면 GitHub Actions가 재배포용 UnityPackage를 생성하고 검사한 뒤, SHA-256 체크섬과 함께 ZIP으로 묶어 해당 Release에 첨부합니다. Unity Editor나 Unity 라이선스는 필요하지 않으며 YamaPlayer, QvPen, 유료 UnyStylus 파일은 포함하지 않습니다. 현재 Actions의 Budget/Billing 제한이 있으므로 최초 실제 실행 확인은 제한 해제 후 진행합니다. [공개 전 감사(일본어)](docs/PUBLIC_RELEASE_AUDIT.md)를 참고하세요.
