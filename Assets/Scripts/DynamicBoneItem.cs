using UnityEngine;

namespace viva
{


    public class DynamicBoneItem : Item
    {

        private DynamicBone m_dynamicBone = null;
        public DynamicBone dynamicBone { get { return m_dynamicBone; } }
        private DynamicBoneGrab grabber = null;
        public float lastTargetIndex = 0.0f;
        public float timeWashed = 0.0f;


        public void InitializeDynamicBoneItem(DynamicBone _dynamicBone)
        {
            m_dynamicBone = _dynamicBone;
        }

        public override void OnItemLateUpdatePostIK()
        {
            PlayerHandState playerHandState = mainOccupyState as PlayerHandState;
            if (grabber)
            {
                float lastTargetIndex = grabber.GetCurrentTargetIndex();
                grabber.RecalculateGrabIndex();
                if (grabber && playerHandState && playerHandState.HasAttribute(HandState.Attribute.SOAPY))
                {

                    float targetIndexChange = Mathf.Abs(lastTargetIndex - grabber.GetCurrentTargetIndex());
                    lastTargetIndex = grabber.GetCurrentTargetIndex();
                    timeWashed += targetIndexChange;

                    Loli loli = mainOwner as Loli;
                    if (loli)
                    {
                        loli.IncreaseDirt(-targetIndexChange * Time.deltaTime);
                        if (timeWashed > 10.0f)
                        {
                            loli.active.bathing.OnCompleteHairBoneWash(grabber);
                        }
                    }
                }
            }
        }

        public override void OnPreDrop()
        {
            if (grabber)
            {
                grabber.StopGrabbing();
                grabber = null;
            }
        }

        public override void OnPostPickup()
        {
            HandState handState = mainOccupyState as HandState;
            if (handState == null)
            {
                return;
            }
            if (grabber == null)
            {
                grabber = transform.gameObject.AddComponent(typeof(DynamicBoneGrab)) as DynamicBoneGrab;
            }
            grabber.BeginGrabbing(handState.fingerAnimator.targetBone, this);
        }
    }

}