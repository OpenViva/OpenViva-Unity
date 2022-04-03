using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{

/// <summary>
/// Base class for all Autonomy tasks.
public class Task{

    public VivaScript _internalSource;
	/// <summary>The name of the task (Optional).</summary>
    public string name;
	/// <summary>The autonomy of the parent character.</summary>
    public readonly Autonomy autonomy;
	/// <summary>A shorthand form of retrieving the parent character from within the Task.</summary>
    public Character self { get{ return autonomy.self; } }
	/// <summary>Callback delegate fired when the task fails.</summary>
    public GenericCallback onFail;
	/// <summary>Callback delegate fired when the task succeeds.</summary>
    public GenericCallback onSuccess;
	/// <summary>Callback delegate fired when the task is removed from the execution queue and is not finished.</summary>
    public GenericCallback onInterrupted;
    public GenericCallback onAutonomyEnter;
    public GenericCallback onAutonomyExit;
    private readonly List<Task> requirements = new List<Task>();
    private readonly List<Task> passives = new List<Task>();
    private float? failTime = null;
    private string failReason = null;
    private float? successTime = null;
	/// <summary>Boolean state if the Task has failed.</summary>
    public bool failed { get{ return failTime.HasValue; } }
	/// <summary>Boolean state if the Task has succeeded.</summary>
    public bool succeeded { get{ return successTime.HasValue; } }
	/// <summary>Boolean state if the Task has failed or succeeded.</summary>
    public bool finished { get{ return failTime.HasValue || successTime.HasValue; } }
	/// <summary>Boolean state if the Task is currently active in an Autonomy tree.</summary>
    public bool registered { get; private set; } = false;
    public Task passiveParent { get; private set; } = null;
    public Task requirementParent { get; private set; } = null;
	/// <summary>Returns the number of passives added to this Task.</summary>
    public int passiveCount { get{ return passives.Count; } }
	/// <summary>Returns the number of requirements added to this Task.</summary>
    public int requirementCount { get{ return requirements.Count; } }
    private bool? validated = null;
    public Task branchInProgress { get; private set; } = null;
    private Task parentBranch = null;
    public bool inAutonomy { get; private set; } = false;
    public int _internalReqInsertOffset = 0;
    public int priority { get; private set; } = 0;
    public int id { get; private set; }
    public bool constant { get; private set; }
    public List<string> tags { get; private set; } = new List<string>();

    private static int idCounter;


    public static Task _InternalAutonomyCreateListener( Autonomy autonomy ){
        var listener = new Task( autonomy );
        listener.inAutonomy = true;
        listener._internalSource = VivaScript._internalDefault;
        listener._internalSource._internalScript = Script._internalDefault;
        listener.name = "Autonomy listener";
        return listener;
    }

	/// <summary>
	/// The base class for all autonomy tasks. 
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
    public Task( Autonomy _autonomy ){
        if( _autonomy == null ){
            throw new System.Exception("Cannot create Task with null autonomy");
        }
        autonomy = _autonomy;
        id = idCounter++;
        
        onInterrupted += delegate{
            foreach( var requirement in requirements ){
                requirement.onInterrupted.Invoke();
            }
            foreach( var passive in passives ){
                passive.onInterrupted.Invoke();
            }
        };
        onAutonomyEnter += delegate{
            if( _internalSource == null ){
                if( requirementParent != null ){
                    _internalSource = requirementParent._internalSource;
                }else if( passiveParent != null ){
                    _internalSource = passiveParent._internalSource;
                }
            }
            foreach( var requirement in requirements ){
                requirement.onAutonomyEnter.Invoke();
            }
            foreach( var passive in passives ){
                passive.onAutonomyEnter.Invoke();
            }
            inAutonomy = true;
        };
        onAutonomyExit += delegate{
            foreach( var requirement in requirements ){
                requirement.onAutonomyExit.Invoke();
            }
            foreach( var passive in passives ){
                passive.onAutonomyExit.Invoke();
            }
            inAutonomy = false;
        };
    }

    /// <summary>
	/// Start a task.
	/// </summary>
	/// <param name="source">Who added the task.</param>
	/// <param name="taskName">The name to give to this task. Cannot be null.</param>
	/// <param name="_priority">The priority to know where to insert into the queue. Higher values are placed at the front.</param>
    /// <returns>If it was successfully started. False if a task named that way already exists, or a missing variable/name was found.<c>null</c></returns>
	public bool Start( VivaScript source, string taskName, int _priority=0 ){
        if( inAutonomy ) return false;
		if( source == null ){
			Debugger.LogError("Cannot add task without a source (Used to track who added the task)");
			return false;
		}
		_internalSource = source;
		if( registered ){
			Debugger.Log("\""+name+"\" is already queued");
			return false;
		}
		if( taskName == null ){
			Debugger.LogError("Cannot queue a task with a null name");
			return false;
		}

        constant = false;
		name = taskName;
        priority = _priority;
        autonomy._InternalAddTask( this );
        return true;
	}

    /// <summary>
	/// Start a task as a constant. Will always run every frame (Like a passive)
	/// </summary>
	/// <param name="source">Who added the task.</param>
	/// <param name="taskName">The name to give to this task. Cannot be null.</param>
	public void StartConstant( VivaScript source, string taskName ){
		if( source == null ){
			Debugger.LogError("Cannot add task without a source (Used to track who added the task)");
			return;
		}
		_internalSource = source;
		if( registered ){
			Debugger.LogWarning("\""+name+"\" is already queued");
			return;
		}
		if( taskName == null ){
			Debugger.LogError("Cannot queue a task with a null name");
			return;
		}

        constant = true;
		name = taskName;    
        autonomy._InternalAddConstantTask( this );
	}

    public void Reset(){
        failTime = null;
        successTime = null;
        validated = null;
        failReason = null;
        Script.HandleScriptCall( _internalSource, onReset, "onReset" );
    }

/// \cond
    public void FixedUpdate(){ if( registered ) Script.HandleScriptCall( _internalSource, onFixedUpdate, "onFixedUpdate" ); }   //double delegate += might add the sub delegates not the delegate itself, big oof
    public void Update(){ Script.HandleScriptCall( _internalSource, onUpdate, "onUpdate" ); }
    public void LateUpdate(){ Script.HandleScriptCall( _internalSource, onLateUpdate, "onLateUpdate" ); }
    public void Gesture( string gesture, Character caller ){ Script.HandleScriptCall( _internalSource, onGesture, gesture, caller, "onGesture"); }
    public void ModifyAnimation(){ Script.HandleScriptCall( _internalSource, onModifyAnimation, "onModifyAnimation" ); }
    public void AnimationChange( int animationLayerIndex ){ Script.HandleScriptCall( _internalSource, onAnimationChange, animationLayerIndex, "onAnimationChange" ); }

    public void _InternalClearAllCallbacks(){
        onFixedUpdate = null;
        onUpdate = null;
        onLateUpdate = null;
        onModifyAnimation = null;
        onAnimationChange = null;
    }
    
    public void _InternalUnregisterInProgress(){
        if( branchInProgress != null ){
            var oldBranch = branchInProgress;
            branchInProgress = null;
            oldBranch._InternalUnregisterInProgress();    //percolate up an unregistry of branches
            autonomy._InternalUnregisterTask( oldBranch );
        }
    }
    public void _InternalRegisterInProgress( Task task ){
        if( task != null && task.inAutonomy && !task.registered && task != branchInProgress ){
            _InternalUnregisterInProgress();
            autonomy._InternalRegisterTask( task );
            branchInProgress = task;
            task.parentBranch = this;
        }
    }
    public string GetTaskSubType(){
        if( requirementParent != null ) return "requirement";
        if( passiveParent != null ) return "passive";
        return constant ? "constant" : "task";
    }
    public string GetTaskFullName(){
        string s = "["+id+"] "+name;
        if( requirementParent != null ) s = "["+requirementParent.id+"] "+requirementParent.name+" -> "+s;
        if( passiveParent != null ) s = "["+passiveParent.id+"] "+passiveParent.name+" -> "+s;
        return s;
    }
    public void _InternalFireOnRegistered(){
        if( registered ) return;
        registered = true;

        RegisterAllPassives();

        successTime = null;
        failTime = null;
        Script.HandleScriptCall( _internalSource, onRegistered, "onRegistered" );
    }
    public void _InternalFireOnUnregistered(){
        if( !registered ) return;
        registered = false;

        if( parentBranch != null ){
            parentBranch._InternalUnregisterInProgress();
            parentBranch = null;
        }


        UnregisterAllPassives();
        
        Script.HandleScriptCall( _internalSource, onUnregistered, "onUnregistered" );
    }
    public void FailOnStopGesture( string returnToBodySet=null, string returnToAnimation=null ){
        onGesture += delegate( string gesture, Character caller ){
            if( gesture == "stop" ){
                Fail("Asked to stop");
                if( returnToBodySet!=null || returnToAnimation!=null ){
                    var returnToIdle = new PlayAnimation( autonomy, returnToBodySet, returnToAnimation, true, 0, false );
                    returnToIdle.Start( VivaScript._internalDefault, "return to idle" );
                }
            }
        };
    }
    public bool? ValidateHierarchy( Task branch ){
        
        for( int i=0; i<requirementCount; i++ ){
            var reqTask = requirements[i];
            bool? subReqState = reqTask.ValidateHierarchy( branch );
            if( subReqState.HasValue ){
                if( !subReqState.Value ){
                    //requirement failed therefore fail parent
                    Fail("requirement \""+reqTask.name+"\" failed");
                    return Validate();	//percolate fail
                }
            }else{
                return null;	//requirement was registered
            }
        }
        
        var completeState = Validate();
        if( completeState.HasValue ){
            return completeState.Value;
        }
        branch._InternalRegisterInProgress( this );

        for( int i=0; i<passiveCount; i++ ){
            var passiveTask = passives[i];
            passiveTask.ValidateHierarchy( passiveTask );
        }

        return null;
    }
/// \endcond
    private void RegisterAllPassives(){
        foreach( Task passive in passives ) autonomy._InternalRegisterTask( passive );
    }
    private void UnregisterAllPassives(){
        foreach( Task passive in passives ) autonomy._InternalUnregisterTask( passive );
    }

	/// <summary>Callback delegate fired during Unity's FixedUpdate().</summary>
    public GenericCallback onFixedUpdate;
	/// <summary>Callback delegate fired during Unity's Update().</summary>
    public GenericCallback onUpdate;
	/// <summary>Callback delegate fired during Unity's LateUpdate().</summary>
    public GenericCallback onLateUpdate;
	/// <summary>Callback delegate fired when the parent character model's animationPlayer animates.</summary>
    public GenericCallback onModifyAnimation;
	/// <summary>Callback delegate fired when the parent character model's animationPlayer changes AnimationState. Passes the layer of the player that changed.</summary>
    public IntReturnFunc onAnimationChange;
	/// <summary>Callback delegate fired when the Task is registered in an Autonomy tree.</summary>
    public GenericCallback onRegistered;
	/// <summary>Callback delegate fired when the Task is unregistered in an Autonomy tree.</summary>
    public GenericCallback onUnregistered;
	/// <summary>Callback delegate fired when the Task is reset (Success or Fail is cleared).</summary>
    public GenericCallback onReset;
	/// <summary>Callback delegate fired when the Task is reset (Success or Fail is cleared).</summary>
    public GestureCallback onGesture;

	/// <summary>Adds a Task into the Task's passive tree. Passives will always run if the parent Task is registered.</summary>
    public void AddPassive( Task passive ){
        if( passive.registered || passive.passiveParent != null || passive.requirementParent != null || passive.registered || passive == this ){
            Debugger.LogError(passive.name+": already registered");
            return;
        }
        passives.Add( passive );
        passive.passiveParent = this;
        if( inAutonomy ) passive.onAutonomyEnter.Invoke();
        if( registered ) autonomy._InternalRegisterTask( passive );
    }
    
	/// <summary>Removes a Task into the Task's passive tree. If the passive Task is active it will be unregistered.</summary>
    public void RemovePassive( Task passive ){
        if( passive.passiveParent == null ){
            Debugger.LogError(passive.name+": Task not a passive");
            return;
        }
        int index = passives.IndexOf( passive );
        if( index > -1 ){
            passive.passiveParent = null;
            passives.RemoveAt( index );
            
            autonomy._InternalUnregisterTask( passive );
            if( inAutonomy ) passive.onAutonomyExit.Invoke();
        }else{
            Debugger.LogError(passive.name+": Passive not found");
        }
    }

	/// <summary>Removes all added passives and requirements. If the Tasks are active they will be unregistered.</summary>
    public void RemoveAllPassivesAndRequirements(){
        for( int i=passiveCount; i-->0; ){
            RemovePassive( passives[i] );
        }
        for( int i=requirementCount; i-->0; ){
            RemoveRequirement( requirements[i] );
        }
    }

	/// <summary>Adds a Task into the Task's requirement tree. Requirements must always be in a success state before the parent Task can run.</summary>
    public void AddRequirement( Task requirement ){
        InsertRequirement( requirement, Mathf.Max(0,requirementCount-_internalReqInsertOffset) );
    }

    public void AddRequirementAfter( Task requirement, Task existingRequirement ){
        var index = requirements.IndexOf( existingRequirement );
        if( index == -1 ){
            Debugger.LogError("Could not find existing requirement for AddRequirementAfter");
            return;
        }
        InsertRequirement( requirement, index+1 );
    }

	/// <summary>Inserts a Task at the beginning of the Task's requirement tree.</summary>
    public void PrependRequirement( Task requirement ){
        InsertRequirement( requirement, 0 );
    }

    private void InsertRequirement( Task requirement, int index ){
        if( requirement == null || requirement.passiveParent != null || requirement.requirementParent != null || requirement.registered || requirement == this ){
            Debugger.LogError(requirement?.name+": Task not eligible for requirement");
            return;
        }
        if( passiveParent != null ){
            // Debugger.LogError("Cannot add "+requirement.name+" because passive tasks cannot have requirements!");
        }
        requirement.requirementParent = this;
        requirements.Insert( index, requirement );

        if( inAutonomy ) requirement.onAutonomyEnter.Invoke();
    }

	/// <summary>Removes a Task into the Task's requirement tree. If the requirement Task is active it will be unregistered.</summary>
    public void RemoveRequirement( Task requirement ){
        if( requirement == null){
            Debugger.LogError("Cannot remove null Task requirement");
            return;
        }
        if( requirement.requirementParent == null ){
            Debugger.LogError(requirement?.name+": Task not a requirement");
            return;
        }
        int index = requirements.IndexOf( requirement );
        if( index > -1 ){
            requirements.RemoveAt( index );
            requirement.requirementParent = null;

            autonomy._InternalUnregisterTask( requirement );
            if( inAutonomy ) requirement.onAutonomyExit.Invoke();
        }else{
            Debugger.LogError(requirement.name+": Requirement not found");
        }
    }

	/// <summary>Gets the passive Task at index in the Task's passive tree.</summary>
    public Task GetPassive( int index ){
        return passives[ index ];
    }
    
	/// <summary>Gets the requirement Task at index in the Task's passive tree.</summary>
    public Task GetRequirement( int index ){
        return requirements[ index ];
    }
	/// <summary>Marks the Task for failure. The onFail callback will run in the next Autonomy tick.</summary>
    public void Fail( string reason="" ){
        successTime = null;
        failTime = Time.time;
        failReason = reason;
    }

	/// <summary>Marks the Task for success. The onSuccess callback will run in the next Autonomy tick.</summary>
    public void Succeed(){
        successTime = Time.time;
        failTime = null;
        failReason = null;
    }

    private void _InternalFail(){
        Script.HandleScriptCall( _internalSource, onFail, "onFail" );
        if( passiveParent == null ){
            Debugger.LogError("FAILED \""+GetTaskFullName()+"\": "+failReason);
        }
    }
    private void _InternalSuccess(){
        Script.HandleScriptCall( _internalSource, onSuccess, "onSuccess" );
        if( passiveParent == null ){
            Debugger.Log("SUCCEEDED \""+GetTaskFullName()+"\"");
        }
    }
    private bool? Validate(){
        if( finished ){
            if( passiveParent != null ){
                if( failed ){
                    if( validated != false ){
                        validated = false;
                        _InternalFail();
                    }
                    return failed;  //fail state may change in _InternalFail
                }else if( succeeded ){
                    if( validated != true ){
                        validated = true;
                        _InternalSuccess();
                    }
                    return succeeded;  //success state may change in _InternalFail
                }
            }else{  //requirement or task
                if( !validated.HasValue ){
                    if( failed ){
                        validated = false;
                        _InternalFail();
                    }else{	//else succeeded
                        validated = true;
                        _InternalSuccess();
                    }
                    return validated;
                }else if( !validated.Value ){
                    return false;   //locked into fail
                }else{
                    //check if success is stil valid
                    if( !OnRequirementValidate() ){    //otherwise restart the logic
                        successTime = null;
                        validated = null;
                        return null;
                    }else{
                        return true;
                    }
                }
            }
        }
        return null;
    }
	/// <summary>Used to constantly check the success/Fail state of a finished requirement task.
    /// If they fall from a success state or if they failed, the Task will be registered for execution in the next Autonomy tick until it succeeds again, thereby acting as a requirement.</summary>
	/// <returns>true: The Task is still in a success state. false: The Task has fallen from its success state.</returns>
    public virtual bool OnRequirementValidate(){
        return succeeded;
    }
}

}