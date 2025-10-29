using Interactions;
using Players;
using Scriptable;
using UI.InGame.Mission;
using UnityEngine;

namespace Mission
{
    public class MissionExecutor : MonoBehaviour
    {
        [SerializeField] private CanvasGroup seekerMission;
        [SerializeField] private MissionUI missionUI;
        [SerializeField] private MissionTimer timer;

        public MissionType currentType = MissionType.None;
        public int targetValue;

        public float progress;
        public bool isProgressing;
        public bool isComplete;

        public void Start()
        {
            timer.OnTimerFinished += CheckMission;
            timer.OnTimerFinished += UnsubscribeEvents;
        }

        internal void SetSeekerVisible(bool show)
        {
            seekerMission.alpha = show ? 1 : 0;
            seekerMission.interactable = show;
            seekerMission.blocksRaycasts = show;
        }

        internal void SetVisible(bool show)
        {
            missionUI.SetVisible(show);

            missionUI.AnimateShow();
        }

        internal void SetMission(MissionType type, string desc, int target)
        {
            currentType = type;
            targetValue = target;

            progress = 0;
            isComplete = false;
            isProgressing = false;

            missionUI.SetMission(desc, target);
            timer.StartTimer();

            SubscribeEvents(currentType);
        }

        private void CheckMission()
        {
            if(progress >= targetValue)
                OnMissionSuccess();
            else
                OnMissionFailed();
        }

        private void SubscribeEvents(MissionType type)
        {
            var notifier = MissionNotifier.Instance;

            switch (type)
            {
                case MissionType.JumpTimes:
                    PlayerLocator.LocalPlayer.OnJumpCallback += OnJump;
                    break;
                case MissionType.SpinForSeconds:
                    PlayerLocator.LocalPlayer.OnSpinCallback += OnSpin;
                    break;
                case MissionType.EatApple:
                case MissionType.EatMushroom:
                    notifier.OnPickup += OnPickup;
                    break;
                case MissionType.StayOnRock:
                    notifier.OnStayRock += OnStayRock;
                    break;
            }
        }

        private void UnsubscribeEvents()
        {
            PlayerLocator.LocalPlayer.OnJumpCallback -= OnJump;
            PlayerLocator.LocalPlayer.OnSpinCallback -= OnSpin;
            MissionNotifier.Instance.OnPickup -= OnPickup;
            MissionNotifier.Instance.OnStayRock -= OnStayRock;
        }

        private void UpdateMission(float delta = 1f)
        {
            progress += delta;
            missionUI.UpdateMission(progress);
        }

        private void OnMissionSuccess()
        {
            progress = 0;

            missionUI.OnMissionSuccess();
            PlayerLocator.LocalPlayer.ApplyRandomReward();
        }

        private void OnMissionFailed()
        {
            progress = 0;

            missionUI.OnMissionFailed();
            PlayerLocator.LocalPlayer.ApplyRandomPenalty();
        }

        private void OnJump()
        {
            if (isComplete) return;

            UpdateMission();
        }

        private void OnSpin(bool isSpin)
        {
            if (isComplete) return;

            isProgressing = isSpin;

            if (isSpin) return;

            progress = 0f;
            missionUI.UpdateMission(progress);
        }

        private void OnPickup(PickupType type)
        {
            switch (type)
            {
                case PickupType.Apple when currentType == MissionType.EatApple:
                case PickupType.Mushroom when currentType == MissionType.EatMushroom:
                    UpdateMission();
                    break;
            }
        }

        private void OnStayRock(bool isStay)
        {
            if (isComplete) return;

            isProgressing = isStay;

            if (isStay) return;

            progress = 0;
            missionUI.UpdateMission(progress);
        }

        private void FixedUpdate()
        {
            if (isComplete) return;
            if (!isProgressing) return;

            progress += Time.deltaTime;
            missionUI.UpdateMission(progress);

            if (progress >= targetValue)
            {
                isComplete = true;
                isProgressing = false;
            }
        }
    }
}