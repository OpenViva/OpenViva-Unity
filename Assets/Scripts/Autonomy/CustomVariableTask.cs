using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public delegate bool CustomVariableBoolReturnFunc( CustomVariable variable );

/// <summary>
/// Ensures that a customVariable is a specific value.
/// </summary>
public class CustomVariableCondition : Task {

	public readonly VivaInstance instance;
	public readonly CustomVariable variable;
	public CustomVariableBoolReturnFunc onCondition;

	/// <summary>
	/// Ensures that a customVariable is a specific value. Use onCondition to check the success/fail of the condition.
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="source">The script who is calling this (Used to keep track of where this task originated from).</param>
	/// <param name="_instance">The object whose variable will be checked.</param>
	/// <param name="variableName">The name of the variable.</param>
	public CustomVariableCondition( Autonomy _autonomy, VivaScript source, VivaInstance _instance, string variableName ):base(_autonomy){

		if( _instance == null ) throw new System.Exception("CustomVariable instance cannot be null");
		if( variableName == null ) throw new System.Exception("CustomVariable _variableName cannot be null");
		name = "custom variable condition";
		instance = _instance;
		variable = instance.customVariables.Get( source, variableName );

		onFixedUpdate += Verify;
	}

	private void Verify(){
		if( instance == null ){
			Fail("Instance was deleted");
			return;
		}
		if( onCondition == null ){
			Fail("onCondition was not set");
			return;
		}
		var completeState = onCondition.Invoke( variable );
		if( completeState ){
			Succeed();
		}else{
			Fail("variable \""+variable.name+"\" condition failed");
		}
	}

	public override bool OnRequirementValidate(){
		Verify();
		return base.OnRequirementValidate();
	}
}

}