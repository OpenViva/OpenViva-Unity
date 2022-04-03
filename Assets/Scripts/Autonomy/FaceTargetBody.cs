using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


/// <summary>
/// Handles the targeting of a point in space. You can target a transform to constantly target its position without having to update the point constantly. If the transform is lost, the target will automatically revert to a null value.
/// </summary>
public class Target{

	public enum TargetType{
		WORLD_POSITION,
		TRANSFORM,
		CHARACTER,
		RIGIDBODY
	}

	private Vector3? lastReadPos = null;
    public object target { get; private set; }
	public TargetType type { get; private set; } = TargetType.WORLD_POSITION;
	public ListenerGeneric onChanged = new ListenerGeneric( "onChanged" );
	private bool changed;

	public Target(){
	}

	public void _InternalReset(){
		onChanged._InternalReset();
	}

	public void _InternalHandleChange(){
		if( changed ){
			changed = false;
			onChanged.Invoke();
		}
	}

	/// <summary>Assigns a point in world space</summary>
	/// <param name="pos">The point in world space. You can pass null to disable the entire TaskTarget.</param>
	public void SetTargetPosition( Vector3? pos ){
		type = TargetType.WORLD_POSITION;
		lastReadPos = pos;
		changed = true;
	}
	
	/// <summary>Assigns the target as a Character. It will read their floor position.</summary>
	/// <param name="character">The character to target. You can pass null to disable the entire TaskTarget.</param>
	public void SetTargetCharacter( Character character ){
		type = TargetType.CHARACTER;
		target = character;
		changed = true;
	}
	
	/// <summary>Assigns the target as a Rigidbody. It will read the worldcentermass position.</summary>
	/// <param name="rigidBody">The rigidBody to target. You can pass null to disable the entire TaskTarget.</param>
	public void SetTargetRigidBody( Rigidbody rigidBody ){
		type = TargetType.RIGIDBODY;
		target = rigidBody;
		changed = true;
	}
	
	/// <summary>Assigns a transform to track its world position.</summary>
	/// <param name="newTargetTransform">The new target transform. You can pass null to disable the entire TaskTarget.</param>
	public void SetTargetTransform( Transform newTargetTransform ){
		type = TargetType.TRANSFORM;
        target = newTargetTransform;
		changed = true;
	}

	/// <summary>Reads the current value of whatever it is tracking.</summary>
	/// <returns>null: There is nothing being tracked. Vector3: a point in world space.</returns>
    public Vector3? Read(){
        switch( type ){
        case TargetType.WORLD_POSITION:
            return lastReadPos;
        case TargetType.TRANSFORM:
			var targetTransform = target as Transform;
            if( targetTransform ){
                lastReadPos = targetTransform.position;
            }else{
				lastReadPos = null;
            }
            return lastReadPos;
        case TargetType.RIGIDBODY:
			var targetRigidBody = target as Rigidbody;
            if( targetRigidBody ){
                lastReadPos = targetRigidBody.worldCenterOfMass;
            }else{
				lastReadPos = null;
            }
            return lastReadPos;
		case TargetType.CHARACTER:
			var targetCharacter = target as Character;
			if( targetCharacter != null ){
				if( targetCharacter.isBiped ){
					var hipsCenter = targetCharacter.biped.hips.rigidBody.worldCenterOfMass;
					lastReadPos = new Vector3( hipsCenter.x, targetCharacter.biped.head.rigidBody.worldCenterOfMass.y, hipsCenter.z );
				}else{
					lastReadPos = targetCharacter.ragdoll.root.rigidBody.worldCenterOfMass;
				}
			}else{
				lastReadPos = null;
			}
			return lastReadPos;
        default:
            return null;
        }
    }
}


/// <summary>Rotates the character model's animation armature after it is animated so it faces a point in space. When added as a requirement, the character's body must face the target before performing the next Task.
/// When added as a passive, the character's body will constantly try to face a target.</summary>
public class FaceTargetBody : Task {

	public float minSuccessBearing;
	public float durationRequired;
	public readonly float rotateDegreeRate = 250.0f;
	/// <summary>The target to rotate towards</summary>
	public readonly Target target = new Target();

	private float duration = 0.0f;
	private float easeDuration;
	private float ease = 0;
	private Vector3 rootDirEuler = Vector3.zero;
	public Target pivot = null;

