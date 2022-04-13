using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public partial class GameDirector : MonoBehaviour {

	public delegate void OnVivaFileCallback();

	public static GameDirector instance;
	public static SkyDirector skyDirector;
	public static GameSettings settings;
    public static LampDirector lampDirector;
	public static Transform utilityTransform;
	private static Set<DynamicBone> m_dynamicBones = new Set<DynamicBone>();
	public static Set<DynamicBone> dynamicBones { get{ return m_dynamicBones; } }
	private static Set<Mechanism> m_mechanisms = new Set<Mechanism>();
	public static Set<Mechanism> mechanisms { get{ return m_mechanisms; } }
	private static Set<Item> m_items = new Set<Item>();
	public static Set<Item> items { get{ return m_items; } }

    [SerializeField]
    private GamePostProcessing m_postProcessing;
	public GamePostProcessing postProcessing { get{ return m_postProcessing;} }
	[SerializeField]
	private ParticleSystem m_waterSplashFX;
	[SerializeField]
	private Camera m_utilityCamera;
	public Camera utilityCamera { get{ return m_utilityCamera; } }
	[SerializeField]
	private AmbienceDirector m_ambienceDirector;
	public AmbienceDirector ambienceDirector { get{ return m_ambienceDirector; } }
	[SerializeField]
	private SkyDirector m_skyDirector;
    [SerializeField]
    private LampDirector m_lampDirector;
	[SerializeField]
	private string m_languageName = "english";
	[VivaFileAttribute]
    public string languageName { get{ return m_languageName; } protected set{ m_languageName = value; } }
	private Language m_language = null;
	public Language language { get{ return m_language; } }
	[SerializeField]
	private Transform m_helperIndicator;
	public Transform helperIndicator { get{ return m_helperIndicator; } }
	[SerializeField]
	private Town m_town;
	public Town town { get{ return m_town; } }

	public Camera mainCamera { get; private set; }
	private OnVivaFileCallback onFinishLoadingVivaFile;
	public bool physicsFrame { get; private set; } = false;

	
	public void AddOnFinishLoadingCallback( OnVivaFileCallback callback ){
		onFinishLoadingVivaFile -= callback;
		onFinishLoadingVivaFile += callback;
	}

	public void SplashWaterFXAt( Vector3 pos, Quaternion rot, float size, float startSpeed, int num ){

		var main = m_waterSplashFX.main;
		main.startSize = new ParticleSystem.MinMaxCurve( 0.5f*size, size );
		main.startSpeed = new ParticleSystem.MinMaxCurve( startSpeed*0.5f, startSpeed );
		m_waterSplashFX.transform.position = pos;
		m_waterSplashFX.transform.rotation = rot;
		m_waterSplashFX.Emit(num);
	}

	private void Awake(){
		Debug.Log("[GameDirector] Awake");
		instance = this;
		skyDirector = m_skyDirector;
		lampDirector = m_lampDirector;
		settings = m_settings;
		utilityTransform = new GameObject("UTILITY").transform;
		player = m_player;
		mainCamera = Camera.main;	//cache for usage

		if( m_player ){
			characters.Add( m_player );
		}
		SetEnableCursor( false );
	}

	private void Start(){
		onFinishLoadingVivaFile += OnPostLoadVivaFile;
		AttemptLoadVivaFile();
		Loli.GenerateAnimations();
	}
	
	private void FixedUpdate(){
		
		physicsFrame = true;
		for( int i=0; i<mechanisms.objects.Count; i++ ){
			m_mechanisms.objects[i].OnMechanismFixedUpdate();
		}
		for( int i=0; i<m_characters.objects.Count; i++ ){
			m_characters.objects[i].OnCharacterFixedUpdate();
		}
		foreach( DynamicBone db in m_dynamicBones.objects ){
			db.StepPhysics();
		}
	}

	private void Update () {
		for( int i=0; i<mechanisms.objects.Count; i++ ){
			m_mechanisms.objects[i].OnMechanismUpdate();
		}
		for( int i=0; i<m_characters.objects.Count; i++ ){
			m_characters.objects[i].OnCharacterUpdate();
		}
	}

	private void LateUpdate(){
		for( int i=0; i<mechanisms.objects.Count; i++ ){
			m_mechanisms.objects[i].OnMechanismLateUpdate();
		}
		for( int i=0; i<m_items.objects.Count; i++ ){
			m_items.objects[i].OnItemLateUpdate();
		}
		for( int i=m_characters.objects.Count; i-->0; ){	//fix Loli IK running first
			m_characters.objects[i].OnCharacterLateUpdatePostIK();
		}
		for( int i=0; i<m_items.objects.Count; i++ ){
			m_items.objects[i].OnItemLateUpdatePostIK();	//items postIK always goes after characters postIK
		}
		for( int i=0; i<m_items.objects.Count; i++ ){
			m_items.objects[i].OnItemFixedUpdate();	//moved to lateupdate so it runs every frame, item bug keeps making them fly away when dropped?
		}

		LateUpdateWeatherRendering();

		physicsFrame = false;
	}
}

}