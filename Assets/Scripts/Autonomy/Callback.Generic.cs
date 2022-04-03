
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{

public class ListenerRegistry{

    public List<ListenerBase> registeredTo = new List<ListenerBase>();
    public List<CustomVariables> registeredCustomVariables = new List<CustomVariables>();

    public void _InternalReset( VivaScript source ){
        var copy = registeredTo.ToArray();
        for( int i=0; i<copy.Length; i++ ){
            copy[i]._InternalRemoveAllListenersFromSource( this );
        }
        foreach( var customVariables in registeredCustomVariables ){
            customVariables?._InternalRemoveAllFromSource( source );
        }
    }
}


public struct Listen<T>{
    public ListenerRegistry registry;
    public T value;
}

public abstract class ListenerBase{

    public readonly string name;
    
    public ListenerBase( string _name ){
        name = _name;
    }

    public abstract void _InternalRemoveAllListenersFromSource( ListenerRegistry registry );
}

public abstract class Listener<T>: ListenerBase{

    protected List<Listen<T>> listens = new List<Listen<T>>();

    public Listener( string _name ):base(_name){
    }

    //only removes externally added listeners!
    public void _InternalReset(){
        for( int i=listens.Count; i-->0; ){
            var listen = listens[i];
            if( listen.registry == null ) continue; //internally added, skip
            listen.registry.registeredTo.Remove( this );
        }
        listens.Clear();
    }

    protected bool RegistryExists( ListenerRegistry source, T newFunction ){
        foreach( var listen in listens ){
            if( listen.registry == source && listen.value.Equals( newFunction ) ) return true;
        }
        return false;
    }
    
    public virtual void _InternalAddListener( T newFunction ){
        if( newFunction == null ) throw new System.Exception("Cannot listen with a null function");
        if( RegistryExists( null, newFunction ) ){
            // Debug.LogError( "_Internal listener "+nameof(newFunction)+" already exists. Skipping..." );
            return;
        }
        listens.Add( new Listen<T>(){ registry=null, value=newFunction } );
    }
    

    public virtual void AddListener( VivaScript source, T newFunction ){
        if( source == null ) throw new System.Exception("Cannot listen to \""+name+"\" with a null source");
        if( newFunction == null ) throw new System.Exception("Cannot listen with a null function");
        if( RegistryExists( source.registry, newFunction ) ){
            Debugger.LogWarning( "Script "+source._InternalGetScript().name+" is already listening to \""+name+"\". Skipping..." );
            return;
        }

        listens.Add( new Listen<T>(){ registry=source.registry, value=newFunction } );
        source.registry.registeredTo.Add( this );
    }

    public void RemoveListener( VivaScript source, T function ){
        if( source == null ) throw new System.Exception("Cannot remove listener from \""+name+"\" with a null source");
        if( function == null ) throw new System.Exception("Cannot remove a null function listener from \""+name+"\"");

        for( int i=listens.Count; i-->0; ){
            var listen = listens[i];
            if( listen.registry == source.registry && listen.value.Equals( function ) ){
                listens.RemoveAt(i);
                source.registry.registeredTo.Remove( this );
                return;
            }
        }
        // Debug.LogError("Could not RemoveListener");
    }

    public void _InternalRemoveListener( T function ){
        for( int i=listens.Count; i-->0; ){
            var listen = listens[i];
            if( listen.value.Equals( function ) ){
                listens.RemoveAt(i);
                return;
            }
        }
        // Debug.LogError("Could not _InternalRemoveListener");
    }

    public override void _InternalRemoveAllListenersFromSource( ListenerRegistry registry ){
        for( int i=listens.Count; i-->0; ){
            var listen = listens[i];
            if( listen.registry == registry ){
                listens.RemoveAt(i);
                registry.registeredTo.Remove( this );
            }
        }
    }
}

public delegate void NullableFloatCallback( float? value );

public class ListenerNullableFloat: Listener<NullableFloatCallback>{

    public float? currentValue { get; private set; }

    public ListenerNullableFloat( string _name ):base(_name){}

    public void Invoke( float? val ){
        currentValue = val;
        try{
            var safeCopy = listens.ToArray();
            for( int i=0; i<safeCopy.Length; i++ ) safeCopy[i].value.Invoke( val );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    
    public override void AddListener( VivaScript source, NullableFloatCallback newFunction ){
        base.AddListener( source, newFunction );
        newFunction.Invoke( currentValue );
    }
    
    public override void _InternalAddListener( NullableFloatCallback newFunction ){
        base._InternalAddListener( newFunction );
        newFunction.Invoke( currentValue );
    }
}

public delegate void DayEventCallback( DayEvent dayEvent );

public class ListenerDayEvent: Listener<DayEventCallback>{

    public DayEvent currentEvent;

    public ListenerDayEvent( string _name ):base(_name){}

    public void Invoke( DayEvent newEvent ){
        currentEvent = newEvent;
        try{
            var safeCopy = listens.ToArray();
            for( int i=0; i<safeCopy.Length; i++ ) safeCopy[i].value.Invoke( currentEvent );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    
    public override void AddListener( VivaScript source, DayEventCallback newFunction ){
        base.AddListener( source, newFunction );
        newFunction.Invoke( currentEvent );
    }
    
    public override void _InternalAddListener( DayEventCallback newFunction ){
        base._InternalAddListener( newFunction );
        newFunction.Invoke( currentEvent );
    }
}

public delegate void WaterCallback( Water water );

public class ListenerWater: Listener<WaterCallback>{

    public Water active { get; private set; }

    public ListenerWater( string _name ):base(_name){}

    public void Invoke( Water newWater ){
        active = newWater;
        try{
            var safeCopy = listens.ToArray();
            for( int i=0; i<safeCopy.Length; i++ ) safeCopy[i].value.Invoke( active );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    
    public override void AddListener( VivaScript source, WaterCallback newFunction ){
        base.AddListener( source, newFunction );
        newFunction.Invoke( active );
    }
}

public class ListenerTask: Listener<TaskCallback>{

    public ListenerTask( string _name ):base(_name){}

    public void Invoke( Task task ){
        try{
            var safeCopy = listens.ToArray();
            for( int i=0; i<safeCopy.Length; i++ ) safeCopy[i].value.Invoke( task );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
}

public class ListenerGeneric: Listener<GenericCallback>{

    public ListenerGeneric( string _name ):base(_name){}

    public void Invoke(){
        try{
            var safeCopy = listens.ToArray();
            for( int i=0; i<safeCopy.Length; i++ ) safeCopy[i].value.Invoke();
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
}

public delegate void VivaObjectReturnFunc( VivaObject obj );
public delegate bool BoolReturnVivaInstanceFunc( VivaInstance vivaInstance );

public class ListenerVivaObject: Listener<VivaObjectReturnFunc>{

    public ListenerVivaObject( string _name ):base(_name){}

    public void Invoke( VivaObject obj ){
        try{
            var safeCopy = listens.ToArray();
            for( int i=0; i<safeCopy.Length; i++ ) safeCopy[i].value.Invoke( obj );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
}

public class ListenerGrabbable: Listener<GrabbableReturnFunc>{

    private Grabbable[] onListenStash;
    private List<Grabbable> onListenStashList;

    public ListenerGrabbable( Grabbable[] _onListenStash, string _name ):base(_name){
        onListenStash = _onListenStash;
    }
    public ListenerGrabbable( List<Grabbable> _onListenStashList, string _name ):base(_name){
        onListenStashList = _onListenStashList;
    }

    public void Invoke( Grabbable grabbable ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( grabbable );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }

    public override void _InternalAddListener( GrabbableReturnFunc newFunction ){
        base._InternalAddListener( newFunction );
        FireInitialStash( newFunction );
    }

    public override void AddListener( VivaScript source, GrabbableReturnFunc newFunction ){
        base.AddListener( source, newFunction );
        FireInitialStash( newFunction );
    }
    
    private void FireInitialStash( GrabbableReturnFunc newFunction ){
        if( onListenStash != null ){
            foreach( var grabbable in onListenStash ){
                if( grabbable ) newFunction.Invoke( grabbable );
            }
        }else if( onListenStashList != null ){
            foreach( var grabbable in onListenStashList ){
                if( grabbable ) newFunction.Invoke( grabbable );
            }
        }
    }
}

public class ListenerGesture: Listener<GestureCallback>{

    public ListenerGesture( string _name ):base(_name){
    }

    public void Invoke( string gestureName, Character caller ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( gestureName, caller );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    public override void AddListener( VivaScript source, GestureCallback newFunction ){
        base.AddListener( source, newFunction );
    }
}

public delegate void RigidbodyCallback( Rigidbody rigidBody );
public class ListenerRigidBody: Listener<RigidbodyCallback>{

    public ListenerRigidBody( string _name ):base(_name){
    }

    public void Invoke( Rigidbody rigidBody ){
        for( int i=0; i<listens.Count; i++ ) listens[i].value.Invoke( rigidBody );
    }
}

public class ListenerCharacter: Listener<CharacterCallback>{

    private Character[] onListenStash;
    private List<Character> onListenStashList;
    public ListenerCharacter( Character[] _onListenStash, string _name ):base(_name){
        onListenStash = _onListenStash;
    }
    public ListenerCharacter( List<Character> _onListenStashList, string _name ):base(_name){
        onListenStashList = _onListenStashList;
    }

    public void Invoke( Character entry ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( entry );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    public override void AddListener( VivaScript source, CharacterCallback newFunction ){
        base.AddListener( source, newFunction );
        if( onListenStash != null ){
            foreach( var entry in onListenStash ){
                if( entry ) newFunction.Invoke( entry );
            }
        }else if( onListenStashList != null ){
            foreach( var entry in onListenStashList ){
                newFunction.Invoke( entry );
            }
        }
    }
}


public class ListenerString: Listener<StringCallback>{

    private List<string> onListenStash;
    public ListenerString( List<string> _onListenStash, string _name ):base(_name){
        onListenStash = _onListenStash;
    }

    public void Invoke( string entry ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( entry );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    public override void AddListener( VivaScript source, StringCallback newFunction ){
        base.AddListener( source, newFunction );
        if( onListenStash != null ){
            foreach( var entry in onListenStash ){
                newFunction.Invoke( entry );
            }
        }
    }
}

public delegate void FloatCallback( float value );

public class ListenerFloat: Listener<FloatCallback>{

    public ListenerFloat( string _name ):base(_name){
    }

    public void Invoke( float value ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( value );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
}

public delegate void StringItemCallback( Item item, Attribute value );

public class ListenerItemString: Listener<StringItemCallback>{

    private List<Attribute> onListenStash;
    private readonly Item item;
    public ListenerItemString( Item _item, List<Attribute> _onListenStash, string _name ):base(_name){
        item = _item;
        onListenStash = _onListenStash;
    }

    public void Invoke( Attribute entry ){
        if( !item ) return;
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( item, entry );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    public override void AddListener( VivaScript source, StringItemCallback newFunction ){
        base.AddListener( source, newFunction );
        if( onListenStash != null && item ){
            foreach( var entry in onListenStash ){
                newFunction.Invoke( item, entry );
            }
        }
    }
}
public delegate void StringCharacterCallback( Character character, Attribute value );

public class ListenerCharacterString: Listener<StringCharacterCallback>{

    private List<Attribute> onListenStash;
    private readonly Character character;
    public ListenerCharacterString( Character _character, List<Attribute> _onListenStash, string _name ):base(_name){
        character = _character;
        onListenStash = _onListenStash;
    }

    public void Invoke( Attribute entry ){
        if( !character ) return;
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( character, entry );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    public override void AddListener( VivaScript source, StringCharacterCallback newFunction ){
        base.AddListener( source, newFunction );
        if( onListenStash != null && character ){
            foreach( var entry in onListenStash ){
                newFunction.Invoke( character, entry );
            }
        }
    }
}

public delegate void CollisionCallback( Collision collision );
public class ListenerCollision: Listener<CollisionCallback>{

    public ListenerCollision( string _name ):base(_name){
    }

    public void Invoke( Collision collision ){
        for( int i=0; i<listens.Count; i++ ) listens[i].value.Invoke( collision );
    }
}

public delegate Attribute[] AttributesReturnCallback();
public delegate void AttributeArrayCallback( Attribute[] attributes );
public delegate void AttributeCallback( Attribute attributes );
public class ListenerAttributes: Listener<AttributeArrayCallback>{

    public ListenerAttributes( string _name ):base(_name){
    }

    public void Invoke( Attribute[] attributes ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( attributes );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
}


public class ListenerItem: Listener<ItemCallback>{

    private Item[] onListenStash;
    public ListenerItem( Item[] _onListenStash, string _name ):base(_name){
        onListenStash = _onListenStash;
    }

    private List<Item> onListenStashList;
    public ListenerItem( List<Item> _onListenStashList, string _name ):base(_name){
        onListenStashList = _onListenStashList;
    }

    public void Invoke( Item item ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( item );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    public override void AddListener( VivaScript source, ItemCallback newFunction ){
        base.AddListener( source, newFunction );
        if( onListenStash != null ){
            foreach( var item in onListenStash ){
                if( item ) newFunction.Invoke( item );
            }
        }else if( onListenStashList != null ){
            foreach( var item in onListenStashList ){
                if( item ) newFunction.Invoke( item );
            }
        }
    }

    public override void _InternalAddListener( ItemCallback newFunction ){
        base._InternalAddListener( newFunction );
        if( onListenStash != null ){
            foreach( var item in onListenStash ){
                if( item ) newFunction.Invoke( item );
            }
        }else if( onListenStashList != null ){
            foreach( var item in onListenStashList ){
                if( item ) newFunction.Invoke( item );
            }
        }
    }
}

public delegate void AnimalCollisionCallback( string source, Collision collision );

public class ListenerAnimalCollision: Listener<AnimalCollisionCallback>{

    public ListenerAnimalCollision( string _name ):base(_name){
    }

    public void Invoke( string source, Collision collision ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( source, collision );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
}

public delegate void BipedCollisionCallback( BipedBone source, Collision collision );

public class ListenerBipedCollision: Listener<BipedCollisionCallback>{

    public ListenerBipedCollision( string _name ):base(_name){
    }

    public void Invoke( BipedBone source, Collision collision ){
        try{
            var safeCopy = listens.ToArray();
            foreach( var listen in safeCopy) listen.value.Invoke( source, collision );
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
}

public delegate bool ItemValidateCallback( Item item );
public class ListenerItemValidate: Listener<ItemValidateCallback>{

    public ListenerItemValidate( string _name ):base(_name){
    }

    public bool Invoke( Item item ){
        var validated = false;
        try{
            var safeCopy = listens.ToArray();
            for( int i=0; i<safeCopy.Length; i++ ){
                validated |= safeCopy[i].value.Invoke( item );
            }
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
        return validated;
    }
}

public class ListenerOnGrabContext: Listener<GrabContextReturnFunc>{

    private List<GrabContext> onListenStash;
    private readonly bool execOnlyIfStashContained; //SKIPS FIRST LISTENER (to allow adding to stash)

    public ListenerOnGrabContext( List<GrabContext> _onListenStash, string _name, bool _execOnlyIfStashContained ):base(_name){
        onListenStash = _onListenStash;
        execOnlyIfStashContained = _execOnlyIfStashContained;
    }

    public void Invoke( GrabContext grabContext ){
        try{
            //duplicate list so things are modified at the end of an invoke
            var safeCopy = listens.ToArray();
            for( int i=0; i<safeCopy.Length; i++ ){
                if( i>0 && execOnlyIfStashContained && !onListenStash.Contains( grabContext ) ) return;
                safeCopy[i].value.Invoke( grabContext );
            }
        }catch( System.Exception e ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Error in listener \""+name+"\"", e.ToString() );
        }
    }
    public override void AddListener( VivaScript source, GrabContextReturnFunc newFunction ){
        base.AddListener( source, newFunction );
        if( onListenStash != null ){
            foreach( var grabContext in onListenStash ){
                newFunction.Invoke( grabContext );
            }
        }
    }
}

}