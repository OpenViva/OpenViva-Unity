using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;


namespace viva{


public delegate Task TaskReturnFunc();
public delegate void TaskCallback( Task task );

/// <summary>
/// Ensures that a customVariable is a specific value.
/// </summary>
public class EnumTaskManager<T> where T: struct, IComparable, IConvertible, IFormattable {

	private Task lastTask = null;
	private readonly TaskReturnFunc[] taskGenerators;
	public T current { get; private set; }

	/// <summary>
	/// Ensures that a customVariable is a specific value. Use onCondition to check the success/fail of the condition.
	/// </summary>
	public EnumTaskManager(){
		
		if( !typeof(T).IsEnum ) throw new System.Exception("EnumTaskManager generic type must be an enum");
		var values = Enum.GetValues( typeof(T) );

		taskGenerators = new TaskReturnFunc[ values.Length ];
	}

	public TaskReturnFunc this[ T enumVal ]{
		set{
			int index = Convert.ToInt32( enumVal );
			taskGenerators[ index ] = value;
		}
	}

	public void SwitchTo( T enumVal ){
		int newIndex = Convert.ToInt32( enumVal );
		var taskGenerator = taskGenerators[ newIndex ];
		if( taskGenerator == null ){
			return;
		}

		int oldIndex = Convert.ToInt32( current );
		var newTask = taskGenerator();
		if( newTask == null ){
			Debugger.LogError("Task returned was null for EnumTaskManager. Skipping request to change to "+enumVal);
			return;
		}else{
			//detect if switched to yet another branch during generation
			if( Convert.ToInt32( current ) != oldIndex ){
				return;
			}
		}

		current = enumVal;
		if( lastTask != null ) lastTask.autonomy.RemoveTask( lastTask );
		
		newTask.Start( VivaScript._internalDefault, enumVal.ToString()+":"+newTask.name );
		lastTask = newTask;
	}
}

}