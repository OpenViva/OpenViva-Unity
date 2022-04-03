using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{

/// <summary>
/// Base class for all Autonomy tasks.
public class Condition: Task{

    public BoolReturnFunc condition;

	/// <summary>
	/// Task that remains in a success as long as the target instance object isn't destroyed
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="_condition">The condition to check. Returns true if success or false if failure. Exception will be thrown if it is null.</param>
    public Condition( Autonomy _autonomy, BoolReturnFunc _condition ):base(_autonomy){
        if( _condition == null ) throw new System.Exception("Cannot make a condition with a null condition parameter");
        condition = _condition;
        name = "condition";

        onFixedUpdate += CheckCondition;
    }

    public void CheckCondition(){
        if( condition.Invoke() ){
            Succeed();
        }else{
            Fail();
        }
    }

    public override bool OnRequirementValidate(){
        return condition.Invoke();
    }
}

}