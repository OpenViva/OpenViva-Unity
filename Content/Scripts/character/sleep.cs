using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using viva;


public class Sleep: VivaScript{

    public enum SleepPhase{
        NONE,
        TIRED,
        RUB_EYES,
        LAY_ON_BED,
        SLEEPING,
        WAKE_UP,
        GET_UP
    }

    private readonly Character character;
    private Item bed;
    private EnumTaskManager<SleepPhase> sleepManager;
    private Timer rubEyesTimer = null;


    public Sleep( Character _character ){
        character = _character;

        if( character.isPossessed ) return;

        SetupAnimations(); 

        sleepManager = new EnumTaskManager<SleepPhase>();
        sleepManager[ SleepPhase.NONE ] = UndoBed;
        sleepManager[ SleepPhase.TIRED ] = Tired;
        sleepManager[ SleepPhase.RUB_EYES ] = RubEyes;
        sleepManager[ SleepPhase.LAY_ON_BED ] = LayDownOnBed;
        sleepManager[ SleepPhase.SLEEPING ] = FallAsleep;
        sleepManager[ SleepPhase.WAKE_UP ] = WakeUp;
        sleepManager[ SleepPhase.GET_UP ] = GetUp;

		AmbienceManager.main.onDayEvent.AddListener( this, OnDayEvent );
		character.onGesture.AddListener( this, ListenForStopGesture );
        character.altAnimationLayer.player.onAnimationChange += ListenForLayBodySet;
    }

    private void ListenForLayBodySet( int animationLayerIndex ){
        if( animationLayerIndex != character.altAnimationLayerIndex ) return;
        switch( character.altAnimationLayer.currentBodySet.name ){
        case "lay side left":
        case "lay side right":
        case "lay side up":
        case "sleep side left":
        case "sleep side right":
        case "sleep side up":
            character.biped.pinLimit.Add( "sleeping", 0f );
            break;
        }
        switch( character.altAnimationLayer.nextBodySet?.name ){
        case "lay side left":
        case "lay side right":
        case "lay side up":
        case "sleep side left":
        case "sleep side right":
        case "sleep side up":
            break;
        default:
            character.biped.pinLimit.Remove( "sleeping" );
            break;
        }
    }


    private void ListenForStopGesture( string gesture, Character source ){
        if( gesture == "stop" ){
            if( sleepManager.current == SleepPhase.LAY_ON_BED ){
                sleepManager.SwitchTo( SleepPhase.TIRED );
            }
        }
    }

    private void OnDayEvent( DayEvent dayEvent ){
        if( sleepManager.current == SleepPhase.NONE && dayEvent == DayEvent.NIGHT ) sleepManager.SwitchTo( SleepPhase.TIRED );
    }

    private Task UndoBed(){
        if( bed ){
            bed.customVariables.Get( this, "occupied" ).value = null;
            bed = null;
        }
        //return to idle
        return new PlayAnimation( character.autonomy, "stand", "idle", false, 0 );
    }

