using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace viva{


public class RigidBodyPickupEvent: MonoBehaviour {
    public UnityEvent onPickup;
    public UnityEvent onDrop;
}

}