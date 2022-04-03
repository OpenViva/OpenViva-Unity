using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Security.Permissions;



namespace viva{

public class FileWatcherManager: MonoBehaviour{

    public class FileWatcher{
        
        public FileSystemWatcher watcher = new FileSystemWatcher();
        public StringCallback onFileChange;
        public bool alwaysOn = false;
        public List<string> filepathChanges = new List<string>();

        public FileWatcher( string directory ){
            watcher.Path = directory;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*";
            watcher.Changed += delegate( object source, FileSystemEventArgs e ){
                filepathChanges.Add( e.FullPath );
                if( !FileWatcherManager.changed.Contains( this ) ){
                    FileWatcherManager.changed.Add( this );
                }
            };

            watcher.EnableRaisingEvents = true;
            // Debug.Log("Watching: "+directory);
        }
    }

    private static List<FileWatcher> fileWatchers = new List<FileWatcher>();
    private static List<FileWatcher> changed = new List<FileWatcher>();
    public static List<string> ignoreChanges = new List<string>();
    public static FileWatcherManager main;


    public void Awake(){
        main = this;
    }

    public FileWatcher GetFileWatcher( string directory ){
        directory = Path.GetFullPath( directory ); //standardize
        FileWatcher fileWatcher = null;
        foreach( var candidate in fileWatchers ){
            if( candidate.watcher.Path == directory ){
                fileWatcher = candidate;
                break;
            }
        }
        
        if( fileWatcher == null ){
            try{
                fileWatcher = new FileWatcher( directory );
                fileWatchers.Add( fileWatcher );
            }catch{
                Debug.LogError( "Could not watch: "+directory );
            }
        }
        return fileWatcher;
    }

    public void StopWatching(){
        for( int i=fileWatchers.Count; i-->0; ){
            var fileWatcher = fileWatchers[i];
            if( fileWatcher.alwaysOn ) continue;
            Debug.Log("Stopped watching "+fileWatcher.watcher.Path);
            fileWatcher.watcher.Dispose();
            fileWatchers.RemoveAt(i);
        }
    }

    private void FixedUpdate(){
        if( changed.Count > 0 ){
            bool reloaded = false;
            foreach( var fileWatcher in changed ){
                foreach( var filepathChange in fileWatcher.filepathChanges ){
                    if( ignoreChanges.Contains( filepathChange ) ){
                        ignoreChanges.Remove( filepathChange );
                        continue;
                    }
                    fileWatcher.onFileChange?.Invoke( filepathChange );
                }
                fileWatcher.filepathChanges.Clear();
            }
            if( reloaded ) Sound.main.PlayGlobalUISound( UISound.RELOADED );
            
            changed.Clear();
        }
    }
}

}