	/// <summary>
	/// Tasks the character to turn their body to face a particular target.
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="speedMultiplier">The speed multiplier of the character's rotation as it changes to face the target.</param>
	/// <param name="_minSuccessBearing">The minimum bearing in degrees that considers the character successfully facing the target.</param>
	/// <param name="_durationRequired">The minimum time required for the character to face the target before succeeding.</param>
	/// <param name="_pivot">Optional pivot to force rotation around a point (only if nearby).</param>
    /// <example>
    /// The following makes the character face the player's feet position.
    /// <code>
	/// var facePlayer = new FaceTargetBody( character.autonomy );
	/// facePlayer.target.SetTargetRagdoll( VivaPlayer.user.character.ragdoll );
    /// </code>
    /// </example>
	public FaceTargetBody( Autonomy _autonomy, float speedMultiplier=1.0f, float _minSuccessBearing = 20.0f, float _durationRequired=0.35f, Target _pivot=null ):base(_autonomy){
		speedMultiplier = Mathf.Max( 0.05f, speedMultiplier );
		easeDuration = 0.5f/speedMultiplier;
		rootDirEuler.y = self.model.armature.eulerAngles.y;
		minSuccessBearing = _minSuccessBearing;
		durationRequired = _durationRequired;
		pivot = _pivot;

		onModifyAnimation += RotateToTarget;
		onRegistered += delegate{
			duration = 0.0f;
			ease = 0;
		};
	}

	private void RotateToTarget(){
		
        var pos = target.Read();
		if( !pos.HasValue ){
			Fail();
            return;
		}
		//do not allow rotating to target if character is not on the floor
		if( !self.ragdoll.surface.HasValue ){
			ease = 0f;
			return;
		}
		
		Vector3 diff = pos.Value -self.ragdoll.surface.Value;
		diff.y = 0.0f;
		Vector3 readPos = self.ragdoll.surface.Value+diff.normalized;
		
		StayOnPivot();

		if( Mathf.Abs( Tools.Bearing( self.model.armature, readPos ) ) <= minSuccessBearing ){
		    //must face target direction for a specified duration
			if( duration >= durationRequired ){
                Succeed();
				ease = 0f;
				return;
            }
		}
		ease += Time.deltaTime;
		float alpha = Tools.EaseInOutQuad( Mathf.Clamp01( ease/easeDuration ) );

		Debug.DrawLine( self.ragdoll.surface.Value, readPos, Color.green, 0.1f );
		Debug.DrawLine( self.model.armature.position, self.model.armature.position+self.model.armature.forward, Color.red, 0.1f );
		float bearing = Tools.Bearing( self.model.armature, readPos );

        float absBearing = Mathf.Abs( bearing );
		float rotateDegrees = rotateDegreeRate*Time.fixedDeltaTime*alpha*Mathf.Sign( bearing );
        if( absBearing < minSuccessBearing ){
            duration += Time.deltaTime;
        }else{
            duration = 0.0f;
        }
		if( absBearing < 30 ){
			rotateDegrees *= 1f-Mathf.Pow( 1f-absBearing/30f, 4 );	//slow down as approaching target
		}

		if( bearing > 0.0f ){
			if( bearing+rotateDegrees <= 0.0f ){
				rotateDegrees = bearing;
			}
		}else if( bearing+rotateDegrees >= 0.0f ){
			rotateDegrees = bearing;
		}
		self.ragdoll.transform.RotateAround( self.model.armature.position, Vector3.up, rotateDegrees );
	}

	private void StayOnPivot(){
		if( pivot == null ) return;

		var pivotPos = pivot.Read();
		if( !pivotPos.HasValue ) return;
		if( !self.ragdoll.surface.HasValue ) return;

		if( !MoveTo.IsCloseToNode( self.ragdoll.surface.Value, pivotPos.Value, MoveTo.minNodeDist ) ) return;
		var toPivot = pivotPos.Value-self.ragdoll.surface.Value;
		toPivot.y = 0;
		var dist = toPivot.magnitude;
		if( dist < Mathf.Epsilon ) return;

		float strength = dist/MoveTo.minNodeDist;
		strength = ( 0.5f-Mathf.Abs( 0.5f-strength ) )*2;
		toPivot /= dist;
		self.ragdoll.root.rigidBody.AddForce( toPivot*strength, ForceMode.VelocityChange );
	}

	public override bool OnRequirementValidate(){
		var pos = target.Read();
		if( !pos.HasValue ) return false;
		//do not allow rotating to target if character is not on the floor
		if( !self.ragdoll.surface.HasValue ) return false;
		
		Vector3 diff = pos.Value -self.ragdoll.surface.Value;
		diff.y = 0.0f;
		Vector3 readPos = self.ragdoll.surface.Value+diff.normalized;

		StayOnPivot();

		if( Mathf.Abs( Tools.Bearing( self.model.armature, readPos ) ) <= minSuccessBearing ){
		    return true;
		}
		return false;
	}
}

}