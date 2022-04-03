using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;



[CreateAssetMenu(fileName = "list", menuName = "VivaEditable/Built-In VivaObjectList", order = 1)]
public class BuiltInVivaObjectList : ScriptableObject{
    public List<BuiltInVivaObject> objects = new List<BuiltInVivaObject>();
}
