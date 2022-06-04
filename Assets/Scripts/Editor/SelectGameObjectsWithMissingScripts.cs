using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectGameObjectsWithMissingScripts : Editor
{
    [MenuItem("Tools/Find Missing Scripts")]
    static void SelectGameObjects()
    {
        //Get the current scene and all top-level GameObjects in the scene hierarchy
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();

        List<Object> objectsWithDeadLinks = new List<Object>();
        foreach (GameObject g in rootObjects)
        {
            //Get all components on the GameObject, then loop through them 
            Component[] components = g.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component currentComponent = components[i];

                //If the component is null, that means it's a missing script!
                if (currentComponent == null)
                {
                    //Add the sinner to our naughty-list
                    objectsWithDeadLinks.Add(g);
                    Selection.activeGameObject = g;
                    Debug.Log(g + " has a missing script!");
                    break;
                }
            }
        }
        if (objectsWithDeadLinks.Count > 0)
        {
            //Set the selection in the editor
            Selection.objects = objectsWithDeadLinks.ToArray();
        }
        else
        {
            Debug.Log("No GameObjects in '" + currentScene.name + "' have missing scripts! Yay!");
        }
    }
}