using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


/// <summary>
/// Runs a timer in seconds. Then the task succeeds.
/// </summary>
public class Timer : Task {

	public float timeLeft = 0.0f;
	public float timeToWait = 0;

	/// <summary>
	/// Tasks the character to play a BodySet animation. BodySet and/or Animation name must be specified.
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="_timeToWait">The time to wait in seconds.</param>
	public Timer( Autonomy _autonomy, float _timeToWait ):base(_autonomy){
		name = "timer";
		timeToWait = _timeToWait;

		onReset += delegate{
			timeLeft = 0;
			onFixedUpdate += CheckTimer;
		};
		Reset();
	}

	private void CheckTimer(){
		timeLeft += Time.deltaTime;
		if( timeLeft >= timeToWait ){
			timeLeft = 0;
			onFixedUpdate -= CheckTimer;
			Succeed();
		}
	}
}

}