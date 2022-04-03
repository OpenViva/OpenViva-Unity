using UnityEngine;
using System.Collections;
using viva;


public class Chicken: VivaScript{

    private readonly Character chicken;
    private Task idleTask = null;
    private float confuseTimer = 0;
    private float faceLookTimer = 0;
    public bool friendly = false;
    private Task runAwayTask;
    private float wallProx = 1.5f;
    private RaycastHit hitInfo;
    private float dropHeightMin = 0.5f;
    private float proxMin = 0.2f;
    private float alertTimer = 0f;
    private readonly float alertDuration = 2f;
    private float cluckTimer = 0;


    public Chicken( Character _character ){
        chicken = _character;
        SetupAnimations();

        chicken.mainAnimationLayer.player.Play( this, "stand", "idle");
        chicken.characterDetector.radius = 3.5f;
        chicken.characterDetector.onCharacterNearby.AddListener( this, OnCharacterNearby );
        chicken.animal.onCollisionEnter.AddListener( this, OnCollisionEnter );

        Sound.PreloadSet("chicken");    //load all 'Content/Sound/chicken' folder sounds

        SetupOnGrabbed();

        Achievement.Add( "Catch a chicken", "Wild chickens can be found roaming the land. Try using the hammer to build a wooden trap.","chicken");
        Achievement.Add( "Get eggs", "Captured chickens drop eggs when you pet them","egg");
    }

    public void LoadState( object[] data ){
        if( data == null || data.Length < 1 ) return;
        SetFriendly( (bool)data[0] );
    }

    public void SetFriendly( bool _friendly ){
        if( friendly == _friendly ) return;

        friendly = _friendly;
        if( friendly ){
            chicken.model.SetTexture( "chicken", "_BaseColorMap",  "chicken_tamed" );
        }else{
            chicken.model.SetTexture( "chicken", "_BaseColorMap", "Chicken_LOD0/chicken_BaseColorMap" );
        }
        Save( "LoadState", new object[]{ true } );
    }

    private void OnCollisionEnter( string source, Collision collision ){

        var item = Util.GetItem( collision.rigidbody );
        if( item && item.HasAttribute("knife") && !chicken.destroyed ){
            Item.Spawn( "chicken leg", item.rigidBody.worldCenterOfMass+Vector3.right*0.01f, item.rigidBody.rotation );
            Item.Spawn( "chicken leg", item.rigidBody.worldCenterOfMass-Vector3.right*0.01f, item.rigidBody.rotation );

            ParticleSystemManager.CreateParticleSystem( "feather poof", item.rigidBody.worldCenterOfMass );

            Sound.Create( item.rigidBody.worldCenterOfMass ).Play( Sound.Load( "chicken", "kill" ) );

            Viva.Destroy( chicken );
            return;
        }

        //spawn an egg when you pet the chicken
        if( friendly ){
            var character = Util.GetCharacter( collision.rigidbody );
            if( !character || character.isAnimal ) return;
            if( source == "CHICKEN_ Pelvis" ){
                LayEgg();
            }
        }
        if( collision.relativeVelocity.sqrMagnitude > 40f ){
            KnockOut();
        }
    }

    private void KnockOut(){
        if( chicken.autonomy.FindTask("knocked out") != null ) return;

        var knockedOut = new Task( chicken.autonomy );
        chicken.animal.pinLimit.Add( "knocked out", 0f );
        chicken.animal.muscleLimit.Add( "knocked out", 0f );
        // chicken.mainAnimationLayer.player.Stop();

        knockedOut.onSuccess += delegate{
            Debug.LogError("SUCCESS");
            chicken.animal.pinLimit.Remove( "knocked out" );
            chicken.animal.muscleLimit.Remove( "knocked out" );
            chicken.mainAnimationLayer.player.Play( this, "stand", "idle" );
        };

        var awakeTestTimer = new Timer( chicken.autonomy, 5f );
        awakeTestTimer.onSuccess += delegate{
            if( chicken.animal.isBeingGrabbed ){
                awakeTestTimer.Reset();
            }else{
                knockedOut.Succeed();
            }
        };

        knockedOut.AddPassive( awakeTestTimer );

        knockedOut.Start( this, "knocked out" );
        Sound.Create( chicken.ragdoll.movementBody.worldCenterOfMass ).Play( "chicken","startle" );
    }

    private void LayEgg(){
        
        if( chicken.isBeingGrabbed ) return;
        if( chicken.autonomy.FindTask("knocked out") != null ) return;

        var layEggAnim = new PlayAnimation( chicken.autonomy, null, "lay egg", true, 0.8f );
        layEggAnim.onEnterAnimation += delegate{
            Sound.Create( chicken.ragdoll.movementBody.worldCenterOfMass ).Play( "chicken", "cluck" );
        };
        layEggAnim.onSuccess += delegate{
            var egg = Item.Spawn( "egg", chicken.ragdoll.movementBody.worldCenterOfMass, Quaternion.identity );
            if( egg ){
                Sound.Create( chicken.ragdoll.movementBody.worldCenterOfMass ).Play( "generic", "egg", "egg_pop.wav" );
                egg.SetIgnorePhysics( chicken.ragdoll, true );
                AchievementManager.main.CompleteAchievement( "Get eggs", true );
            }
        };
        layEggAnim.Start( this, "egg spawn" );
    }

