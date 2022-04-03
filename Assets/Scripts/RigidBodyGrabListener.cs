using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace viva{


public class RigidBodyGrabListener: MonoBehaviour {

    public delegate void GrabCallback( GrabContext grabContext );

    public int grabConnections { get; private set; }
    public GrabCallback onGrab;
    public GrabCallback onDrop;
    

    private void Awake(){
        onGrab += delegate{
            grabConnections++;
        };
        onDrop += delegate{
            grabConnections--;
        };
    }
}

}