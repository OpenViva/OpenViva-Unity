using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace viva{


public partial class GameDirector : MonoBehaviour {

    public void ApplyAllQualitySettings(){
        int currQuality = QualitySettings.GetQualityLevel();
        GameDirector.instance.RebuildCloudRendering();
        bool enableRealtimeReflections = currQuality >= 1;
        float refreshTimeout = currQuality>=2? 0:1;
        float maxRefreshTimeout = currQuality>=2? 0:8;
        int resolution = currQuality<=1? 16:64;

        player.realtimeReflectionController.enabled = enableRealtimeReflections;
        player.realtimeReflectionController.refreshTimeout = refreshTimeout;
        player.realtimeReflectionController.maxRefreshTimeout = maxRefreshTimeout;
        player.realtimeReflectionController.reflectionProbe.resolution = resolution;

        ApplyAntiAliasingSettings();
    }
    
    public void ApplyAntiAliasingSettings(){
        QualitySettings.antiAliasing = settings.antiAliasing;
    }
}

}