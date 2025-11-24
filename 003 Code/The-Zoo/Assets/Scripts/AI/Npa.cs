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

        public override void Initialize()
        {
            base.Initialize();

            agent = GetComponent<AgentTransform>();
            raySensor = GetComponent<RayPerceptionSensorComponent3D>();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            if (!agent) return;

            sensor.AddObservation(transform.up.normalized);
            sensor.AddObservation(transform.forward.normalized);
            sensor.AddObservation(transform.right.normalized);
            sensor.AddObservation(agent.moveInput);
            sensor.AddObservation(agent.lookInput);
            sensor.AddObservation(agent.CanMove);
            sensor.AddObservation(agent.CanJump);
            sensor.AddObservation(agent.SpinHold);
            sensor.AddObservation(agent.isSpin);
            sensor.AddObservation(agent.isRun);

            sensor.AddObservation(GetSeekerViewDot());
            sensor.AddObservation(agent.suspicion / AgentTransform.maxSuspicion);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            if (agent.isDead.Value) return;

            var dAction = actions.DiscreteActions;

            agent.MoveAction(dAction[0], dAction[1]);
            agent.LookAction(dAction[2]);
            agent.JumpAction(dAction[3]);
            agent.SpinAction(dAction[4]);
            agent.RunAction(dAction[5]);
            agent.AttackAction(dAction[6]);
        }

        private void IsSeekerFind(out Transform tr)
        {
            tr = null;

            if (!raySensor) return;

            var observations = raySensor.RaySensor.RayPerceptionOutput;

            if (observations.RayOutputs == null) return;

            foreach (var sub in observations.RayOutputs)
                if (sub.HitTaggedObject && sub.HitGameObject.CompareTag("Seeker"))
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
    }
}