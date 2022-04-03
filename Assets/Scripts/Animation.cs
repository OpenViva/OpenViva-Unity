using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;


namespace viva{

[System.Serializable]
public class Animation: VivaEditable{

    private delegate void ChannelCallback( AnimationChannel channel, int bindIndex );

    private static Dictionary<string,Animation> animations = new Dictionary<string, Animation>();
    public static string root { get{ return Viva.contentFolder+"/Animations"; } }


    public static Animation Load( string name ){
        Animation animation;
        if( animations.TryGetValue( name, out animation ) ){
            return animation;
        }else{
            string data = File.ReadAllText( Animation.root+"/"+name+".anim" );
            animation = JsonUtility.FromJson( data, typeof(Animation) ) as Animation;
            MirrorAndRegisterIfRequired( animation );

            if( animation != null && animation.IsValid() ){
                animations[ animation.name ] = animation;
                return animation;
            }else{
                return null;
            }
        }
    }

    public static void RegisterBuiltInAnimation( string data ){
        var animation = JsonUtility.FromJson( data, typeof(Animation) ) as Animation;
        if( animation != null && animation.IsValid() ){
            animations[ animation.name ] = animation;
            MirrorAndRegisterIfRequired( animation );
        }else{
            Debug.LogError("Built in Animation \""+animation.name+"\" could not be loaded");
        }
    }

    private static void MirrorAndRegisterIfRequired( Animation animation ){
        if( !animation.createMirrorVariant ) return;

        var mirroredAnim = new Animation( animation, true );
        animations[ mirroredAnim.name ] = mirroredAnim;
    }

    [SerializeField]
    private string m_name;
    public string name { get{ return m_name; } }
    [SerializeField]
    private float m_duration;
    public float duration { get{ return m_duration; } }
    [SerializeField]
    private float m_baseFramerate;
    public float baseFramerate { get{ return m_baseFramerate; } }
    [SerializeField]
    private AnimationChannel[] m_channels;
    public AnimationChannel[] channels { get{ return m_channels; } }
    [SerializeField]
    private bool m_ragdollCompatible = false;
    public bool ragdollCompatible { get{ return m_ragdollCompatible; } }
    public bool enableRootMotion = false;
    public bool createMirrorVariant = false;
    

    public Animation( string _name, AnimationChannel[] _channels, float _duration, float _baseFramerate, FBXRequest __internalSourceRequest ):base(__internalSourceRequest){
        m_name = _name;
        m_channels = _channels;
        m_duration = _duration;
        m_baseFramerate = _baseFramerate;
        Debug.Log("Created animation \""+name+"\" "+duration+" sec. @"+baseFramerate);
    }

    public Animation( Animation copy, bool mirror ):base(null){
        m_name = copy.m_name;
        m_channels = new AnimationChannel[ copy.m_channels.Length ];
        for( int i=0; i<m_channels.Length; i++ ){
            m_channels[i] = new AnimationChannel( copy.m_channels[i] );
        }
        m_duration = copy.m_duration;
        m_baseFramerate = copy.m_baseFramerate;
        if( mirror ){
            if( m_name.EndsWith("_right") ){
                m_name = Regex.Replace( m_name, "_right$", "_left" );
            }else if( m_name.EndsWith("_left") ){
                m_name = Regex.Replace( m_name, "_left$", "_right" );
            }else{
                Debug.LogError("Could not mirror animation name \""+m_name+"\". Must end with _right or _left");
            }
            foreach( var channel in m_channels ){
                channel.Mirror();
            }
            // Debug.Log("Mirrored animation \""+name+"\" "+duration+" sec. @"+baseFramerate);
        }
    }

    public bool IsValid(){
        if( name == null ) return false;
        if( channels == null ) return false;
        foreach( var channel in channels ){
            if( channel == null ) return false;
        }
        if( duration <= 0 ) return false;
        if( m_baseFramerate <= 0 ) return false;
        return true;
    }
    