    private void SetupAnimations(){
        
        //stand tired animations
        var stand = character.animationSet.GetBodySet("stand");
        var standTired = character.animationSet.GetBodySet( "stand tired" );

        var standToTiredYawn = stand.Single( "yawn", "stand_to_stand_tired", false );
        standToTiredYawn.AddEvent( Event.Voice(0.1f,"yawn") );
        standToTiredYawn.curves[BipedRagdoll.headID] = new Curve(0,0.2f);
        
        // standTired["yawn"] = standToTiredYawn;
        stand.transitions[ standTired ] = standToTiredYawn;

        var tiredWalk = new AnimationSingle( viva.Animation.Load("stand_tired_walk_forward_loop"), character, true, 0.8f );
        tiredWalk.AddEvent( Event.Footstep(.44f,false) );
        tiredWalk.AddEvent( Event.Footstep(.96f,true) );
        
        var tiredRun = new AnimationSingle( viva.Animation.Load("body_stand_tired_run"), character, true,1.2f );
        tiredRun.AddEvent( Event.Footstep(.44f,false) );
        tiredRun.AddEvent( Event.Footstep(.96f,true) );

        var standTiredIdle = standTired.Mixer(
            "idle",
            new AnimationNode[]{
                new AnimationSingle( viva.Animation.Load("stand_tired_idle_loop" ), character, true ),
                tiredWalk,
                tiredRun
            },
            new Weight[]{
                character.GetWeight("idle"),
                character.GetWeight("walking"),
                character.GetWeight("running")
            }
        );
        standTiredIdle.curves[BipedRagdoll.headID] = new Curve(0.6f);
        standToTiredYawn.nextState = standTiredIdle;
        
		var slideDoorRight = standTired.Single( "slide door right", "stand_slide_door_right", true, 0.8f );
		slideDoorRight.nextState = standTiredIdle;

		var slideDoorLeft = standTired.Single( "slide door left", "stand_slide_door_left", true, 0.8f );
		slideDoorLeft.nextState = standTiredIdle;

        var standTiredRefuse = standTired.Single( "refuse", "stand_tired_refuse", false );
        standTiredRefuse.nextState = standTiredIdle;
        standTiredRefuse.curves[BipedRagdoll.headID] = new Curve(0f,0.2f);
        standTiredRefuse.curves[BipedRagdoll.eyesID] = new Curve(0f,0.2f);

        var rubEyeRight = standTired.Single( "rub eye right", "stand_tired_rub_eyes_right", false );
        rubEyeRight.nextState = standTiredIdle;
        rubEyeRight.AddEvent( Event.Voice( 0.5f, "sleep disturbed short" ) );
        rubEyeRight.curves[BipedRagdoll.headID] = new Curve(0,0.1f);

        var rubEyeLeft = standTired.Single( "rub eye left", "stand_tired_rub_eyes_left", false );
        rubEyeLeft.nextState = standTiredIdle;
        rubEyeLeft.curves[BipedRagdoll.headID] = new Curve(0,0.1f);
        rubEyeLeft.AddEvent( Event.Voice( 0.5f, "sleep disturbed short" ) );

        //crawl tired animations
        var crawlTired = character.animationSet.GetBodySet( "crawl tired" );

        var standTiredToCrawl = new AnimationSingle( viva.Animation.Load( "stand_tired_to_crawl" ), character, false );
        standTiredToCrawl.AddEvent( Event.Function( 0.3f, this, DropAllGrabbedItems ) );
        standTired.transitions[ crawlTired ] = standTiredToCrawl;
        
        var crawlToStandTired = new AnimationSingle( viva.Animation.Load( "crawl_tired_to_stand_tired" ), character, false );
        crawlTired.transitions[ standTired ] = crawlToStandTired;
        crawlToStandTired.nextState = standTiredIdle;
        crawlToStandTired.curves[BipedRagdoll.headID] = new Curve( new CurveKey[]{
			new CurveKey(0,1),new CurveKey(1,0)
		});

        var crawlToStand = new AnimationSingle( viva.Animation.Load( "crawl_tired_to_stand_tired" ), character, false );
        crawlTired.transitions[ stand ] = crawlToStand;
        crawlToStand.nextState = stand["idle"];
        crawlToStand.curves[BipedRagdoll.headID] = new Curve( new CurveKey[]{
			new CurveKey(0,0),new CurveKey(1,1)
		});

        var crawlTiredLocomotion = crawlTired.Mixer(
            "idle",
            new AnimationNode[]{
                new AnimationSingle( viva.Animation.Load("crawl_tired_idle_loop"), character, true ),
                new AnimationSingle( viva.Animation.Load("crawl_tired_forward_loop"), character, true ),
                new AnimationSingle( viva.Animation.Load("crawl_tired_forward_loop"), character, true )
            },
            new Weight[]{
                character.GetWeight("idle"),
                character.GetWeight("walking"),
                character.GetWeight("running")
            }
        );
        standTiredToCrawl.nextState = crawlTiredLocomotion;

        //lay side right animations
        var laySideRight = character.animationSet.GetBodySet( "lay side right" );

        var laySideRightIdle = laySideRight.Single( "idle", "lay_side_pillow_happy_idle_loop_right", true );
        laySideRightIdle.curves[BipedRagdoll.headID] = new Curve(0);

        var crawlTiredToLaySideRight = new AnimationSingle( viva.Animation.Load( "crawl_tired_to_lay_side_pillow_happy_right" ), character, false );
        crawlTired.transitions[ laySideRight ] = crawlTiredToLaySideRight;
        crawlTiredToLaySideRight.nextState = laySideRightIdle;
        crawlTiredToLaySideRight.curves[BipedRagdoll.headID] = new Curve(0);

        var laySideRightYawn = laySideRight.Single( "yawn", "lay_side_pillow_yawn_long_right", false );
		laySideRightYawn.AddEvent( Event.Voice(0,"yawn") );
        laySideRightYawn.curves[BipedRagdoll.headID] = new Curve(0);

        //lay side left animations  
        var laySideLeft = character.animationSet.GetBodySet( "lay side left" );

        var laySideLeftIdle = laySideLeft.Single( "idle", "lay_side_pillow_happy_idle_loop_left", true );
        laySideLeftIdle.curves[BipedRagdoll.headID] = new Curve(0);

        var crawlTiredToLaySideLeft = new AnimationSingle( viva.Animation.Load( "crawl_tired_to_lay_side_pillow_happy_left" ), character, false );
        crawlTired.transitions[ laySideLeft ] = crawlTiredToLaySideLeft;
        crawlTiredToLaySideLeft.nextState = laySideLeftIdle;
        crawlTiredToLaySideLeft.curves[BipedRagdoll.headID] = new Curve(0);

        var laySideLeftYawn = laySideLeft.Single( "yawn", "lay_side_pillow_yawn_long_left", false );
		laySideLeftYawn.AddEvent( Event.Voice(0,"yawn") );
        laySideLeftYawn.curves[BipedRagdoll.headID] = new Curve(0);

        //sleep side right animations
        var sleepSideRight = character.animationSet.GetBodySet( "sleep side right" );

        var sleepSideRightIdle = sleepSideRight.Single( "idle", "sleep_side_pillow_idle_loop_right", true );
		sleepSideRightIdle.AddEvent( Event.Voice(0,"sleep breathing") );
        sleepSideRightIdle.curves[BipedRagdoll.headID] = new Curve(0);

        var laySideRightToSleepSideRight = new AnimationSingle( viva.Animation.Load( "lay_side_pillow_to_sleep_side_pillow_right" ), character, false );
        laySideRight.transitions[ sleepSideRight ] = laySideRightToSleepSideRight;
        laySideRightToSleepSideRight.nextState = sleepSideRightIdle;
        laySideRightToSleepSideRight.curves[BipedRagdoll.headID] = new Curve(0);
        
        //sleep side left animations
        var sleepSideLeft = character.animationSet.GetBodySet( "sleep side left" );

        var sleepSideLeftIdle = sleepSideLeft.Single( "idle", "sleep_side_pillow_idle_loop_left", true );
		sleepSideLeftIdle.AddEvent( Event.Voice(0,"sleep breathing") );
        sleepSideLeftIdle.curves[BipedRagdoll.headID] = new Curve(0);

        var laySideLeftToSleepSideLeft = new AnimationSingle( viva.Animation.Load( "lay_side_pillow_to_sleep_side_pillow_left" ), character, false );
        laySideLeft.transitions[ sleepSideLeft ] = laySideLeftToSleepSideLeft;
        laySideLeftToSleepSideLeft.nextState = sleepSideLeftIdle;
        laySideLeftToSleepSideLeft.curves[BipedRagdoll.headID] = new Curve(0);
        
        //sleep side up animations
        var sleepSideUp = character.animationSet.GetBodySet( "sleep side up" );
        
        var sleepSideUpIdle = sleepSideUp.Single( "idle", "sleep_pillow_up_idle_loop", true );
		sleepSideUpIdle.AddEvent( Event.Voice(0,"sleep breathing") );
        sleepSideUpIdle.curves[BipedRagdoll.headID] = new Curve( new CurveKey[]{ new CurveKey(0,0.3f) });
        
        var sleepSideRightToSleepSideUp = new AnimationSingle( viva.Animation.Load( "sleep_side_pillow_to_sleep_pillow_up_right" ), character, false );
        sleepSideRight.transitions[ sleepSideUp ] = sleepSideRightToSleepSideUp;
        sleepSideRightToSleepSideUp.nextState = sleepSideUpIdle;
        sleepSideRightToSleepSideUp.curves[BipedRagdoll.headID] = new Curve(0);

        var sleepSideLeftToSleepSideUp = new AnimationSingle( viva.Animation.Load( "sleep_side_pillow_to_sleep_pillow_up_left" ), character, false );
        sleepSideLeft.transitions[ sleepSideUp ] = sleepSideLeftToSleepSideUp;
        sleepSideLeftToSleepSideUp.nextState = sleepSideUpIdle;
        sleepSideLeftToSleepSideUp.curves[BipedRagdoll.headID] = new Curve(0);

        var sleepSideUpToSleepSideLeft = new AnimationSingle( viva.Animation.Load( "sleep_pillow_up_to_sleep_side_pillow_left" ), character, false );
        sleepSideUp.transitions[ sleepSideLeft ] = sleepSideUpToSleepSideLeft;
        sleepSideUpToSleepSideLeft.nextState = sleepSideLeftIdle;
        sleepSideUpToSleepSideLeft.curves[BipedRagdoll.headID] = new Curve(0);
        
        var sleepSideUpToSleepSideRight = new AnimationSingle( viva.Animation.Load( "sleep_pillow_up_to_sleep_side_pillow_right" ), character, false );
        sleepSideUp.transitions[ sleepSideRight ] = sleepSideUpToSleepSideRight;
        sleepSideUpToSleepSideRight.nextState = sleepSideRightIdle;
        sleepSideUpToSleepSideRight.curves[BipedRagdoll.headID] = new Curve(0);

        //awake animations
        var laySideUp = character.animationSet.GetBodySet( "lay side up" );

        var laySideUpIdle = laySideUp.Single( "idle", "awake_happy_pillow_up_idle_loop", true );
        laySideUpIdle.curves[BipedRagdoll.headID] = new Curve( new CurveKey[]{ new CurveKey(0,0.4f) });

        var sleepSideRightToLaySideUp = new AnimationSingle( viva.Animation.Load( "sleep_side_pillow_to_awake_happy_pillow_up_right" ), character, false );
        sleepSideRight.transitions[laySideUp] = sleepSideRightToLaySideUp;
        sleepSideRightToLaySideUp.nextState = laySideUpIdle;
        sleepSideRightToLaySideUp.curves[BipedRagdoll.headID] = new Curve(0);
        
        var sleepSideLeftToLaySideUp = new AnimationSingle( viva.Animation.Load( "sleep_side_pillow_to_awake_happy_pillow_up_left" ), character, false );
        sleepSideLeft.transitions[laySideUp] = sleepSideLeftToLaySideUp;
        sleepSideLeftToLaySideUp.nextState = laySideUpIdle;
        sleepSideLeftToLaySideUp.curves[BipedRagdoll.headID] = new Curve(0);
        
        var laySideUpToCrawl = new AnimationSingle( viva.Animation.Load( "awake_pillow_up_to_crawl" ), character, false );
        laySideUp.transitions[crawlTired] = laySideUpToCrawl;
        laySideUpToCrawl.nextState = crawlTiredLocomotion;
        laySideUpToCrawl.curves[BipedRagdoll.headID] = new Curve(0);
        
        var laySideRightToLaySideUp = new AnimationSingle( viva.Animation.Load( "awake_side_pillow_to_awake_pillow_up_right" ), character, false );
        laySideRight.transitions[laySideUp] = laySideRightToLaySideUp;
        laySideRightToLaySideUp.nextState = laySideUpIdle;
        laySideRightToLaySideUp.curves[BipedRagdoll.headID] = new Curve(0);
        
        var laySideLeftToLaySideUp = new AnimationSingle( viva.Animation.Load( "awake_side_pillow_to_awake_pillow_up_left" ), character, false );
        laySideLeft.transitions[laySideUp] = laySideLeftToLaySideUp;
        laySideLeftToLaySideUp.nextState = laySideUpIdle;
        laySideLeftToLaySideUp.curves[BipedRagdoll.headID] = new Curve(0);

        var sleepSideUpToLaySideUp = new AnimationSingle( viva.Animation.Load( "sleep_pillow_up_to_awake_pillow_up" ), character, false );
        sleepSideUp.transitions[laySideUp] = sleepSideUpToLaySideUp;
        sleepSideUpToLaySideUp.nextState = laySideUpIdle;
        sleepSideUpToLaySideUp.curves[BipedRagdoll.headID] = new Curve(0);
    }

