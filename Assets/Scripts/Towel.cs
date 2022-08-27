using UnityEngine;

namespace viva
{


    public class Towel : Item
    {

        [SerializeField]
        private DynamicBone towelDynamicBone;
        [SerializeField]
        private SkinnedMeshRenderer towelSMR;

        public TowelClip lastWallClip { get; private set; }


        public override void OnPostPickup()
        {

            GameDirector.dynamicBones.Add(towelDynamicBone);
            towelDynamicBone.FreezeAllParticles(1.0f);

            if (lastWallClip != null)
            {
                lastWallClip.ClearActiveTowel();
            }
            rigidBody.isKinematic = false;
        }

        protected override void OnUnregisterItemLogic()
        {
            GameDirector.dynamicBones.Remove(towelDynamicBone);
        }

        public override void OnItemFixedUpdate()
        {
            var pos = GamePhysics.getRaycastPos(transform.position, Vector3.down, 1.0f, WorldUtil.wallsMask, QueryTriggerInteraction.Ignore);
            if (!pos.HasValue)
            {
                pos = transform.position + Vector3.down;
            }
            float height = (transform.position - pos.Value).y;
            if (rigidBody.isKinematic)
            {
                height = 1.0f;
            }
            towelSMR.SetBlendShapeWeight(0, Mathf.Clamp01(1.0f - height) * 100.0f);
        }

        public override void OnPreDrop()
        {
            rigidBody.isKinematic = false;

            //find nearest TowelClip
            Collider[] objects = Physics.OverlapSphere(transform.position, 0.2f, WorldUtil.regionMask);
            foreach (var obj in objects)
            {
                TowelClip wallClip = obj.GetComponent<TowelClip>();
                if (wallClip && wallClip.activeTowel == null)
                {
                    wallClip.RackTowel(this);
                    return;
                }
            }
        }

        public void SetLastWallClip(TowelClip towelClip)
        {
            lastWallClip = towelClip;
            rigidBody.isKinematic = true;
        }
    }

}