    private void SetupOnGrabbed(){
        foreach( var muscle in chicken.ragdoll.muscles ){
            foreach( var grabbable in muscle.grabbables ){
                grabbable.onGrabbed.AddListener( this, OnGrabbed );
                grabbable.onReleased.AddListener( this, OnReleased );
            }
        }
    }

    private void OnGrabbed( GrabContext grabContext ){
        chicken.ragdoll.pinLimit.Add( "on grabbed", 0f );
        chicken.ragdoll.muscleLimit.Add( "on grabbed", 0f );
        SetFriendly( true );
        Sound.Create( chicken.ragdoll.movementBody.worldCenterOfMass ).Play( "chicken","startle" );

        AchievementManager.main.CompleteAchievement( "Catch a chicken", true );
    }

    private void OnReleased( GrabContext grabContext ){
        if( !chicken.ragdoll.isBeingGrabbed ){
            if( chicken.autonomy.FindTask("knocked out") == null ){
                chicken.ragdoll.pinLimit.Remove( "on grabbed" );
                chicken.ragdoll.muscleLimit.Remove( "on grabbed" );
            }

            BeginRunAway();
            chicken.locomotionForward.SetPosition(1f);
        }
    }

    private void OnCharacterNearby( Character otherCharacter ){
        if( friendly ) return;
        if( otherCharacter.isAnimal ) return;
        BeginRunAway();
    }

    private void BeginRunAway(){
        
        alertTimer = alertDuration;

        if( runAwayTask != null ){
            chicken.autonomy.RemoveTask( runAwayTask );
            runAwayTask.Succeed();
        }
        runAwayTask = new Task( chicken.autonomy );
        runAwayTask.onFixedUpdate += FixedUpdateRunAway;

        runAwayTask.Start( this, "run away" );
    }

    private void FixedUpdateRunAway(){
        Character closestTarget = null;
        float minSqDist = chicken.characterDetector.radius*chicken.characterDetector.radius;
        foreach( Character candidate in chicken.characterDetector.nearbyCharacters ){
            if( candidate.isAnimal || friendly ) continue;
            var sqDist = Vector3.SqrMagnitude( candidate.ragdoll.movementBody.worldCenterOfMass-chicken.ragdoll.movementBody.worldCenterOfMass );
            if( sqDist < minSqDist ){
                minSqDist = sqDist;
                closestTarget = candidate;
            }
        }
        chicken.locomotionForward.SetPosition( Mathf.LerpUnclamped( chicken.locomotionForward.position, alertTimer/alertDuration, Time.fixedDeltaTime*4f ) );
        if( closestTarget == null ){
            alertTimer = Mathf.Max( 0, alertTimer-Time.deltaTime );
            if( alertTimer <= 0 ){
                runAwayTask.Succeed();
                chicken.locomotionForward.SetPosition(0f);
            }
        }

        var forward = chicken.model.armature.forward;
        var currDeg = Mathf.Atan2( forward.x, forward.z )*Mathf.Rad2Deg;
        float targetDeg = currDeg;
        var collisionDeg = FindCollisionDirection();
        if( collisionDeg.HasValue ){
            targetDeg = collisionDeg.Value;
        }else{
            var avoidDeg = FindAvoidDirection();
            if( avoidDeg.HasValue ){
                targetDeg = avoidDeg.Value;
            }
        }
        
        float deltaDeg = Mathf.Clamp( Mathf.DeltaAngle( currDeg, targetDeg ), -7, 7 );
        chicken.model.armature.rotation *= Quaternion.Euler( 0, deltaDeg, 0 );

        cluckTimer -= Time.deltaTime*alertTimer/alertDuration;
        if( cluckTimer < 0 ){
            cluckTimer = 0.3f+Random.value*0.6f;
            var handle = Sound.Create( chicken.ragdoll.movementBody.worldCenterOfMass );
            if( Random.value < 0.01f ){
                handle.Play( "chicken","startle" );
            }else{
                handle.Play( "chicken","cluck" );
            }
        }
    }

    private float? FindAvoidDirection(){
        //avoid spheres around avoidTransforms
        float sphereSumWeight = 0.0f;
        Vector3 sphereAvoidSum = Vector3.zero;
        foreach( var candidate in chicken.characterDetector.nearbyCharacters ){
            if( candidate.isAnimal ) continue;
            Vector3 diff = candidate.ragdoll.movementBody.worldCenterOfMass-chicken.ragdoll.movementBody.worldCenterOfMass;
            float dist = diff.magnitude;
            if( dist < chicken.characterDetector.radius ){
                
                diff.y = 0.0f;
                sphereAvoidSum += -diff.normalized;
                sphereSumWeight += chicken.characterDetector.radius-dist;
            }
        }
        if( sphereSumWeight > 0.0f ){
            Vector3 avgSphereAvoidNormal = sphereAvoidSum/sphereSumWeight;
            return Mathf.Atan2( avgSphereAvoidNormal.x, avgSphereAvoidNormal.z )*Mathf.Rad2Deg;
        }
        return null;
    }

