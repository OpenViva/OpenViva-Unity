using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

public class BulkAnimationCurveCopy : EditorWindow
{
	public AnimationClip[] sources = new AnimationClip[0];
	public AnimationClip[] targets = new AnimationClip[0];
	public AnimationClip apose;
	[MenuItem("Window/Animation Event Copier")]
	static void Init(){
		GetWindow(typeof(BulkAnimationCurveCopy));
	}
	public static void ShowWindow(){

		GetWindow<BulkAnimationCurveCopy>();
	}

	Vector2 scrollPos;
	public void OnGUI(){
		ScriptableObject target = this;
		SerializedObject sObj = new SerializedObject( target );
		scrollPos = EditorGUILayout.BeginScrollView(scrollPos,
				GUILayout.Width(EditorGUIUtility.currentViewWidth),
				GUILayout.Height(200));
		showSourceDragDrop();
		EditorGUILayout.PropertyField( sObj.FindProperty("sources"), true );
		EditorGUILayout.PropertyField( sObj.FindProperty("targets"), true );
		EditorGUILayout.EndScrollView();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Clear All",GUILayout.Width(70))){
			sources = new AnimationClip[0];
			targets = new AnimationClip[0];
		}
		if (GUILayout.Button("Execute",GUILayout.Width(70))){
			copyAnimations();
		}
		GUILayout.EndHorizontal();
		if (GUILayout.Button("Add Missing shapeKeys",GUILayout.Width(150))){
			fixMissingShapeKeys();
		}
		if (GUILayout.Button("Copy Animation Events",GUILayout.Width(150))){
			copyAnimationEvents();
		}
		EditorGUILayout.PropertyField( sObj.FindProperty("apose"), false );
		if (GUILayout.Button("Set Additive Reference Pose",GUILayout.Width(200))){
			setAdditiveReferencePose();
		}
		sObj.ApplyModifiedProperties();
	}

	void setAdditiveReferencePose(){
		if( targets.Length == 0 ){
			Debug.Log("No targets");
			return;
		}
		if( apose == null ){
			Debug.Log("apose is not set yet!");
			return;
		}
		for( int i=0; i<targets.Length; i++ ){
			AnimationClip target = targets[i];
			AnimationUtility.SetAdditiveReferencePose( target, apose, 0.0f );
		}
		Debug.Log("Set reference poses for "+targets.Length+" animations");
	}

	void fixMissingShapeKeys(){
		if( targets.Length == 0 ){
			Debug.Log("No targets");
			return;
		}
		string[] ensureExist = { "blendShape.platysma", "blendShape.centerLip" };
		for( int i=0; i<targets.Length; i++ ){
			AnimationClip target = targets[i];
			AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves( target, true );

			for( int j=0; j<ensureExist.Length; j++ ){
				bool exists = false;
				for( int k=0; k<curveDatas.Length; k++ ){

					AnimationClipCurveData curveData = curveDatas[k];
					if( curveData.path == "body" && curveData.propertyName == ensureExist[j] ){
						exists = true;
					}
				}
				if( !exists ){
					Debug.Log( "Added "+ensureExist[j] );
					EditorCurveBinding ecb = new EditorCurveBinding();
					ecb.path = "body";
					ecb.propertyName = ensureExist[j];

					Keyframe[] keys = new Keyframe[1];
					keys[0] = new Keyframe(0,0);
					AnimationCurve curve = new AnimationCurve( keys );
					curve.preWrapMode = WrapMode.PingPong;
					curve.postWrapMode = WrapMode.PingPong;
					AnimationUtility.SetEditorCurve(
						target,
						"body",
						typeof(SkinnedMeshRenderer),
						ensureExist[j],
						curve
					);
				}
			}
		}
	}

	void showSourceDragDrop(){
		Event evt = Event.current;
        Rect drop_area = GUILayoutUtility.GetRect (0.0f, 50.0f, GUILayout.ExpandWidth (true));
        GUI.Box (drop_area, "Drop .fbx sources");
		switch (evt.type) {
        case EventType.DragUpdated:
        case EventType.DragPerform:
            if (!drop_area.Contains (evt.mousePosition))
                return;
             
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
         
            if (evt.type == EventType.DragPerform) {
                DragAndDrop.AcceptDrag ();

				List<AnimationClip> newClips = new List<AnimationClip>();
                foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences) {
					string assetPath = AssetDatabase.GetAssetPath( dragged_object.GetInstanceID());
					AnimationClip asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(AnimationClip)) as AnimationClip;
					if( asset != null ){
						asset.name = dragged_object.name;
						newClips.Add(asset);
					}
                }
				sources = newClips.ToArray();
            }
            break;
        }
	}
	void copyAnimationEvents(){
		Debug.Log("______ANIMATION EVENT COPIER______");
		if( sources.Length != 1 ){
			Debug.Log("ERROR sources must be 1 for copying animation events.");
			return;
		}
		if( targets.Length < 1 ){
			Debug.Log("Targets must be at least 1");
			return;
		}
		
		AnimationClip sourceAnimClip = sources[0] as AnimationClip;
		for( int i=0; i<targets.Length; i++ ){
			AnimationClip target = targets[i];
			AnimationClip targetAnimClip = target as AnimationClip;
            AnimationUtility.SetAnimationEvents(targetAnimClip, AnimationUtility.GetAnimationEvents(sourceAnimClip));
		}
		Debug.Log("Copied animation events to "+targets.Length+" animation clips!");
	}
	void copyAnimations(){
		Debug.Log("______ANIMATION COPIER______");
		//ensure we have a 1-1 for each
		int matchCount = 0;
		AnimationClip[] matches = new AnimationClip[ targets.Length ];
		for( int i=0; i<targets.Length; i++ ){
			AnimationClip target = targets[i];
			foreach( AnimationClip source in sources ){
				if( target.name == source.name ){
					matchCount++;
					matches[i] = source;
					break;
				}
				if( target.name == source.name+"_no_events" ){
					matchCount++;
					matches[i] = source;
					break;
				}
			}
		}
		List<AnimationClip> missingTargets = new List<AnimationClip>();
		foreach( AnimationClip source in sources ){
			bool found = false;
			foreach( AnimationClip target in targets ){
				if( target.name == source.name || target.name == source.name+"_no_events" ){
					found = true;
					break;
				}
			}
			if( !found ){
				missingTargets.Add( source );
			}
		}
		if( matchCount != matches.Length || missingTargets.Count != 0 ){
			for( int i=0; i<matches.Length; i++ ){
				if( matches[i] == null ){
					Debug.Log("Missing #source for ["+targets[i].name+"]");
				}
			}
			foreach( AnimationClip missingTarget in missingTargets ){
				Debug.Log("Missing #target for ["+missingTarget.name+"]");
			}
		}else{
			for( int i=0; i<matches.Length; i++ ){
				AnimationClip target = targets[i];
				AnimationClip match = matches[i]; 
				AnimationClipCurveData[] curveDatas = AnimationUtility.GetAllCurves( match, true );
				for( int j=0; j<curveDatas.Length; j++ ){
					AnimationUtility.SetEditorCurve(
						target,
						curveDatas[j].path,
						curveDatas[j].type,
						curveDatas[j].propertyName,
						curveDatas[j].curve
					);
				}
			}
			Debug.Log("Copied over "+targets.Length+" animations!");
		}
	}
}