using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEditor;
using UnityEngine;

namespace viva{

[CustomEditor(typeof(VivaMenuButton),true)]
public class VivaMenuButtonEditor : Editor{

    public override void OnInspectorGUI(){
    
        base.OnInspectorGUI();
    }
}

}