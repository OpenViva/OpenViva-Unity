using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{

/// <summary>
/// The MonoBehaviour class for handling character logic. Task objects can be added to perform complex sequences of logic trees.
public class Autonomy: MonoBehaviour{

	private Character m_self;
	public Character self { get{ return m_self; } set{ m_self = m_self==null?value:m_self; } }
	private readonly List<Task> queue = new List<Task>();
	private readonly List<Task> constant = new List<Task>();
	public ListenerGeneric onFixedUpdate = new ListenerGeneric( "OnFixedUpdate" );
	public ListenerGeneric onUpdate = new ListenerGeneric( "OnUpdate" );
	public ListenerGeneric onLateUpdate = new ListenerGeneric( "OnLateUpdate" );
	private Task root;
	private List<Task> toAdd = new List<Task>();
	public readonly ListenerTask onTaskRegistered = new ListenerTask( "onTaskRegistered" );
	public Task current { get{ return queue.Count>0 ? queue[0] : null; } }


	public bool HasTag( string tag ){
		if( current == null ){
			return tag=="idle" && !self.mainAnimationLayer.player.isTransitioning;
		}
		return current.tags.Contains(tag);
	}

	public void _InternalAddTask( Task task ){
		var alreadyExists = FindTask( task.name );
		if( alreadyExists != null ){
			Debugger.LogWarning("Task with the name \""+task.name+"\" already exists. Pick a different name.");
			return;
		}
		toAdd.Add( task );
	}

	public void _InternalAddConstantTask( Task task ){
		var alreadyExists = FindTask( task.name );
		if( alreadyExists != null ){
			Debugger.LogWarning("Task with the name \""+task.name+"\" already exists. Pick a different name.");
			return;
		}
		constant.Add( task );
		task.onAutonomyEnter.Invoke();
	}

	public Task FindTask( string taskName ){
		foreach( var queuedTask in queue ){
			if( queuedTask.name == taskName ) return queuedTask;
		}
		foreach( var queuedTask in toAdd ){
			if( queuedTask.name == taskName ) return queuedTask;
		}
		foreach( var queuedTask in constant ){
			if( queuedTask.name == taskName ) return queuedTask;
		}
		return null;
	}

	public void _InternalReset(){
		var safeCopy = constant.ToArray();
		foreach( var constantTask in safeCopy ) constantTask._InternalUnregisterInProgress();
		constant.Clear();

		queue.Clear();
		onTaskRegistered._InternalReset();
		root?._InternalUnregisterInProgress();
		if( root == null ){
			root = new Task( this );
			root.name = "Autonomy root";
		}
		toAdd.Clear();
	}

	/// <summary>
	/// Remove a task from the queue.false onRemovedFromQueue will fire on that task.
	/// </summary>
	/// <param name="task">The task to remove. Cannot be null.</param>
	public void RemoveTask( Task task ){
		if( task == null ){
			Debugger.LogError("Cannot remove a null task");
			return;
		}
		toAdd.Remove( task );
		var index = queue.IndexOf( task );
		if( index != -1 ){ 
			queue.RemoveAt( index );
			root._InternalUnregisterInProgress();
			task.onAutonomyExit?.Invoke();
		}else{
			index = constant.IndexOf( task );
			if( index != -1 ){
				constant.RemoveAt( index );
				task._InternalUnregisterInProgress();
				task.onAutonomyExit?.Invoke();
			}
		}
	}

	public bool RemoveTask( string name ){
		var task = FindTask( name );
		if( task != null ){
			RemoveTask( task );
			return true;
		}
		return false;
	}

	private void Validate(){
		if( constant.Count > 0 ){
			var safeCopy = constant.ToArray();
			foreach( var constantTask in safeCopy ){
				constantTask.ValidateHierarchy( constantTask );
			}
		}
		if( queue.Count > 0 ){
			
			var task = queue[0];
			var completeState = task.ValidateHierarchy( root );
			if( completeState.HasValue ){
				//remove from queue
				queue.Remove( task );
				root._InternalUnregisterInProgress();
				task.onAutonomyExit.Invoke();
			}
		}
		//all task additions are added after hierarchy validation to prevent skipping success/failure branches
		if( toAdd.Count > 0 ){
			foreach( var newTask in toAdd ){
				Debugger.Log("\""+newTask.GetTaskFullName()+"\" was added to logic tree");
				queue.Insert( 0, newTask );
				newTask.onAutonomyEnter.Invoke();
			}
			queue.Sort( (a,b)=>b.priority.CompareTo(a.priority) );
			toAdd.Clear();
		}
	}

	private void FixedUpdate(){
		onFixedUpdate?.Invoke();
		Validate();
	}

	private void Update(){
		onUpdate?.Invoke();
	}

	private void LateUpdate(){
		onLateUpdate?.Invoke();
	}

	public void _InternalUnregisterTask( Task task ){
		if( task == null || !task.registered ){
			return;
		}

        Debugger.Log("- "+task.GetTaskSubType()+" \""+task.GetTaskFullName()+"\"");

		foreach( var animationLayer in self.animationLayers ) animationLayer.player.onAnimationChange -= task.AnimationChange;
		onFixedUpdate._InternalRemoveListener( task.FixedUpdate );
		onUpdate._InternalRemoveListener( task.Update );
		onLateUpdate._InternalRemoveListener( task.LateUpdate );
		self.mainAnimationLayer.player.onModifyAnimation -= task.ModifyAnimation;
		self.onGesture._InternalRemoveListener( task.Gesture );
		task._InternalFireOnUnregistered();

		if( !task.finished ) task.onInterrupted?.Invoke();
	}

	public void _InternalRegisterTask( Task task ){
		if( task == null || task.registered ){
			return;
		}
        Debugger.Log("+ "+task.GetTaskSubType()+" \""+task.GetTaskFullName()+"\"");

		foreach( var animationLayer in self.animationLayers ) animationLayer.player.onAnimationChange += task.AnimationChange;
		onFixedUpdate._InternalAddListener( task.FixedUpdate );
		onUpdate._InternalAddListener( task.Update );
		onLateUpdate._InternalAddListener( task.LateUpdate );
		self.mainAnimationLayer.player.onModifyAnimation += task.ModifyAnimation;
		self.onGesture._InternalAddListener( task.Gesture );
		task._InternalFireOnRegistered();
		onTaskRegistered.Invoke( task );
	}
}

}