    private void DropAllGrabbedItems(){
        character.biped.rightHandGrabber.ReleaseAll();
        character.biped.leftHandGrabber.ReleaseAll();
    }

    private Task Tired(){
        var playTiredAnim = new PlayAnimation( character.autonomy, "stand tired", null, true, 0 );
        
        if( rubEyesTimer == null ){
            rubEyesTimer = new Timer( character.autonomy, 20+Random.value*15 );
            rubEyesTimer.onSuccess += delegate{
                if( sleepManager.current == SleepPhase.TIRED ) sleepManager.SwitchTo( SleepPhase.RUB_EYES );
            };
            rubEyesTimer.StartConstant( this, "rub eyes timer" );
        }

        return playTiredAnim;
    }

    public void SleepOnBed( Item item ){
        if( bed != null ) return;
        if( item.HasAttribute("bed") ){
            if( CanSleepOnBed( item ) ){
                bed = item;
                sleepManager.SwitchTo( SleepPhase.LAY_ON_BED );
            }else{
                var refuse = new PlayAnimation( character.autonomy, null, "refuse" );
                refuse.Start( this, "refuse" );
            }
        }
    }

    private bool CanSleepOnBed( Item bed ){
        var occupied = bed.customVariables.Get( this, "occupied" );
        return occupied.value as Character == null;
    }

