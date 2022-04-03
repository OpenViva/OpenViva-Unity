using UnityEngine;
using viva;

public class Hug: VivaScript{

    private Character self;
    private bool hugging = false;

    public Hug( Character character ){
        self = character;

        var hugAnim = new AnimationSingle( viva.Animation.Load("MyAnimation"), self, false );
        self.animationSet["stand"]["hug"] = hugAnim;

        self.autonomy.listener.onFixedUpdate += delegate{
            if( !self.ragdoll.ground.HasValue || !VivaPlayer.user.character.ragdoll.ground.HasValue ) return;
            var distance = Vector3.Distance( self.ragdoll.ground.Value, VivaPlayer.user.character.ragdoll.ground.Value );
            if( distance > 4.0f ){
                HugPlayer();
            }
        };
    }

    public void HugPlayer(){
        if( hugging ){
            return; //dont hug so console doesnt spam
        }
        var playHugAnim = new PlayAnimation( self.autonomy, null, "hug" );
        playHugAnim.onSuccess += delegate{
            hugging = false;
        };
        self.autonomy.AddTask( playHugAnim, "hug character" );

        var followPlayer = new MoveTo( self.autonomy, null, 1.0f );
        followPlayer.target.SetTargetRagdoll( VivaPlayer.user.character.ragdoll );
        followPlayer.onRegistered += delegate{
            self.biped.lookTarget.SetTargetTransform( VivaPlayer.user.character.ragdoll.head.transform );
        };
        followPlayer.onUnregistered += delegate{
            self.biped.lookTarget.SetTargetTransform( null );
        };

        playHugAnim.AddRequirement( followPlayer );
        hugging = true;
    }
}