using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public partial class AutonomyWait : Autonomy.Task {

	private float duration;
	private float timeStarted = Mathf.Infinity;
	public bool loop = false;
	
    public AutonomyWait( Autonomy _autonomy, string _name, float _duration ):base(_autonomy,_name){
		duration = _duration;

		onRegistered += delegate{ timeStarted = Time.time; };
    }

	public override bool? Progress(){
		if( Time.time-timeStarted > duration ){
			if( loop ){
				timeStarted = Time.time;
				Reset();
			}
			return true;
		}
		return null;
	}

}

}