    private Task LayDownOnBed(){
        var playAboutToSleepAnim = new PlayAnimation( character.autonomy, "lay side "+RandomSide(2), null, true, 0 );
        if( !StayOnBed( playAboutToSleepAnim ) ){
            sleepManager.SwitchTo( SleepPhase.TIRED );
            return null;
        }

        var fallAsleepTimer = new Timer( character.autonomy, 5 );
        fallAsleepTimer.onSuccess += delegate{
            sleepManager.SwitchTo( SleepPhase.SLEEPING );
        };
        fallAsleepTimer.onFail += delegate{
            sleepManager.SwitchTo( SleepPhase.LAY_ON_BED );
        };

        fallAsleepTimer.AddRequirement( playAboutToSleepAnim );

        return fallAsleepTimer;
    }

    private bool StayOnBed( Task task ){
        if( bed == null || !bed.model.bounds.HasValue ){
            return false;
        }
        var bedBounds = bed.model.bounds;

        var ensureBedStillUnoccupied = new CustomVariableCondition( character.autonomy, this, bed, "occupied" );
        ensureBedStillUnoccupied.onCondition += delegate( CustomVariable variable ){
            var occupant = variable.value as Character;
            return occupant == null || occupant == character;
        };
        ensureBedStillUnoccupied.onFail += delegate{
            //find next unoccupied bed
            var beds = character.biped.vision.FindItemsByAttributes( new AttributeRequest( new string[]{"bed"}, true, CountMatch.EQUAL ) );
            bed = null;
            foreach( var newBed in beds ){
                if( CanSleepOnBed( newBed ) ){
                    bed = newBed;
                    break;
                }
            }
            sleepManager.SwitchTo( SleepPhase.LAY_ON_BED );
            ensureBedStillUnoccupied.Reset();   //catch fail chain
        };

        var goToBed = new MoveTo( character.autonomy, 0 );
        goToBed.SetNearbyTargetBodySet( "crawl tired", 1f );
        goToBed.target.SetTargetPosition( bedBounds.Value.center+Vector3.up*bedBounds.Value.extents.y );
        goToBed.onSuccess += delegate{
            bed.customVariables.Get( this, "occupied" ).value = character; //occupy bed
        };
        goToBed.AddRequirement( ensureBedStillUnoccupied );
        
        task.AddRequirement( goToBed );

        var faceUpright = new FaceTargetBody( character.autonomy, 1, 20.0f, 0.6f, goToBed.target );
        faceUpright.target.SetTargetPosition( bedBounds.Value.center+bed.model.rootTransform.forward*100 );
        goToBed.onRegistered += delegate{
            task.AddRequirementAfter( faceUpright, goToBed );
        };
        faceUpright.onSuccess += delegate{
            task.RemoveRequirement( faceUpright );
        };

        return true;
    }