    private float? FindCollisionDirection(){

        //avoid walls
        Vector3? fromTerrainNormal = null;
        var body = chicken.ragdoll.movementBody;
        for( int i=0; i<2; i++ ){   //0,1
            int side = i*2-1;   //-1,1
            Vector3 viewPos = body.worldCenterOfMass;
            Vector3 viewDir = ( chicken.model.armature.forward+chicken.model.armature.right*side*0.5f ).normalized;
            Debug.DrawLine( viewPos, viewPos+viewDir*wallProx, Color.green, Time.fixedDeltaTime );
            if( SampleWalls( viewPos, viewDir ) ){
                if( !fromTerrainNormal.HasValue ){
                    fromTerrainNormal = -viewDir;
                }else{
                    fromTerrainNormal = ( fromTerrainNormal-viewDir )/2.0f;
                }
            }
        }

        //refine from terrain normal
        if( fromTerrainNormal.HasValue ){
            int subHits = 1;    //count the first raycast
            float seekVariation = 0.5f+UnityEngine.Random.value;
            float avgDistance = 0.0f; 
            for( int i=0; i<8; i++ ){
                float side = (float)i/4.0f-1;   //-1.0f ~ 1.0f
                Vector3 viewDir = ( chicken.model.armature.forward+chicken.model.armature.right*side*seekVariation+chicken.model.armature.up*(UnityEngine.Random.value-0.5f)*0.5f ).normalized;
                Debug.DrawLine( body.worldCenterOfMass, body.worldCenterOfMass+viewDir*wallProx, Color.yellow, Time.fixedDeltaTime );
                if( SampleWalls( body.worldCenterOfMass, viewDir ) ){
                    fromTerrainNormal += hitInfo.normal;
                    subHits++;
                    avgDistance += hitInfo.distance;
                }
            }
            fromTerrainNormal /= subHits;
            avgDistance /= subHits;

            //reflect off of wall
            if( fromTerrainNormal.Value.x != 0.0f || fromTerrainNormal.Value.z != 0.0f ){

                Vector3 wallReflection = Vector3.Reflect( chicken.model.armature.forward, fromTerrainNormal.Value );
                // wallProx = Mathf.Min( Mathf.Max( proxMin, avgDistance ), wallProx );

                // Debug.DrawLine( transform.position+Vector3.up*0.6f, transform.position+Vector3.up*0.6f+wallReflection, Color.cyan, 2.5f );
                float reflectedDeg = Mathf.Atan2( wallReflection.x, wallReflection.z )*Mathf.Rad2Deg;
                return reflectedDeg;
            }
        }
        return null;
    }
    
    private bool SampleWalls( Vector3 pos, Vector3 dir ){
        //avoid walls
        if( Physics.Raycast( pos, dir, out hitInfo, wallProx, WorldUtil.defaultMask|WorldUtil.itemsStaticMask ) ){
            return true;
        }
        //avoid cliffs
        Debug.DrawLine( pos+dir*wallProx, pos+dir*wallProx+Vector3.down*dropHeightMin, Color.red, Time.fixedDeltaTime );
        if( !Physics.Raycast( pos+dir*wallProx, Vector3.down, out hitInfo, dropHeightMin, WorldUtil.defaultMask|WorldUtil.itemsStaticMask ) ){
            hitInfo.normal = -dir;
            hitInfo.distance = wallProx*0.9f;
            Debug.DrawLine( pos+dir*wallProx, pos+dir*wallProx+Vector3.up*0.2f, Color.blue, Time.fixedDeltaTime );
            return true;
        }
        return false;
    }

    private void SetupAnimations(){

        var idle = new AnimationSingle( viva.Animation.Load("chicken_idle"), chicken, true );
        var walk = new AnimationSingle( viva.Animation.Load("chicken_walk"), chicken, true, 1.2f );
        var run = new AnimationSingle( viva.Animation.Load("chicken_run"), chicken, true, 1.2f );

        var stand = chicken.animationSet.GetBodySet("stand");

        var lay = stand.Single( "lay egg", "chicken_lay", true );

        var locomotion = stand.Mixer(
            "idle",
            new AnimationNode[]{
                idle, walk, run
            },
            new Weight[]{
                chicken.GetWeight("idle"),
                chicken.GetWeight("walking"),
                chicken.GetWeight("running"),
            },
            false
        );

        lay.nextState = locomotion;

        chicken.GetWeight("idle").value = 1.0f;
        chicken.GetWeight("walking").value = 0.0f;
        chicken.GetWeight("running").value = 0.0f;
    }
}