# 한밭대학교 컴퓨터공학과 Instant팀

**팀 구성**
- 20227126 김만종
- 20217142 양경찬

## <u>Teamate</u> Project Background

- ### 필요성

  * 기존의 숨바꼭질(Hide and Seek) 장르와 소셜 디덕션(Social Deduction) 장르를 결합하여 플레이어 간의 심리전을 강조한 새로운 멀티플레이 경험 제공
  * 단순한 물리적 숨기가 아닌, 플레이어를 모방하는 AI NPC들 사이에서 플레이어 Seeker로부터 자신의 '행동'을 숨겨야 하는 고도화된 게임플레이
  * 연구용으로 사용되는 Unity ML-Agents를 활용하여 실제 플레이어처럼 행동하는 AI NPC를 구현하고, 실제 게임에 적용함으로써 활용 방법 제시

- ### 기존 해결책의 문제점

  * **기존 소셜 디덕션 게임:** 대부분 플레이어 간의 직접적인 상호작용(대화, 투표)에 의존하며, 게임 환경 내 AI가 탐지나 교란의 핵심 요소로 작용하는 경우가 드묾
  * **기존 숨바꼭질 게임:** 주로 오브젝트 변신(Prop Hunt)이나 정적인 숨기에 초점을 맞춤. 본 프로젝트처럼 다수의 AI NPC 사이에서 '행동 패턴'을 위장하여 플레이어 Seeker를 속이는 방식의 게임은 부족함

## System Design

- ### System Requirements

  * **Unity Engine**: 6000.2.6f
  * **Netcode for GameObjects**: 2.6.0
  * **ML-Agents**: 2.0.1
  * **Unity Services**: 1.1.8
    * Authentication (인증), Multiplayer (세션 관리)
  * **Unity Cinemachine**: 3.1.4
    * 3인칭 궤도 카메라(Orbital Follow)
  * **DOTween Pro** (Third-Party): 1.0.480
    * UI 애니메이션 연출

## Case Study

- ### Description

  * **소셜 디덕션 및 숨바꼭질 장르 융합:** 플레이어 Hider가 플레이어 Seeker를 피해 AI NPC들 사이에서 생존하는 PvP 규칙을 채택
  * **AI NPC 행동 모방 (ML-Agents):** Hider 플레이어와 동일한 외형(`AnimalType`)을 가진 NPC(`Npa.cs`)를 `NpcSpawner.cs`가 스폰함. ML-Agents를 사용하여 이 NPC들이 플레이어와 유사하게(점프, 스핀, 이동) 행동하도록 학습시켜 Hider가 군중 속에 자신의 정체를 숨길 수 있는 환경을 구축
  * **플레이어 역할 시스템 (Seeker/Hider):** `RoleManager.cs`가 게임 시작 시 무작위로 플레이어들에게 Seeker 또는 Hider 역할을 배정함
    * `SeekerRole.cs`: 플레이어 Seeker는 더 빠른 이동 속도를 가지며, 공격(`TryInteract`)을 통해 다른 플레이어(`HittableBody`)에게 피해를 줄 수 있음
    * `HiderRole.cs`: 플레이어 Hider는 맵 상의 오브젝트(`InteractableObject`)와 상호작용(예: 아이템 줍기)이 가능하며, NPC처럼 행동하여 Seeker의 눈을 속여야 함
  * **Unity Netcode (NGO) 아키텍처:** 호스트-서버(Host-Server) 모델 기반. `ConnectionManager`가 Unity Multiplayer Service를 통해 세션을 관리하며, `GameManager`가 `NetworkList<PlayerData>`로 모든 플레이어의 역할과 상태를 동기화함. `PlayManager`는 게임 흐름(타이머, 역할 배정, 스폰)을 RPC와 `NetworkVariable`로 관리함
  * **구형(Spherical) 월드 물리:** `PlanetGravity.cs`와 `PlanetBody.cs`를 통해 캐릭터들이 구형 행성 표면을 자연스럽게 이동하고 표면에 맞춰 정렬되도록 구현함
  * **Hider 미션 시스템:** Hider 플레이어에게 생존 외 추가 목표(점프 횟수, 특정 아이템 줍기 등)를 부여하여 게임플레이에 다양성을 더함. 미션 성공 시 버프, 실패 시 디버프를 제공하여 위험과 보상을 동시에 제공함

## Conclusion

- ### 주요 성과

  * **완전한 PvPvE 멀티플레이 게임 루프 구현:** Unity Services와 Netcode for Gameobjects를 활용하여 세션 관리, 로비, 역할 배정, 인게임 플레이(Hider 생존 및 미션 수행, Seeker 탐색 및 공격), 결과 처리까지 이어지는 완전한 멀티플레이 게임 사이클을 성공적으로 구현함
  * **핵심 소셜 디덕션 메커니즘 구축:** 플레이어 Seeker가 플레이어 Hider를 다수의 AI NPC(`Npa.cs`) 사이에서 찾아내야 하는 핵심 게임플레이 메커니즘을 구현함. Hider는 NPC의 행동을 모방하여 자신의 정체를 숨겨야 함
  * **이벤트 기반 시스템 아키텍처:** `GamePlayEventHandler`와 `MissionNotifier` 등 이벤트 버스를 사용하여 UI, 게임 로직, 미션 시스템 간의 결합도를 낮추고 유연한 구조를 설계함
  * **확장 가능한 컨텐츠 구조:** 동물(`AnimalData`), 미션(`MissionData`), 상호작용 오브젝트(`InteractionData`) 등을 `ScriptableObject` 기반으로 설계하여, 코드 수정 없이 새로운 게임 요소를 쉽게 추가하고 관리할 수 있는 시스템을 구축함

- ### 향후 발전 방향

  * **Hider AI(NPC) 행동 고도화:** ML-Agents 학습(`HiderTrainAgent.cs`)을 통해 수집된 플레이어 행동 데이터를 기반으로, 현재의 `Npa.cs`보다 더 정교하고 플레이어와 구별하기 어려운 NPC 행동 모델을 생성 및 적용
  * **전용 서버(Dedicated Server) 도입:** 현재 호스트-서버 모델의 안정성 한계를 극복하기 위해, Unity Game Server Hosting (Multiplay) 등을 활용한 전용 서버 아키텍처로 전환하여 더 안정적이고 확장 가능한 멀티플레이 환경 제공
  * **컨텐츠 확장 및 밸런싱:** `AnimalData`에 정의된 다양한 동물 모델 구현, `MissionType` 기반의 새로운 Hider 미션 추가, Seeker와 Hider 간의 스킬 또는 능력 추가 등을 통해 게임플레이 깊이 확장 및 역할 간 밸런스 조정
  
## Project Outcome
- ### 2025 년 한밭대학교 컴퓨터공학과 캡스톤 디자인 전시회 출품
- The Zoo - 멀티플레이 심리전 기반 네트워크 게임 개발
