using System.Collections;
using UnityEngine;
using viva;


public class OnsenReceptionDesk: VivaScript{

    private Item item;
    private Vector3 workerPos;
    private Vector3 bellPos;
    private Vector3 towelRoomPos;
    private Character worker;
    private Character client;


    public OnsenReceptionDesk( Item _item ){
        item = _item;
        var workerPosGameObject = item.model.FindChildModel("worker_pos");
        var bellPosGameObject = item.model.FindChildModel("bell_pos");
        var towel_roomGameObject = GameObject.Find("towel_room");
        if( workerPosGameObject == null || bellPosGameObject == null || towel_roomGameObject == null ) return;

        workerPos = workerPosGameObject.rootTransform.position;
        bellPos = bellPosGameObject.rootTransform.position;
        towelRoomPos = towel_roomGameObject.transform.position;

        item.onTriggerEnterRigidBody.AddListener( this, RingBell );
    }

    private void RingBell( Rigidbody rigidBody ){
        if( rigidBody.gameObject.layer != WorldUtil.characterCollisionsLayer ) return;

        if( rigidBody.velocity.sqrMagnitude > 0.1f && Vector3.Dot( rigidBody.velocity, Vector3.up ) < 0 ){
            SpeechBubble.Create( bellPos+Random.onUnitSphere*0.05f+Vector3.up*0.05f, 0.4f ).Display( BuiltInAssetManager.main.FindSprite("exclamation") );
            Sound.Create( bellPos ).Play( "generic","onsen","reception_bell.wav" );

            var character = Util.GetCharacter( rigidBody );
            if( character ) AttendClient( character );
        }
    }

    private void SetupAnimations( Character character ){
		var stand = character.animationSet.GetBodySet("stand");

		var standBow = stand.Single( "bow", "body_stand_bow", false, 1.2f );
        standBow.nextState = stand["idle"];

        standBow.curves[BipedRagdoll.headID] = new Curve( new CurveKey[]{
			new CurveKey(0.2f,1),new CurveKey(0.5f,0),new CurveKey(0.8f,1)
		});
    }

    public DialogOption[] GetDialogOptions(){
        return new DialogOption[]{
            new DialogOption("clerk",DialogOptionType.GENERIC),
        };
    }

    public void OnDialogOption( Character character, DialogOption option ){
        if( option.value == "clerk" ) EmployAsClerk( new object[]{ character } );
    }

    private void EmployAsClerk( object[] objs ){
        if( objs == null || objs.Length != 1 ) return;
        var character = objs[0] as Character;
        Save( "EmployAsClerk", new object[]{ character } );
        worker = character;
        if( character == null ) return;

        SetupAnimations( character );

        CreateGoToWorkerPos().Start( this, "onsen clerk" );
    }

    private MoveTo CreateGoToWorkerPos(){
        var moveTo = new MoveTo( worker.autonomy, 0, 0.5f );
        moveTo.target.SetTargetPosition( workerPos );
        return moveTo;
    }

    private void ResetEmployee(){
        client = null; 
    }

    private void AttendClient( Character newClient ){
        if( !newClient || !worker || client ) return;
        
        client = newClient;

        var faceTarget = new FaceTargetBody( worker.autonomy, 1, 20, Mathf.Epsilon );
        faceTarget.target.SetTargetCharacter( client );

        var moveTo = CreateGoToWorkerPos();
        faceTarget.AddRequirement( moveTo );

        var bow = new PlayAnimation( worker.autonomy, "stand", "bow", true, 0.7f );
        bow.AddRequirement( faceTarget );
        bow.Start( this, "bow to customer" );

        bow.onEnterAnimation += delegate{
            if( worker ){
                SpeechBubble.Create( worker.biped.head.rigidBody ).Display( BuiltInAssetManager.main.FindSprite("wait") );
            }
        };

        bow.onSuccess += EnsureTowelForClient;
        bow.onFail += ResetEmployee;
    }

    private void EnsureTowelForClient(){
        if( !client || !worker ){
            ResetEmployee();
            return;
        }
        var towels = worker.biped.rightHandGrabber.FindItems( "towel" );
        if( towels.Count == 0 ){
            towels = worker.biped.leftHandGrabber.FindItems( "towel" );
        }
        if( towels.Count == 0 ){
            GetTowel();
        }else{
            GiveTowel( towels[0] );
        }
    }

    private void GiveTowel( Item towel ){
        if( !client || !worker ){
            ResetEmployee();
            return;
        }

        var goToClient = new MoveTo( worker.autonomy, 1f, 0.5f );
        goToClient.target.SetTargetCharacter( client );

        goToClient.onFail += ResetEmployee;
        goToClient.onSuccess += delegate{
            worker.scriptManager.CallOnScript("give","GiveItem", new object[]{ client } );
        };
        goToClient.Start(this,"go to client give towel");
    }

    private void GetTowel(){

        var towel = Item.Spawn( "towel", towelRoomPos, Quaternion.identity );
        if( towel ){
            if( !client || !worker || !towel ){
                ResetEmployee();
                return;
            }//else
            var pickupTowel = new Pickup( worker.autonomy, towel, Pickup.DropCurrentIfNecessary );
            pickupTowel.onSuccess += EnsureTowelForClient;
            pickupTowel.onFail += ResetEmployee;

            pickupTowel.Start( this, "get towel" );
        }
    }
} 