    private void CheckIfMorning( DayEvent dayEvent ){
        if( dayEvent == DayEvent.MORNING ){
            sleepManager.SwitchTo( SleepPhase.WAKE_UP );
        }
    }

    private Task FallAsleep(){
        var sleepTillMorning = new Task( character.autonomy );   //sleep for 60 seconds

        sleepTillMorning.onRegistered += delegate{
            character.biped.muscleLimit.Add( "sleeping", 0.3f );
            AmbienceManager.main.onDayEvent.AddListener( this, CheckIfMorning );
        };
        sleepTillMorning.onUnregistered += delegate{
            character.biped.muscleLimit.Remove( "sleeping" );
            AmbienceManager.main.onDayEvent.RemoveListener( this, CheckIfMorning );
        };

        sleepTillMorning.onInterrupted += delegate{
            if( sleepManager.current == SleepPhase.LAY_ON_BED ) sleepManager.SwitchTo( SleepPhase.LAY_ON_BED );
        };

        var playSleepingAnim = new PlayAnimation( character.autonomy, "sleep side "+RandomSide(3), null );
        sleepTillMorning.AddRequirement( playSleepingAnim );
        return sleepTillMorning;
    }

    private Task WakeUp(){
        var getUpTimer = new Timer( character.autonomy, 3 );
        getUpTimer.onSuccess += delegate{
            sleepManager.SwitchTo( SleepPhase.GET_UP );
        };

        var playWakeUpAnim = new PlayAnimation( character.autonomy, "lay side up" );
        getUpTimer.AddRequirement( playWakeUpAnim );
        
        return getUpTimer;
    }

