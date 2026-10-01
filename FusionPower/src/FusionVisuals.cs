using UnityEngine;

namespace Baiye.FusionPower
{
    public sealed class FusionVisuals : KMonoBehaviour
    {
        public enum Machine { Separator, Breeder, Reactor, TripleAlpha }

        public Machine machine;

        private Storage leftStorage;
        private Storage rightStorage;
        private Storage rightAuxStorage;
        private float leftCapacity;
        private float rightCapacity;
        private MeterController leftMeter;
        private MeterController rightMeter;
        private KBatchedAnimController anim;
        private Operational operational;
        private bool? lastAnimatedActive;
        private FusionProcessBase process;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            anim = GetComponent<KBatchedAnimController>();
            operational = GetComponent<Operational>();
            process = GetComponent<FusionProcessBase>();
            Storage[] stores = GetComponents<Storage>();
            switch (machine)
            {
                case Machine.Separator:
                    leftStorage = stores[0];
                    rightStorage = stores[3];
                    leftCapacity = rightCapacity = 20f;
                    break;
                case Machine.Breeder:
                    leftStorage = stores[0];
                    rightStorage = stores[2];
                    leftCapacity = 50f;
                    rightCapacity = 5f;
                    break;
                case Machine.Reactor:
                    leftStorage = stores[0];
                    rightStorage = stores[1];
                    leftCapacity = 2f;
                    rightCapacity = 10f;
                    break;
                case Machine.TripleAlpha:
                    leftStorage = stores[0];
                    rightStorage = stores[1];
                    rightAuxStorage = stores[4];
                    leftCapacity = 10f;
                    rightCapacity = 70f;
                    break;
            }
            if (anim != null)
            {
                leftMeter = new MeterController(anim, "meter_left_target", "meter_left",
                    Meter.Offset.Infront, Grid.SceneLayer.NoLayer, "meter_left_target");
                rightMeter = new MeterController(anim, "meter_right_target", "meter_right",
                    Meter.Offset.Infront, Grid.SceneLayer.NoLayer, "meter_right_target");
                leftMeter.interpolateFunction = MeterController.StandardLerp;
                rightMeter.interpolateFunction = MeterController.StandardLerp;
            }
            Debug.Log("[BaiyeFusionPower] Visuals spawned for " + machine + " with " + stores.Length
                + " storages and two KAnim meters");
        }

        private void Update()
        {
            if (leftMeter != null && leftStorage != null)
                leftMeter.SetPositionPercent(Mathf.Clamp01(leftStorage.ExactMassStored() / leftCapacity));
            if (rightMeter != null && rightStorage != null)
                rightMeter.SetPositionPercent(Mathf.Clamp01((rightStorage.ExactMassStored()
                    + (rightAuxStorage != null ? rightAuxStorage.ExactMassStored() : 0f)) / rightCapacity));

            if (anim == null || operational == null) return;
            bool active = operational.IsOperational && process != null
                && GameClock.Instance.GetTime() - process.LastWorkTime <= 0.4f;
            if (lastAnimatedActive == active) return;
            if (active)
            {
                anim.Play("working_pre", KAnim.PlayMode.Once);
                anim.Queue("working_loop", KAnim.PlayMode.Loop);
            }
            else if (lastAnimatedActive == true)
            {
                anim.Play("working_pst", KAnim.PlayMode.Once);
                anim.Queue("off", KAnim.PlayMode.Loop);
            }
            else
                anim.Play("off", KAnim.PlayMode.Loop);
            lastAnimatedActive = active;
            Debug.Log("[BaiyeFusionPower] " + machine + " animated active=" + active);
        }

        protected override void OnCleanUp()
        {
            leftMeter?.Unlink();
            rightMeter?.Unlink();
            base.OnCleanUp();
        }
    }
}
