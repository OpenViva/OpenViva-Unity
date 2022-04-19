using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{

public class GameSettings : VivaSessionAsset {

	[SerializeField]
	private float m_mouseSensitivity = 70.0f;
    [VivaFileAttribute]
    public float mouseSensitivity { get{ return m_mouseSensitivity; } protected set{ m_mouseSensitivity = value; } }
	[SerializeField]
	private float m_musicVolume = 0.0f;
    [VivaFileAttribute]
    public float musicVolume { get{ return m_musicVolume; } protected set{ m_musicVolume = value; } }
	[SerializeField]
    private float m_worldTime = 0.0f;
    [VivaFileAttribute]
    public float worldTime { get{return m_worldTime;} protected set{ m_worldTime = value; } }
	[SerializeField]
    private int m_dayNightCycleSpeedIndex = 0;
    [VivaFileAttribute]
    public int dayNightCycleSpeedIndex { get{return m_dayNightCycleSpeedIndex;} protected set{ m_dayNightCycleSpeedIndex = value; } }
    [SerializeField]
    private bool m_disableGrabToggle = false;
    [VivaFileAttribute]
    public bool disableGrabToggle { get{return m_disableGrabToggle;} protected set{ m_disableGrabToggle = value; } }
    [SerializeField]
    private bool m_pressToTurn = true;
    [VivaFileAttribute]
    public bool pressToTurn { get{return m_pressToTurn;} protected set{ m_pressToTurn = value; } }
    [SerializeField]
    private Player.VRControlType m_vrControls = Player.VRControlType.TRACKPAD;
    [VivaFileAttribute]
    public Player.VRControlType vrControls { get{return m_vrControls;} protected set{ m_vrControls = value; } }
    [SerializeField]
    private bool m_trackpadMovementUseRight = false;
    [VivaFileAttribute]
    public bool trackpadMovementUseRight { get{return m_trackpadMovementUseRight;} protected set{ m_trackpadMovementUseRight = value; } }
	[SerializeField]
    private int m_antiAliasing = 2;
    [VivaFileAttribute]
    public int antiAliasing { get{return m_antiAliasing;} protected set{ m_antiAliasing = value; } }


    private string[] dayNightCycleSpeedDesc = new string[]{
        "12 minutes",
        "24 minutes",
        "40 minutes",
        "2 hour",
        "Never Change"
    };

	public void AdjustMouseSensitivity( float direction ){
		SetMouseSensitivity( mouseSensitivity+direction );
	}
    public void SetMouseSensitivity( float amount ){
		m_mouseSensitivity = Mathf.Clamp( amount, 10.0f, 250.0f );
	}
    public void AdjustMusicVolume( float direction ){
        SetMusicVolume( musicVolume+direction );
        GameDirector.instance.UpdateMusicVolume();
	}
    public void SetMusicVolume( float percent ){
		m_musicVolume = Mathf.Clamp01( percent );
	}
    public void ShiftWorldTime( float timeAmount ){
        m_worldTime += timeAmount;
        GameDirector.skyDirector.ApplyDayNightCycle();
    }
    public void SetWorldTime( float newTime ){
		m_worldTime = newTime;
        GameDirector.skyDirector.ApplyDayNightCycle();
	}
    public string AdjustDayTimeSpeedIndex( int direction ){
		SetDayNightCycleSpeedIndex( dayNightCycleSpeedIndex+direction );
		GameDirector.skyDirector.UpdateDayNightCycleSpeed();
        return dayNightCycleSpeedDesc[ dayNightCycleSpeedIndex ];
    }
	public void SetDayNightCycleSpeedIndex( int index ){
        m_dayNightCycleSpeedIndex = Mathf.Clamp( index, 0, dayNightCycleSpeedDesc.Length-1 );
	}
	public void ToggleDisableGrabToggle(){
        m_disableGrabToggle = !m_disableGrabToggle;
	}
	public void TogglePresstoTurn(){
        m_pressToTurn = !m_pressToTurn;
	}
	public void SetVRControls( Player.VRControlType newVRControls ){
		m_vrControls = newVRControls;
	}
	public void ToggleTrackpadMovementUseRight(){
		m_trackpadMovementUseRight = !m_trackpadMovementUseRight;
	}
    public void CycleAntiAliasing(){
        switch( antiAliasing ){
        case 0:
            m_antiAliasing = 2;
            break;
        case 2:
            m_antiAliasing = 4;
            break;
        case 4:
            m_antiAliasing = 8;
            break;
        default:
            m_antiAliasing = 0;
            break;
        }
        GameDirector.instance.ApplyAntiAliasingSettings();
    }

    public void CycleQualitySetting(){
        int currQuality = (int)QualitySettings.GetQualityLevel();
        currQuality = (currQuality+1)%5;
        QualitySettings.SetQualityLevel( currQuality );
        GameDirector.instance.ApplyAntiAliasingSettings();
    }

    public void SelectBestVRControllerSetup(){

		//setup VR settings based on VR device
        Debug.Log("[Settings] Selected Index device");
        m_pressToTurn = false;
        m_disableGrabToggle = true;
        if( GameDirector.player ){
            Vector3 position = new Vector3( -0.000290893f,0.04441151f,-0.1133812f );
            Vector3 euler = new Vector3( 16.92607f,108.1964f,154.5403f );
            GameDirector.player.rightPlayerHandState.SetAbsoluteVROffsets( position, euler, true );
            GameDirector.player.leftPlayerHandState.SetAbsoluteVROffsets( position, euler, true );
		}
    }
}

}