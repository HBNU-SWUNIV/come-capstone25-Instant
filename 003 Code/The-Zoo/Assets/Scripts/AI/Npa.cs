using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace AI
{
    public class Npa : Agent
    {
        private AgentTransform agent;
        private RayPerceptionSensorComponent3D raySensor;

        private readonly float delay = 30f;
        private readonly float minStopTime = 1;
        private readonly float maxStopTime = 10;
        private bool isStop = true;

        public override void Initialize()
        {
            base.Initialize();

            agent = GetComponent<AgentTransform>();
            raySensor = GetComponent<RayPerceptionSensorComponent3D>();

            StartCoroutine(StopCo());
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (!agent) return;

            sensor.AddObservation(transform.position);
            sensor.AddObservation(transform.up.normalized);
            sensor.AddObservation(transform.forward.normalized);
            sensor.AddObservation(agent.moveInput);
            sensor.AddObservation(agent.lookInput);
            sensor.AddObservation(agent.rBody.linearVelocity.normalized);
            sensor.AddObservation(agent.rBody.linearVelocity.magnitude);
            sensor.AddObservation(agent.CanMove);
            sensor.AddObservation(agent.CanJump);
            sensor.AddObservation(agent.SpinHold);
            sensor.AddObservation(agent.isSpin);
            sensor.AddObservation(agent.isRun);

            sensor.AddObservation(GetSeekerViewDot());
            sensor.AddObservation(agent.suspicion / AgentTransform.SuspicionThreshold);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (agent.isDead.Value) return;

            var dAction = actions.DiscreteActions;

            agent.LookAction(dAction[2]);
            agent.JumpAction(dAction[3]);
            agent.SpinAction(dAction[4]);
            agent.RunAction(dAction[5]);
            agent.AttackAction(dAction[6]);

            if (isStop) return;

            agent.MoveAction(dAction[0], dAction[1]);
        }

        private void IsSeekerFind(out Transform tr)
        {
            tr = null;

            if (raySensor == null) return;

            var observations = raySensor.RaySensor.RayPerceptionOutput;

            if (observations.RayOutputs == null) return;

            foreach (var sub in observations.RayOutputs)
                if (sub.HitTagIndex == 0)
                {
                    tr = sub.HitGameObject.transform;
                    return;
                }
        }

        private float GetSeekerViewDot()
        {
            IsSeekerFind(out var seekerTr);
            if (!seekerTr) return -1f;

            var toHider = (transform.position - seekerTr.position).normalized;
            var seekerForward = seekerTr.forward;
            return Vector3.Dot(seekerForward, toHider); // 1에 가까울수록 정면
        }

        private IEnumerator StopCo()
        {
            var first = Random.Range(minStopTime, maxStopTime);
            yield return new WaitForSeconds(first);

            isStop = false;

            while (agent)
            {
                var time1 = Random.Range(delay - 20, delay + 10);
                yield return new WaitForSeconds(time1);

                isStop = true;

                if (isStop)
                {
                    agent.moveInput = Vector2.zero;
                    agent.animator.OnMove(false);
                    agent.animator.OnRun(false);
                    agent.animator.OnSpin(false);
                }

                var time2 = Random.Range(minStopTime, maxStopTime);
                yield return new WaitForSeconds(time2);

                isStop = false;
            }
        }
    }
}