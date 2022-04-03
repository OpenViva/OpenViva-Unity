using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class Onsen: VivaScript{

    private Transform navMeshTransform;
    private Vector3[] navMeshTriangles = null;

	public Onsen( Item item ){

        var navMeshObject = GameObject.Find("onsen_nav_mesh");
        if( navMeshObject ){
            var mf = navMeshObject.GetComponent<MeshFilter>();
            if( mf ){
                navMeshTransform = mf.transform;
                navMeshTriangles = mf.sharedMesh.vertices;
            }
        }
        Achievement.Add( "Make a character relax", "Select a character and command to the sign","onsen sign" );
	}

    private Vector3? GetRandomWaterFloorPoint(){
		if( !navMeshTransform ){
			Debug.LogError("Could not find onsen nav mesh transform");
			return null;
		}
        int triangles = navMeshTriangles.Length/3;
        int index = Random.Range( 0, triangles )*3;

        Vector3 a = navMeshTriangles[ index++ ];
        Vector3 b = navMeshTriangles[ index++ ];
        Vector3 c = navMeshTriangles[ index ];

        Vector3 d = a+(b-a)*Random.value;
        return navMeshTransform.TransformPoint( d+(c-d)*Random.value );
    }

	public DialogOption[] GetDialogOptions(){
		if( !navMeshTransform ) return null;
        return new DialogOption[]{
            new DialogOption("swim",DialogOptionType.GENERIC,"swim_onsen"),
        };
    }

    public void OnDialogOption( Character character, DialogOption option ){
        if( option.value == "swim" ) BeginOnsen( new object[]{ character } );
    }

	public void StopTaskIfGestured( Task task ){
	}

	private void BeginOnsen( object[] parameters ){
		if( parameters == null || parameters.Length != 1 ) return;
		var character = parameters[0] as Character;
		if( !character ) return;

		var poolTarget = GetRandomWaterFloorPoint();
		if( !poolTarget.HasValue ) return;
		poolTarget = poolTarget.Value;
		Tools.DrawCross( poolTarget.Value, Color.green );
		

		SetupAnimations( character );

		var goToPool = new MoveTo( character.autonomy, 0, 6 );
		goToPool.SetNearbyTargetBodySet( "squat", 3f );
		goToPool.target.SetTargetPosition( poolTarget );
		goToPool.onSuccess += delegate{
			SwimAroundUntilWall( character );
		};
        goToPool.FailOnStopGesture( "stand", "idle" );

		goToPool.Start( this, "go to pool" );
	}

	private void SwimAroundUntilWall( Character character ){
		var swimAroundPerpetual = new Task( character.autonomy );
		swimAroundPerpetual.onFixedUpdate += delegate{
			CheckHitWall( character, swimAroundPerpetual );
			character?.locomotionForward.SetPosition(1f);
		};

		var swimInSquat = new PlayAnimation( character.autonomy, "squat", "idle", false, 0 );
		swimAroundPerpetual.AddRequirement( swimInSquat );
        swimAroundPerpetual.FailOnStopGesture( "stand", "idle" );

		swimAroundPerpetual.Start( this, "swim around" );
	}

	private void CheckHitWall( Character character, Task parentTask ){
		
		int hits = 0;
		Vector3 wallNorm = Vector3.zero;
		Vector3 wallPos = Vector3.zero;
		for( int i=-1; i<=1; i++ ){
			Vector3 source = character.biped.lowerSpine.rigidBody.worldCenterOfMass;
			Vector3 dir = character.biped.upperSpine.target.forward+character.biped.upperSpine.target.right*i*0.1f;
			dir.y = 0f;
			Debug.DrawLine( source, source+dir.normalized, Color.green, 0.1f );
			if( Physics.Raycast( source, dir, out RaycastHit hitInfo, 1.0f, WorldUtil.defaultMask, QueryTriggerInteraction.Ignore ) ){
				hits++;
				wallNorm += hitInfo.normal;
				wallPos += hitInfo.point;
			}
		}
		if( hits == 3 ){
			wallNorm /= 3;
			wallPos /= 3;
			parentTask.Succeed();

			var moveToRelax = new MoveTo( character.autonomy );
			moveToRelax.SetNearbyTargetBodySet( "squat", 3f );
			moveToRelax.target.SetTargetPosition( wallPos+wallNorm*0.5f );

			moveToRelax.onSuccess += delegate{
				
				AchievementManager.main.CompleteAchievement( "Make a character relax", true );

				var relax = new PlayAnimation( character.autonomy, "relax", "idle", false, -1 );

				Tools.DrawCross( wallPos+wallNorm*2f, Color.red, 0.4f, 4f );

				var faceRelax = new FaceTargetBody( character.autonomy );
				faceRelax.target.SetTargetPosition( wallPos+wallNorm*2f );

				var onlyInWater = new Condition( character.autonomy, delegate{ return character.onWater.active; });

				relax.AddRequirement( onlyInWater );
				
				relax.AddRequirement( faceRelax );

				relax.onFail += delegate{
					var returnToIdle = new PlayAnimation( character.autonomy, "stand", "idle", false, -1 );
					returnToIdle.Start( this, "return to idle" );
				};

				relax.Start( this, "relax" );
			};
			
			moveToRelax.Start( this, "move to relax" );
		}
	}

	private void RelaxNextToSpot( Character character ){
		var relax = new PlayAnimation( character.autonomy, "relax", "idle", false, -1 );
		relax.Start( this, "relax" );
	}

	private void SetupAnimations( Character character ){

		var stand = character.animationSet.GetBodySet("stand");
		var squat = character.animationSet.GetBodySet("squat");
		var relax = character.animationSet.GetBodySet("relax");

		var squat_idle_loop = new AnimationSingle( viva.Animation.Load("squat_idle_loop"), character, true );
		var squat_forward_loop = new AnimationSingle( viva.Animation.Load("squat_forward_loop"), character, true );

		var squatLocomotion = squat.Mixer( "idle", new AnimationNode[]{
				squat_idle_loop,
				squat_forward_loop,
				squat_forward_loop
			},
			new Weight[]{
				character.GetWeight("idle"),
				character.GetWeight("walking"),
				character.GetWeight("running"),
			},
			true
		);

		var stand_to_squat = new AnimationSingle( viva.Animation.Load("stand_to_squat"), character, false );
		stand_to_squat.nextState = squatLocomotion;
		stand.transitions[ squat ] = stand_to_squat;

		var squat_to_stand = new AnimationSingle( viva.Animation.Load("squat_to_stand"), character, false );
		squat_to_stand.nextState = stand["idle"];
		squat.transitions[ stand ] = squat_to_stand;

		
		var relax_idle_loop = relax.Single( "idle", "relax_idle_loop", true );
		relax_idle_loop.curves[BipedRagdoll.headID] = new Curve(0);

		var squat_to_relax = new AnimationSingle( viva.Animation.Load("squat_to_relax"), character, false );
		squat_to_relax.nextState = relax["idle"];
		squat.transitions[ relax ] = squat_to_relax;

		var relax_to_squat = new AnimationSingle( viva.Animation.Load("relax_to_squat"), character, false );
		relax_to_squat.nextState = squat["idle"];
		relax_to_squat.curves[BipedRagdoll.headID] = new Curve(0);
		relax.transitions[ squat ] = relax_to_squat;
	}
}  