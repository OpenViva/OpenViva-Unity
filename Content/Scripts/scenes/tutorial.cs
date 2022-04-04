using System.Collections;
using UnityEngine;
using viva;


public class Tutorial: VivaScript{

    public enum Phase{
        NONE,
        HELLO,
        FOLLOW,
        STOP_FOLLOW,
        PLAYER_GRAB_PEACH,
        TAKE,
        GIVE_PEACH_TO_MERIDA,
        SELECT_MERIDA,
        COMMAND_TO_FUTON,
        COMPLETE
    }

    private Scene scene;
    private Phase phase = Phase.NONE;
    private bool complete = false;
    private Character merida;
    private bool waved = false;
    private bool followed = false;
    private bool stoppedFollow = false;

    public Tutorial( Scene _scene ){
        scene = _scene;

        var spawnPos = GameObject.Find("toriiGate0").transform.TransformPoint( 1, 0.5f, -1 );
        Character.Spawn( "klee", spawnPos, Quaternion.Euler(0,180,0), delegate( Character character ){
            character.onGesture.AddListener( this, OnGesture );
            character.onSelected.AddListener( this, OnMeridaSelected );
            merida = character;
            NextPhase();
        } );
    }

    private void OnMeridaSelected(){
        if( phase == Phase.SELECT_MERIDA ){
            Viva.main.StartCoroutine( DelayNext() );
        }
    }

    private void OnGesture( string gesture, Character source ){
        if( phase == Phase.HELLO ){ 
            if( gesture == "hello" && !waved ){
                OpenGate(0);
                waved = true;
            }
        }else if( phase == Phase.FOLLOW ){
            if( gesture == "follow" && !followed ){
                followed = true;
                OpenGate(1);
            }
        }else if( phase == Phase.STOP_FOLLOW ){
            if( gesture == "stop" && !stoppedFollow ){
                stoppedFollow = true;
                OpenGate(2);
            }
        }else if( phase == Phase.TAKE ){
            if( gesture == "take" ){
                if( VivaPlayer.user.character.biped.rightHandGrabber.IsGrabbing("peach") || VivaPlayer.user.character.biped.leftHandGrabber.IsGrabbing("peach") ){
                    Viva.main.StartCoroutine( DelayNext() );
                }
            }
        }
    }

    private void NextPhase(){
        complete = false;
        phase++;
        switch( phase ){
        case Phase.HELLO:
        case Phase.FOLLOW:
        case Phase.STOP_FOLLOW:
            DisplayMessageInFrontOfGate((int)phase-1);
            MoveMerida((int)phase-1);
            break;
        case Phase.PLAYER_GRAB_PEACH:
            var spawnPos = GameObject.Find("toriiGate3").transform.position+Vector3.up*1.5f-Vector3.forward*1.5f;
            var peach = Item.Spawn("peach", spawnPos, Quaternion.identity );
            if( peach ){
                peach.grabbables[0].onGrabbed.AddListener( this, OnPeachGrabbed );
                peach.onAttributeChanged.AddListener( this, PeachAttributeAdded );
                DisplayMessage( spawnPos );
            }
            break;
        case Phase.TAKE:
        case Phase.GIVE_PEACH_TO_MERIDA:
            DisplayMessageInFrontOfGate(3);
            break;
        case Phase.SELECT_MERIDA:
            DisplayMessageInFrontOfGate(4);
            var bedPos = GameObject.Find("toriiGate4").transform.position+Vector3.up*0.25f-Vector3.forward*1.5f;
            var futon = Item.Spawn("futon", bedPos, Quaternion.identity );
            if( futon ){
                futon.onSelected.AddListener( this, OnFutonSelected );
            }
            break;
        case Phase.COMMAND_TO_FUTON:
            DisplayMessageInFrontOfGate(4);
            break;
        case Phase.COMPLETE:
            DisplayMessageInFrontOfGate(4);
            break;
        }
    }

    private void PeachAttributeAdded( Item item, Attribute attribute ){
        if( attribute.name == Item.offerAttribute && phase == Phase.TAKE ){
            Viva.main.StartCoroutine( DelayNext() );
        }
    }

    private void OnFutonSelected(){
        if( phase == Phase.COMMAND_TO_FUTON ){
            Viva.main.StartCoroutine( DelayNext() );
        }
    }

