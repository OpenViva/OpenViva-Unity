using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SpatialTracking;


namespace viva{

public class PlayerHeadState : HeadState
{
    [SerializeField]
    private TrackedPoseDriver m_behaviourPose;
    public TrackedPoseDriver behaviourPose { get{ return m_behaviourPose; } }
}

}