    public void ApplyRagdollProfile( Model model ){
        if( model == null ){
            Debug.LogError("Model cannot be null for ragdoll offsets");
            return;
        }
        BipedProfile profile = model.bipedProfile;
        if( profile == null ){
            Debug.LogError("Cannot apply animation offsets with a null profile");
            return;
        }
        if( ragdollCompatible ){
            Debug.LogError("Animation already has had RagdollProfile offsets applied");
            return;
        }
        m_ragdollCompatible = true;
        var newChannelPairs = new List<Tuple<int,AnimationChannel>>();

        //fix tpose bone rolls
        IterateChannels( profile,
            delegate( AnimationChannel channel, int boneEnumIndex ){
                if( channel.bindTarget == model.deltaTransform.name ){
                    newChannelPairs.Add( new Tuple<int,AnimationChannel>( boneEnumIndex, channel ) );
                    //apply offset in reverse order to prevent applying multiple times to same FrameSet
                    for( int setIndex=0; setIndex<3; setIndex++ ){
                        var set = channel.frameSets[ setIndex ];
                        for( int i=0; i<set.frames.Length; i++ ){ //scale positions to match generic hip height
                            set.frames[i] /= profile.hipHeight;
                        }
                    }
                }
            },
            delegate( AnimationChannel channel, int boneEnumIndex ){
                newChannelPairs.Add( new Tuple<int,AnimationChannel>( boneEnumIndex, channel ) );
                //apply offset in reverse order to prevent applying multiple times to same FrameSet
                int maxFrames = Mathf.FloorToInt( baseFramerate*duration );
                for( int i=maxFrames; i-->0; ){ //apply parent and local rotation offsets to match generic armature rotations
                    
                    var set0 = channel.frameSets[0];
                    var set1 = channel.frameSets[1];
                    var set2 = channel.frameSets[2];
                    var set3 = channel.frameSets[3];
                    Quaternion q = new Quaternion( set0.Sample(i), set1.Sample(i), set2.Sample(i), set3.Sample(i) );
                    q = (profile.animParentDeltas[ boneEnumIndex ]*q)*profile.animLocalDeltas[ boneEnumIndex ];
                    q.Normalize();

                    if( i < set0.frames.Length ) set0.frames[i] = q.x;
                    if( i < set1.frames.Length ) set1.frames[i] = q.y;
                    if( i < set2.frames.Length ) set2.frames[i] = q.z;
                    if( i < set3.frames.Length ) set3.frames[i] = q.w;
                }
            },
            delegate( AnimationChannel channel, int bindIndex ){
                newChannelPairs.Add( new Tuple<int,AnimationChannel>( bindIndex, channel ) );
            }
        );
        //apply spine delta offsets
        IterateChannels( profile,
            null,
            delegate( AnimationChannel channel, int boneEnumIndex ){

                //apply offset in reverse order to prevent applying multiple times to same FrameSet
                int maxFrames = Mathf.FloorToInt( baseFramerate*duration );
                for( int i=maxFrames; i-->0; ){ //apply parent and local rotation offsets to match generic armature rotations
                    
                    var set0 = channel.frameSets[0];
                    var set1 = channel.frameSets[1];
                    var set2 = channel.frameSets[2];
                    var set3 = channel.frameSets[3];

                    var spineTPoseDeltaIndex = -1;
                    for( int j=0; j<BipedProfile.deltaTposeBones.Length; j++ ){
                        if( (int)BipedProfile.deltaTposeBones[j] == boneEnumIndex ){
                            spineTPoseDeltaIndex = j;
                            break;
                        }
                    }

                    if( spineTPoseDeltaIndex == -1 ) continue;

                    Quaternion q = new Quaternion( set0.Sample(i), set1.Sample(i), set2.Sample(i), set3.Sample(i) );
                    var children = BipedProfile.GetHierarchyChildren( (BipedBone)boneEnumIndex );
                    if( children != null ){
                        var cachedRelRot = new Quaternion[ children.Length ];
                        var sourceChannels = new AnimationChannel[ children.Length ];
                        for( int childIndex = 0; childIndex < children.Length; childIndex++ ){
                            
                            var sourceChannel = FindRagdollBoneRotationChannel( profile, children[ childIndex ] );
                            Quaternion relativeRot = q*new Quaternion(
                                sourceChannel.frameSets[0].Sample(i),
                                sourceChannel.frameSets[1].Sample(i),
                                sourceChannel.frameSets[2].Sample(i),
                                sourceChannel.frameSets[3].Sample(i)
                            );
                            cachedRelRot[ childIndex ] = relativeRot;
                            sourceChannels[ childIndex ] = sourceChannel;
                        }
                        q *= Quaternion.Euler( -profile.spineTposeDeltas[ spineTPoseDeltaIndex ].eulerAngles );
                        //restore old children relative rotations back into local space]
                        for( int childIndex = 0; childIndex < children.Length; childIndex++ ){
                            var sourceChannel = sourceChannels[ childIndex ];
                            var newLocalRelRot = Quaternion.Inverse( q )*cachedRelRot[ childIndex ];
                            var childSet0 = sourceChannel.frameSets[0];
                            var childSet1 = sourceChannel.frameSets[1];
                            var childSet2 = sourceChannel.frameSets[2];
                            var childSet3 = sourceChannel.frameSets[3];
                            if( i < childSet0.frames.Length ) childSet0.frames[i] = newLocalRelRot.x;
                            if( i < childSet1.frames.Length ) childSet1.frames[i] = newLocalRelRot.y;
                            if( i < childSet2.frames.Length ) childSet2.frames[i] = newLocalRelRot.z;
                            if( i < childSet3.frames.Length ) childSet3.frames[i] = newLocalRelRot.w;
                        }
                    }
                    q.Normalize();

                    if( i < set0.frames.Length ) set0.frames[i] = q.x;
                    if( i < set1.frames.Length ) set1.frames[i] = q.y;
                    if( i < set2.frames.Length ) set2.frames[i] = q.z;
                    if( i < set3.frames.Length ) set3.frames[i] = q.w;
                }
            },
            null
        );
        var newChannels = new AnimationChannel[ newChannelPairs.Count ];
        for( int i=0; i<newChannelPairs.Count; i++ ){
            var pair = newChannelPairs[i];
            var channel = pair._2;
            if( channel.channel == AnimationChannel.Channel.BLENDSHAPE ){
                channel.RenameBindTarget( ( (RagdollBlendShape)pair._1 ).ToString() );
            }else{
                channel.RenameBindTarget( ( (BipedBone)pair._1 ).ToString() );
            }
            newChannels[i] = pair._2;
        }
        m_channels = newChannels;
        Debug.Log("Applied offsets to \""+name+"\"");
    }