    private void MoveMerida( int index ){
        var gate = GameObject.Find("toriiGate"+index).transform;

        var goToGate = new MoveTo( merida.autonomy );
        goToGate.target.SetTargetPosition( gate.TransformPoint( new Vector3( 1, 0.5f, -1 ) ) );

        var lookAtPlayer = new FaceTargetBody( merida.autonomy );
        lookAtPlayer.target.SetTargetCharacter( VivaPlayer.user.character );
        lookAtPlayer.AddRequirement( goToGate );

        lookAtPlayer.Start( this, "gate task "+index );
    }

    private void DisplayMessageInFrontOfGate( int index ){
        var gate = GameObject.Find("toriiGate"+index).transform;
        var pos = gate.position+Vector3.up*1.5f-Vector3.forward*0.4f;
        MessageManager.main.DisplayMessage( pos, GetPhaseInstructions(), delegate{ return complete; } );
    }

    private void DisplayMessage( Vector3 pos ){
        MessageManager.main.DisplayMessage( pos, GetPhaseInstructions(), delegate{ return complete; } );
    }

    private void OnPeachGrabbed( GrabContext grabContext){
        if(phase == Phase.PLAYER_GRAB_PEACH ){
            Viva.main.StartCoroutine( DelayNext() );
        }else if(grabContext.grabber.character == merida && phase == Phase.GIVE_PEACH_TO_MERIDA ){
            MoveMerida(3);
            //OpenGate(3);
            
        }
    }

    private IEnumerator DelayNext(){
        
        complete = true;
        yield return new WaitForSeconds( 1.0f );
        Sound.main.PlayGlobalUISound( UISound.MESSAGE_SUCCESS );
        NextPhase();
    }

    private string GetPhaseInstructions(){
        var vr = VivaPlayer.user.isUsingKeyboard;
        switch( phase ){
        case Phase.HELLO:
            if( vr ){
                return@"Welcome to Open Viva
You can use gestures to interact with characters.
Face the character and press (R) to gesture HELLO";
            }else{
                return @"Welcome to Open Viva.
You can use gestures to interact with characters.
Slowly wave your hand at the character to gesture HELLO";
            }
        case Phase.FOLLOW:
            if( vr ){
                return @"Press (F) to gesture FOLLOW";
            }else{
                return @"Wave your hand towards you with
your palm facing you to gesture FOLLOW";
            }
        case Phase.STOP_FOLLOW:
            if( vr ){
                return @"Hold (F) to gesture STOP FOLLOW";
            }else{
                return @"Place your palm flat in front of them
to gesture STOP. Useful to stop them from doing anything.";
            }
        case Phase.PLAYER_GRAB_PEACH:
            if( vr ){
                return @"Pick up the peach with left or right mouse
the peach will glow when you are within range";
            }else{
                return @"Pick up the peach with the controller grip";
            }
        case Phase.TAKE:
            if( vr ){
                return @"With the peach in your hand offer it by pressing
(Q) for left hand or (E) for right hand";
            }else{
                return @"With the peach in your hand offer it by putting
your arm in front with your palm facing up";
            }
        case Phase.GIVE_PEACH_TO_MERIDA:
            if( vr ){
                return @"While in offer mode, give the peach to Merida";
            }else{
                return @"While in offer mode, give the peach to Merida";
            }
        case Phase.SELECT_MERIDA:
            if( vr ){
                return @"To give specific commands, select Merida
by holding left mouse and pointing at her";
            }else{
                return @"To give specific commands, select Merida
by holding right trigger and pointing at her";
            }
        case Phase.COMMAND_TO_FUTON:
            return @"Now do the same on the bed and select sleep";
        case Phase.COMPLETE:
            if( vr ){
                return @"Tutorial complete!
                Exit by pressing TAB-> Menu -> Return To Main Menu";
            }else{
                return @"Tutorial complete! Exit through Menu
                Exit by pressing MENU BUTTON-> Menu -> Return To Main Menu";
            }
        }
        return "";
    }
    
    public void OpenGate( int index ){
        Viva.main.StartCoroutine( AnimateGate( GameObject.Find("gate"+index) ) );
    }

    private IEnumerator AnimateGate( GameObject gate ){

        complete = true;
        yield return new WaitForSeconds( 1.0f );
        Sound.main.PlayGlobalUISound( UISound.MESSAGE_SUCCESS );
        
        float timer = 0;
        float duration = 1.5f;
        float startY = gate.transform.position.y;
        float targetY = gate.transform.position.y-2f;

        while( timer < duration ){
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01( timer/duration );
            alpha = Tools.EaseInOutQuad( alpha );

            var pos = gate.transform.position;
            pos.y = Mathf.LerpUnclamped( startY, targetY, alpha );
            gate.transform.position = pos;

            yield return new WaitForFixedUpdate();
        }
        NextPhase();
    }
} 