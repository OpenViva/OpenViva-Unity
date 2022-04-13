using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public class Liquid : Item {

	private static readonly int inCupMeshVertCount = 17;
	private static readonly int maxSpills = 3;
	private static readonly int maxRingsPerSpill = 14;
	private static readonly int perSpillRingVertCount = 2;
	private static readonly float ringSpawnRate = 16.0f;

	private static readonly int totalMeshVertexCount = inCupMeshVertCount + maxSpills*maxRingsPerSpill*perSpillRingVertCount+2*3;

	public class Spill{

		public Vector3[] worldRingPos = new Vector3[maxRingsPerSpill];
		public Vector3[] worldRingVel = new Vector3[maxRingsPerSpill];
		public bool[] worldRingFalling = new bool[maxRingsPerSpill];
		public int active = 0;
		public float ringsActive = 0.0f;
	}

	[SerializeField]
	private MeshFilter meshFilter;
	[SerializeField]
	private MeshRenderer meshRenderer;

	private Mesh mesh;
	
	private Spill[] spills = new Spill[]{new Spill(), new Spill(), new Spill()};
	private int connectedSpillIndex = -1;
	private int oldConnectedSpillIndex = -1;

	//mesh variables
	private Vector3[] meshVertices = new Vector3[totalMeshVertexCount];
	private List<Vector2> meshUVs = new List<Vector2>();
	private int[] inCupMeshIndices = new int[(inCupMeshVertCount-1)*3 ];
	private List<Vector3> meshNormals = new List<Vector3>();
	[SerializeField]
	private Material[] meshMaterials = new Material[2]; 
	private int[] allSpillMeshIndices = null;

	private readonly float cupRadiansStep = (1.0f/(inCupMeshVertCount-1))*Mathf.PI*2.0f;
	private int cupExitOutIndexA;
	private int cupExitOutIndexB;
	private int cupExitInIndexA;
	private int cupExitInIndexB;
	private Vector3 cupAcceleration = Vector3.zero;
	private Vector3 lastCupVelocity = Vector3.zero;
	private Vector3 lastCupPos = Vector3.zero;
	private Vector3 liquidNormal = Vector3.up;
	private Vector3 liquidVelocity = Vector3.zero;
	[SerializeField]
	private float fill = 0.8f;
	[SerializeField]
	private float bottomRadius = 0.01f;
	[SerializeField]
	private float topRadius = 0.03f;
	private float damping = 1.0f;
	[SerializeField]
	private float maxLiquidHeight = 0.1f;
	private float fillLostVel = 0.0f;
	private bool wasSpilling = false;
	private float ignoreExternalForcesTimer = 0.0f;

	[SerializeField]
	private ParticleSystem splashSystem;
	private ParticleSystem.EmitParams splashParams;

	private void Start () {
		
		lastCupPos = transform.position;
		int triIndex = 0;
		for( int j=inCupMeshVertCount-1, i=1; i<inCupMeshVertCount; j=i++ ){
			inCupMeshIndices[triIndex++] = j;
			inCupMeshIndices[triIndex++] = 0;
			inCupMeshIndices[triIndex++] = i;
		}
		
		for( int i=0; i<meshVertices.Length; i++ ){
			meshNormals.Add(Vector3.up);
			meshUVs.Add(Vector2.zero);
		}
		
		mesh = new Mesh();
		mesh.subMeshCount = 2;	//1 for in cup mesh and 1 for all spill meshes
		meshFilter.mesh = mesh;
		
		meshRenderer.materials = meshMaterials;
		splashParams = new ParticleSystem.EmitParams();
		enabled = false;
	}

	private void updateExternalForces(){
		
		if( ignoreExternalForcesTimer < 0.0f ){
			Vector3 currCupVelocity = (transform.position-lastCupPos)*Time.deltaTime;
			cupAcceleration = (currCupVelocity-lastCupVelocity)/Time.deltaTime;
			liquidVelocity -= transform.InverseTransformDirection( cupAcceleration );
			lastCupPos = transform.position;
			lastCupVelocity = currCupVelocity;
		}else{
			ignoreExternalForcesTimer -= Time.deltaTime;
			cupAcceleration = Vector3.zero;
		}
		Vector3 targetNormal = Quaternion.Inverse( transform.rotation )*Vector3.up;

		float velMag = liquidVelocity.magnitude;
		damping += ( (1.0f-velMag)-damping )*Time.deltaTime*1.0f;
		liquidVelocity += (targetNormal-liquidNormal)*Time.deltaTime*4.0f*damping;

		liquidVelocity *= damping;
		liquidNormal += liquidVelocity;
		liquidNormal.y = Mathf.Max(0.0f, liquidNormal.y);
		liquidNormal = liquidNormal.normalized;
	}
	
	public override void OnItemLateUpdate(){
		
		meshMaterials[0].SetFloat("_BottomRadius",bottomRadius);
		meshMaterials[0].SetFloat("_GrowRadius",topRadius-bottomRadius);
		meshMaterials[0].SetFloat("_MaxHeight",maxLiquidHeight);
		
		updateExternalForces();

		bool isSpilling = recalculateLiquidMesh();
		if( isSpilling != wasSpilling ){
			wasSpilling = isSpilling;
			//find available spill to activate
			if( isSpilling ){
				
				oldConnectedSpillIndex = ++oldConnectedSpillIndex%maxSpills;
				connectedSpillIndex = oldConnectedSpillIndex;
				Spill spill = spills[connectedSpillIndex];
				spill.active = 1;
				spill.ringsActive = 0.95f;
			}else{
				connectedSpillIndex = -1;
			}
		}
		//calculate bottom exit point
		if( isSpilling ){
			
			//spawn splash particles if fast enough
			if( Vector3.SqrMagnitude( cupAcceleration ) > 0.02f ){
				splashParams.position = transform.position+transform.up*( maxLiquidHeight )-Vector3.up*0.01f;
				splashParams.startSize = 0.1f+Random.value*0.3f;
				splashParams.rotation = 360.0f*Random.value;
				splashParams.velocity = transform.up;
				splashSystem.Emit( splashParams, 1 );
			}
			//drain proportional to radius and exitRing width
			fillLostVel = (1.0f+fill)*Time.deltaTime*0.2f*(1.3f-Mathf.Abs(transform.up.y));
		}
		//calculate all rings
		int spillMeshVertIndex = inCupMeshVertCount;	//start from inCup vert count
		int rings = 0;
		for( int spillIndex=0; spillIndex<spills.Length; spillIndex++ ){
			Spill spill = spills[ spillIndex ];
			if( spill.active == 0 ){
				continue;
			}
			if( connectedSpillIndex == spillIndex ){
				
				int oldSpillActiveCount = (int)spill.ringsActive;
				spill.ringsActive += Time.deltaTime*ringSpawnRate;

				//increment spill ring index
				if( oldSpillActiveCount != (int)spill.ringsActive ){
					int spawnNewIndex = oldSpillActiveCount%maxRingsPerSpill;
					spill.worldRingPos[ spawnNewIndex ] = transform.position+transform.up*( maxLiquidHeight )-Vector3.up*0.01f;
					spill.worldRingVel[ spawnNewIndex ] = transform.TransformDirection( Vector3.up )*0.01f-cupAcceleration;
	
					spill.worldRingFalling[ spawnNewIndex ] = true;
					spill.active++;
				}
				rings += (int)Mathf.Min( spill.ringsActive, maxRingsPerSpill );
			}else{
				rings += (int)Mathf.Max(Mathf.Min( spill.ringsActive, maxRingsPerSpill )-1.0f, 0.0f );
			}
		}
		allSpillMeshIndices = new int[ rings*3*2*3 ];

		Vector3 worldForward = transform.up;
		Vector3 worldRight;
		if( transform.up.y > 0.0f ){
			worldRight = Vector3.Cross( worldForward, Vector3.up );
		}else{
			worldRight = Vector3.Cross( worldForward, -Vector3.up );
		}
		worldForward *= 0.01f;
		int spillMeshIndicesIndex = 0;
		for( int spillIndex=0; spillIndex<spills.Length; spillIndex++ ){

			Spill spill = spills[ spillIndex ];
			if( spill.active == 0 ){
				continue;
			}
			Vector2 uv = Vector2.one;
			if( connectedSpillIndex == spillIndex ){	//create connected to cup ring
				
				meshNormals[spillMeshVertIndex] = liquidNormal;
				uv.x = 0.0f;
				meshUVs[spillMeshVertIndex] = uv;
				meshVertices[spillMeshVertIndex++] = calculateCupExitPlaneIntersect( cupExitInIndexA, cupExitInIndexB );

				meshNormals[spillMeshVertIndex] = liquidNormal;
				uv.x = 1.0f;
				uv.y = 0.0f;
				meshUVs[spillMeshVertIndex] = uv;
				meshVertices[spillMeshVertIndex++] = calculateCupExitPlaneIntersect( cupExitOutIndexA, cupExitOutIndexB );
			}
			//update rings
			float ringsActiveCapped = Mathf.Min( spill.ringsActive, maxRingsPerSpill );
			for( int j=0; j<ringsActiveCapped; j++ ){
				if( !spill.worldRingFalling[j] ){
					continue;
				}
				Vector3 currPos = spill.worldRingPos[j];
				Vector3 currVel = spill.worldRingVel[j]+Vector3.up*-0.11f*Time.deltaTime;
				RaycastHit hit = new RaycastHit();
				if( Physics.Raycast( currPos, currVel, out hit, currVel.magnitude, Instance.wallsMask|Instance.characterMovementMask ) ){
					currPos = hit.point;

					spill.worldRingFalling[j] = false;
					splashParams.position = currPos+Random.insideUnitSphere*0.02f;
					splashParams.startSize = 0.1f+Random.value*0.3f;
					splashParams.rotation = 360.0f*Random.value;
					splashParams.velocity = Vector3.ProjectOnPlane( currVel, hit.normal )*20.0f+Vector3.up*0.1f;
					splashSystem.Emit( splashParams, 1 );
					spill.active--;
					if( spill.active == 1 ){
						spill.active = 0;
					}
					handleObjectSplashing( hit.rigidbody );
				}
				spill.worldRingPos[j] = currPos+currVel;
				spill.worldRingVel[j] = currVel;
			}
			int spillActiveCounter = (int)ringsActiveCapped;
			
			float streamChoke = 0.025f;
			int spillRingIndex = ((int)spill.ringsActive)%maxRingsPerSpill;
			while( spillActiveCounter-- > 0 ){

				if( spillRingIndex==0 ){
					spillRingIndex=(int)ringsActiveCapped;
				}
				spillRingIndex--;
				if( spillActiveCounter == (int)ringsActiveCapped-1 && connectedSpillIndex != spillIndex ){
				}else if( spill.worldRingFalling[spillRingIndex] ){
					allSpillMeshIndices[spillMeshIndicesIndex++] = spillMeshVertIndex-1;
					allSpillMeshIndices[spillMeshIndicesIndex++] = spillMeshVertIndex-2;
					allSpillMeshIndices[spillMeshIndicesIndex++] = spillMeshVertIndex;
					allSpillMeshIndices[spillMeshIndicesIndex++] = spillMeshVertIndex+1;
					allSpillMeshIndices[spillMeshIndicesIndex++] = spillMeshVertIndex-1;
					allSpillMeshIndices[spillMeshIndicesIndex++] = spillMeshVertIndex;
				}

				Vector3 spillPos = transform.InverseTransformPoint(spill.worldRingPos[spillRingIndex]);

				Vector3 wiggleOffset = (spillRingIndex%2)*worldForward;

				uv.x = 0.0f;
				uv.y = (spillActiveCounter*4.0f)/ringsActiveCapped;
				meshUVs[spillMeshVertIndex] = uv;
				meshVertices[spillMeshVertIndex++] = spillPos+worldRight*streamChoke+wiggleOffset;
				uv.x = 1.0f;
				meshUVs[spillMeshVertIndex] = uv;
				meshVertices[spillMeshVertIndex++] = spillPos-worldRight*streamChoke-wiggleOffset;

				streamChoke -= 0.03f/maxRingsPerSpill;
			}
		}
		
		mesh.vertices = meshVertices;
		mesh.SetUVs(0,meshUVs);
		mesh.SetNormals(meshNormals);
		mesh.SetIndices(inCupMeshIndices,MeshTopology.Triangles,0);
		mesh.SetIndices(allSpillMeshIndices, MeshTopology.Triangles, 1 );

		if( fillLostVel < 0.001f ){
			fillLostVel = 0.0f;
		}else{
			//fill -= fillLostVel;
			fill = Mathf.Clamp01( fill );
			fillLostVel *= Mathf.Pow( 0.005f, Time.deltaTime );
		}
	}

	private void handleObjectSplashing( Rigidbody splashedBody ){
		
		if( splashedBody == null ){
			return;
		}
		Loli shinobu = splashedBody.gameObject.GetComponent(typeof(Loli)) as Loli;

		if( shinobu != null ){
			// shinobu.GetPassive().fireEnvironmentEvent( PassiveBehaviors.Event.WATER_SPLASH, transform );
		}

	}

	private Vector3 calculateCupExitPlaneIntersect( int inIndex, int outIndex ){
		
		//make 3D segment
		Vector3 inVertex = meshVertices[inIndex];
		Vector3 outVertex = meshVertices[outIndex];
		//intersect with y=maxLiquidHeight plane
		Vector3 d = outVertex-inVertex;
		float t = ( maxLiquidHeight-inVertex.y )/d.y;

		return inVertex+d*t;
	}

	private bool recalculateLiquidMesh(){

		if( fill == 0 ){
			return false;
		}
		
		float liquidPlaneHeight = fill*maxLiquidHeight;

		//if close to empty, clamp liquidNormal to fake empty effect
		float fakeEmptyBlend = Mathf.Max( 0.0f, (1.0f-fill)-0.6f )/0.4f;
		fakeEmptyBlend = Mathf.Pow( fakeEmptyBlend, 4 );
		
		liquidPlaneHeight -= fakeEmptyBlend*(2.0f-Mathf.Abs(transform.up.y))*maxLiquidHeight;
		liquidNormal = Vector3.LerpUnclamped( liquidNormal, Vector3.up, fakeEmptyBlend );

		
		Vector3 vertex = Vector3.zero;
		vertex.y = liquidPlaneHeight;
		meshVertices[0] = vertex;
		meshNormals[0] = Vector3.Lerp( Vector3.up, liquidNormal, 0.5f );

		//Find all spill cup exit indices
		cupExitInIndexA = -1;
		cupExitOutIndexA = -1;
		bool exiting = (-liquidNormal.x+liquidNormal.y*liquidPlaneHeight)/liquidNormal.y > maxLiquidHeight;	//starting exit radians=0
		
		int sumAbovePlaneHeight = 0;
		bool spill = false;
		float radians = 0.0f;
		for( int i=1; i<inCupMeshVertCount; i++ ){

			vertex.x = Mathf.Cos(radians)*topRadius;
			vertex.z = Mathf.Sin(radians)*topRadius;

			//intersect with plane
			float planeY = (-liquidNormal.x*vertex.x+liquidNormal.y*liquidPlaneHeight-liquidNormal.z*vertex.z)/liquidNormal.y;
			vertex.y = Mathf.Clamp( planeY, -1.0f, 1.0f );
			if( planeY > maxLiquidHeight ){
				spill = true;
				if( !exiting ){
					cupExitOutIndexA = i;
					cupExitOutIndexB = i-1;
					if( cupExitOutIndexB == 0 ){
						cupExitOutIndexB = inCupMeshVertCount-1;
					}
					exiting = true;
				}
			}else if( exiting ){
				cupExitInIndexA = i-1;
				if( cupExitInIndexA == 0 ){
					cupExitInIndexA = inCupMeshVertCount-1;
				}
				cupExitInIndexB = i;
				exiting = false;
			}

			radians += cupRadiansStep;
			meshVertices[i] = vertex;
			meshNormals[i] = liquidNormal;

			sumAbovePlaneHeight += (int)Mathf.Sign(planeY);
		}
		//default exit indices it didn't cross
		if( sumAbovePlaneHeight == -(inCupMeshVertCount-1) ){
			fill = 0.0f;
			return false;
		}
		if( cupExitInIndexA == -1 ){
			cupExitInIndexB = 1;
			cupExitInIndexA = inCupMeshVertCount-1;
		}
		if( cupExitOutIndexA == -1 ){
			cupExitOutIndexA = 1;
			cupExitOutIndexB = inCupMeshVertCount-1;
		}
		return spill;
	}
}

}