    private void IterateChannels( BipedProfile profile, ChannelCallback onPositionChannel, ChannelCallback onRotationChannel, ChannelCallback onBlendshapeChannel ){
        foreach( var channel in channels ){
            //remove animated channels not part of ragdoll profile
            switch( channel.channel ){
            case AnimationChannel.Channel.POSITION:
            case AnimationChannel.Channel.ROTATION:
                {
                    int boneEnumIndex = -1;
                    for( int i=0; i<profile.bones.Length; i++ ){
                        var boneInfo = profile.bones[i];
                        if( boneInfo != null && boneInfo.transform.name == channel.bindTarget ){
                            boneEnumIndex = i;
                            break;
                        }
                    }
                    if( boneEnumIndex == -1 ){
                        continue;
                    }

                    if( channel.channel == AnimationChannel.Channel.POSITION ){
                        onPositionChannel?.Invoke( channel, boneEnumIndex );
                    }else if( channel.channel == AnimationChannel.Channel.ROTATION ){
                        onRotationChannel?.Invoke( channel, boneEnumIndex );
                    }
                }
                break;
            case AnimationChannel.Channel.BLENDSHAPE:
                {
                    for( int i=0; i<profile.blendShapeBindings.Length; i++ ){
                        if( profile.blendShapeBindings[i] == channel.bindTarget ){
                            onBlendshapeChannel?.Invoke( channel, i );
                        }
                    }
                }
                break;
            }
        }
    }

    private AnimationChannel FindRagdollBoneRotationChannel( BipedProfile profile, BipedBone ragdollBone ){
        foreach( var channel in channels ){
            if( channel.channel != AnimationChannel.Channel.ROTATION ){
                continue;
            }
            int boneEnumIndex = -1;
            for( int i=0; i<profile.bones.Length; i++ ){
                var boneInfo = profile.bones[i];
                if( boneInfo != null && boneInfo.transform.name == channel.bindTarget ){
                    boneEnumIndex = i;
                    break;
                }
            }
            if( boneEnumIndex != (int)ragdollBone ){
                continue;
            }
            return channel;
        }
        return null;
    }
    
    public override string GetInfoHeaderTitleText(){
        return name;
    }
    public override void _InternalOnGenerateThumbnail(){
        ThumbnailGenerator.main.GenerateAnimationThumbnailTexture( this, thumbnail );
    }
    public override string GetInfoHeaderText(){
        return "Animation";
    }
    public override string GetInfoBodyContentText(){
        string s = "";
        s += "Duration: "+duration+"\n";
        s += "Channels: "+channels.Length+"\n";
        s += "BipedRagdoll compatible: "+( ragdollCompatible ? "<color=#00ff00>YES" : "<color=#ff0000>NO" )+"</color>\n";

        return s;
    }
    public override void OnCreateMenuSelected(){
        _internalSourceRequest?.OnCreateMenuSelected();
        GameUI.main.createMenu.DisplayVivaObjectInfo<Animation>( this );
        GameUI.main.createMenu.DisplayEditRagdollButton();
        var sourceFBXRequest = _internalSourceRequest as FBXRequest;
        if( sourceFBXRequest != null && sourceFBXRequest.lastSpawnedFBX ){
            ThumbnailGenerator.main.AnimateModel( sourceFBXRequest.lastSpawnedFBX.FindParentModel( this ), this );
        }
    }
    public override void OnCreateMenuDeselected(){
        _internalSourceRequest?.OnCreateMenuDeselected();
        ThumbnailGenerator.main.StopAnimation();
    }

    public override void OnInstall( string subFolder=null ){
        BuiltInAssetManager.main.Install( this, root, name, ".anim" );
        Sound.main.PlayGlobalUISound( UISound.SAVED );
        
#if UNITY_EDITOR
        BuiltInAssetManager.main.ArchiveAnimation( name, JsonUtility.ToJson( this, true ) );
#endif
    }

    public override List<CreateMenu.OptionInfo> OnCreateMenuOptionInfoDrawer(){
        var toggleOptions = new List<CreateMenu.OptionInfo>();
        toggleOptions.Add(
            new CreateMenu.OptionInfo("Create mirrored variant", createMirrorVariant, delegate( bool value ){
                createMirrorVariant = value;
            })
        );
        toggleOptions.Add(
            new CreateMenu.OptionInfo("Enable root motion", enableRootMotion, delegate( bool value ){
                enableRootMotion = value;
                ThumbnailGenerator.main.AnimateModel( ThumbnailGenerator.main.lastSelectedModel, this );
            })
        );
        return toggleOptions;
    }
}

}