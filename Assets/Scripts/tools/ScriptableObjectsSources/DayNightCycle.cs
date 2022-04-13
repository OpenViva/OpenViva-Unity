using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


[System.Serializable]
[CreateAssetMenu(fileName = "Day Night Cycle", menuName = "Day Night Cycle", order = 1)]
public class DayNightCycle: ScriptableObject{

    [System.Serializable]
    public class Phase{

        public Color ambience = Color.black;
        public Color sunColor = Color.white;
        public float atmosphereThickness = 1.5f;
        public Color toonAmbience = Color.black;
        public float exposure = 1.0f;
        public Color skyTint = Color.white;
        public Color nightSky = Color.black;
        public Color cloudColorA = Color.grey;
        public Color cloudColorB = Color.white;
        public Cubemap environmentMap;
        public Color fogColor;
    }

    [SerializeField]
    public Phase[] phases = new Phase[10];
}

}