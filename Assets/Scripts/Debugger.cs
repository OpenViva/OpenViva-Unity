using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class Debugger : MonoBehaviour{
    
    private static List<string> lines = new List<string>();
    
    [SerializeField]
    private Text text;

    private static int maxLines = 25;

    private void Awake(){
        AddText( "-----------------------------------" );
        AddText( "You can use                                     " );
        AddText( "Debugger.Log();                                 " );
        AddText( "<color=#ffff00>Debugger.LogWarning();</color>   " );
        AddText( "<color=#ff0000>Debugger.LogError();</color>" );
        maxLines = Mathf.FloorToInt( Screen.height/text.fontSize )-1;
        
    }

    public static void Log( string message ){
        AddText( message );
#if UNITY_EDITOR
        Debug.Log( message );
#endif
    }

    public static void LogError( string message ){
        AddText( "<color=#ff0000>"+message+"</color>" );
#if UNITY_EDITOR
        Debug.LogError( message );
#endif
    }

    public static void LogWarning( string message ){
        AddText( "<color=#ffff00>"+message+"</color>" );
#if UNITY_EDITOR
        Debug.LogWarning( message );
#endif
    }

    public static void _InternalReset(){
        if( GameUI.main == null ) return;

        var main = GameUI.main.debugger;
        main.text.text = "";
        lines.Clear();
    }

    private static void AddText( string message ){
        if( GameUI.main == null ) return;
        
        message += "\n";
        var main = GameUI.main.debugger;

        string oldLine = null;
        if( lines.Count >= maxLines && lines.Count > 0 ){
            bool found = false;
            for( int i=0; i<1; i++ ){
                var line = lines[i];

                if( line.EndsWith( message ) ){
                    if( line.Length > message.Length ){
                        found = true;
                        var words = line.Split( new char[]{'x'}, 2, System.StringSplitOptions.None );
                        var count = System.Int32.Parse( words[0] )+1;
                        lines[i] = ""+count+"x"+words[1];
                        break;
                    }else{
                        message = "2x: "+message;
                        break;
                    }
                }
            }
            if( !found ){
                oldLine = lines[0];
                lines.RemoveAt(0);
                lines.Add( message );
                main.text.text = message+main.text.text.Substring( 0, Mathf.Clamp( main.text.text.Length-oldLine.Length, 0, main.text.text.Length) );
            }else{
                main.text.text = string.Join( "", lines );
            }
        }else{
            lines.Add( message );
            main.text.text = message+main.text.text;
        }
    }
}

}