    private Task GetUp(){
        var getUpAnim = new PlayAnimation( character.autonomy, "stand", null, false, 0 );
        getUpAnim.onSuccess += delegate{ sleepManager.SwitchTo( SleepPhase.NONE ); };
        getUpAnim.onFail += delegate{ sleepManager.SwitchTo( SleepPhase.NONE ); };
        return getUpAnim;
    }

    private string RandomSide(int maxSide){
        return new string[]{ "right", "left", "up" }[ Random.Range(0,maxSide) ];
    }
    
    //rub eyes in whichever BodySet the character is in, ignore if it fails
    private Task RubEyes(){
        string rubEyeAnim;
        if( character.biped.rightHandGrabber.grabbing ){
            rubEyeAnim = "rub eye left";
        }else if( character.biped.leftHandGrabber.grabbing ){
            rubEyeAnim = "rub eye right";
        }else{
            rubEyeAnim = Random.value > 0.5f ? "rub eye right" : "rub eye left";
        }
        var playRubEyeAnim = new PlayAnimation( character.autonomy, null, rubEyeAnim );
        playRubEyeAnim.onSuccess += delegate{ sleepManager.SwitchTo( SleepPhase.TIRED ); };
        playRubEyeAnim.onFail += delegate{ sleepManager.SwitchTo( SleepPhase.TIRED); };

        return playRubEyeAnim;
    }
}
