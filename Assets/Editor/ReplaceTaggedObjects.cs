    using UnityEngine;
    using UnityEditor;
    using System.Collections;

public class ReplaceTaggedObjects : ScriptableWizard {

	public GameObject NewType;
	public GameObject[] OldObjects;
	
	[MenuItem("Tools/Replace Tagged Objects")]
	
	
	public static void CreateWizard() {
		ScriptableWizard.DisplayWizard("Replace Tagged Objects", typeof(ReplaceTaggedObjects), "Replace");
	}
	
	public void OnWizardCreate() {

		foreach (GameObject go in OldObjects) {
			GameObject newObject;
			newObject = (GameObject)EditorUtility.InstantiatePrefab(NewType);
			newObject.transform.position = go.transform.position;
			newObject.transform.rotation = go.transform.rotation;
			newObject.transform.parent = go.transform.parent;

			DestroyImmediate(go);
		}
	}
}
