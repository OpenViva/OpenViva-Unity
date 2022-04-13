using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class MeshSlicer{
        
    public static SplitPiece inPiece;
    public static SplitPiece outPiece;
    private static Ray ray = new Ray();
    
    public class SplitPiece{
        public class ContourNode{
            public ContourNode next;
            public ContourNode prev;
            public readonly int triIndex;
            public bool searched;
            public int hashB;
            public ContourNode(  int _triIndex, int _hashB ){
                triIndex = _triIndex;
                hashB = _hashB;
            }
        }
        
        public List<Vector3> verts = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<int>[] triGroups = new List<int>[]{ new List<int>(), new List<int>() }; //build surface and interior tri list
        public Dictionary<int,ContourNode> contourNodes = new Dictionary<int,ContourNode>();
        
        public SplitPiece(){
        }
        public void CreateContour( int triIndex, int hashA, int hashB ){
            contourNodes[ hashA ] = new ContourNode( triIndex, hashB );
        }
        public Mesh CompileMesh( Vector3 sliceNormal, Vector3 sliceTangent, Bounds? uvBounds ){
                        
            //connect contour graph via hash links
            List<ContourNode> missingNext = new List<ContourNode>();
            foreach( ContourNode node in contourNodes.Values ){
                ContourNode candidate;
                if( contourNodes.TryGetValue( node.hashB, out candidate ) ){
                    if( candidate != node ){
                        node.next = candidate;
                        candidate.prev = node;
                    }
                }else{
                    missingNext.Add( node );
                }
            }
            //ensure all nodes have a neighbour
            if( missingNext.Count > 0 ){
                List<ContourNode> missingPrev = new List<ContourNode>();
                foreach( ContourNode node in contourNodes.Values ){
                    if( node.prev == null ){
                        missingPrev.Add( node );
                    }
                }
                foreach( ContourNode node in missingNext ){
                    //find closest replacement node
                    
                    Vector3 nodePos = verts[ node.triIndex ];
                    int closestIndex = 0;
                    float leastSqDist = Mathf.Infinity;
                    for( int i=0; i<missingPrev.Count; i++ ){
                        if( missingPrev[i] == node ){
                            continue;
                        }
                        float sqDist = Vector3.SqrMagnitude( verts[ missingPrev[i].triIndex ]-nodePos );
                        if( sqDist < leastSqDist ){
                            leastSqDist = sqDist;
                            closestIndex = i;
                        }
                    }
                    //might produce self-intersecting 2D poly graph
                    ContourNode closest = missingPrev[ closestIndex ];
                    node.next = closest;
                    closest.prev = node;
                    missingPrev.RemoveAt( closestIndex );
                }
            }
            
            if( sliceNormal.y > 0.0f ){
                foreach( ContourNode node in contourNodes.Values ){
                    // Debug.DrawLine( verts[ node.triIndex ], verts[ node.next.triIndex ], Color.magenta, 5.0f );
                }
            }
            //separate into individual islands
            int removed = 0;
            List<List<int>> islands = new List<List<int>>();
            foreach( ContourNode node in contourNodes.Values ){
                if( !node.searched ){
                    List<int> island = new List<int>();
                    
                    island.Add( node.triIndex );
                    node.searched = true;
                    Vector3 lastPoint = verts[ node.triIndex ];
                    Vector3 lastDir = sliceNormal;
                    ContourNode child = node.next;
                    do{
                        child.searched = true;
                        if( child.next.searched ){
                             island.Add( child.triIndex );
                             break;
                        }
                        Vector3 childPos = verts[ child.triIndex ];
                        Vector3 currDir = verts[ child.next.triIndex ]-lastPoint;
                        if( Vector3.SqrMagnitude( lastPoint-childPos ) > 0.00001f ){
                            if( Vector3.Cross( currDir, lastDir ) == Vector3.zero ){
                                if( sliceNormal.y > 0 ){
                                    // Debug.DrawLine( childPos, childPos+currDir+Vector3.up*0.001f, Color.cyan, 5.0f );
                                }
                                removed++;
                                child = child.next;
                                continue;
                            }else{
                                lastDir = currDir;
                            }
                            lastPoint = childPos;
                        }
                        //simplify chain, ignore connection if consecutive connections are in the same direction
                        island.Add( child.triIndex );
                        child = child.next;
                    }while( true );
                    islands.Add( island );
                }
            }
            // Debug.Log("Removed: "+removed);
            //build uvs based on bounding box
            Quaternion unrotate = Quaternion.Inverse( Quaternion.LookRotation( sliceNormal, sliceTangent ) );
            Vector2 minPos;
            Vector2 maxPos;
            if( uvBounds.HasValue ){
                minPos = uvBounds.Value.min;
                maxPos = uvBounds.Value.max;
            }else{
                minPos = unrotate*verts[ islands[0][0] ];
                maxPos = minPos;
                foreach( List<int> island in islands ){
                    for( int i=0; i<island.Count; i++ ){
                        Vector2 unrotated = unrotate*verts[ island[i] ];
                        minPos = Vector2.Min( minPos, unrotated );
                        maxPos = Vector2.Max( maxPos, unrotated );
                    }
                }
            }
            //triangulate with an averaged center point (trade off of speed for soft-concave mesh support)
            Vector2 invSize = Vector2.one/(maxPos-minPos);
            foreach( List<int> island in islands ){
                
                //dont add island fill if it has less than 3 vertices
                if( island.Count < 3 ){
                    continue;
                }
                //triangulate zig zag
                int triCount = verts.Count;
                foreach( int triIndex in island ){
                    Vector3 vert = verts[ triIndex ];
                    verts.Add( vert );
                    normals.Add( sliceNormal );
                    Vector2 unrotated = unrotate*vert;
                    uvs.Add( ( unrotated-minPos )*invSize );
                }
                
                int lastIndex = 0;
                for( int i=1; i<island.Count/2; i++ ){
                    triGroups[1].Add( triCount+i );
                    triGroups[1].Add( triCount+lastIndex );
                    triGroups[1].Add( triCount+island.Count-i );
                    
                    triGroups[1].Add( triCount+i+1 );
                    triGroups[1].Add( triCount+i );
                    triGroups[1].Add( triCount+island.Count-i );
                    lastIndex = island.Count-i;
                }
                if( island.Count%2 == 1 && island.Count > 3 ){
                    triGroups[1].Add( triCount+island.Count/2+2 );
                    triGroups[1].Add( triCount+island.Count/2+1 );
                    triGroups[1].Add( triCount+island.Count/2 );
                }
            }
            
            Mesh mesh = new Mesh();
            mesh.vertices = verts.ToArray();
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.subMeshCount = 2;
            mesh.SetIndices( triGroups[0].ToArray(), MeshTopology.Triangles, 0 );
            mesh.SetIndices( triGroups[1].ToArray(), MeshTopology.Triangles, 1 );
            return mesh;
        }
    }
    private static Vector3[] targVerts;
    private static Vector3[] targNormals;
    private static Vector2[] targUVs;
    public static Mesh[] Slice( Mesh mesh, Vector3 slicePosition, Vector3 sliceNormal, Vector3 sliceTangent, Bounds? uvBounds=null, int minVertexOverlap=0 ){
        if( mesh.GetTopology(0) != MeshTopology.Triangles ){
            return null;
        }
        Plane slice = new Plane( sliceNormal, slicePosition );
        //initialize
        targVerts = mesh.vertices;
        targNormals = mesh.normals;
        targUVs = mesh.uv;
        //create meshes a and b
        inPiece = new SplitPiece();
        outPiece = new SplitPiece();
        //slice all submeshes
        for( int i=0; i<mesh.subMeshCount; i++ ){
            SliceSubmesh( slice, mesh, i );
        }
        //build meshes only if there were any intersections
        if( inPiece.contourNodes.Count < minVertexOverlap || outPiece.contourNodes.Count < minVertexOverlap ){
            return null;
        }
        return new Mesh[]{
            inPiece.CompileMesh( sliceNormal, sliceTangent, uvBounds ),
            outPiece.CompileMesh( -sliceNormal, sliceTangent, uvBounds )
        };
    }
    private static void SliceSubmesh( Plane slice, Mesh mesh, int submeshIndex ){
        
        int triGroupIndex = Mathf.Min( submeshIndex, 1 );
        int[] targTris = mesh.GetIndices( submeshIndex );
        int triIndex = 0;
        while( triIndex < targTris.Length ){
            int ia = targTris[ triIndex++ ];
            int ib = targTris[ triIndex++ ];
            int ic = targTris[ triIndex++ ];
            Vector3 a = targVerts[ia];
            Vector3 b = targVerts[ib];
            Vector3 c = targVerts[ic];
            //detect if triangle was sliced
            bool sideA = slice.GetSide(targVerts[ia]);
            bool sideB = slice.GetSide(targVerts[ib]);
            bool sideC = slice.GetSide(targVerts[ic]);
            if( sideA == sideB && sideB == sideC ){
                SplitPiece piece;
                if( sideA ){
                    piece = outPiece;
                }else{
                    piece = inPiece;
                }
                int trisACount = piece.verts.Count;
                piece.verts.Add(a);
                piece.verts.Add(b);
                piece.verts.Add(c);
                piece.normals.Add( targNormals[ia] );
                piece.normals.Add( targNormals[ib] );
                piece.normals.Add( targNormals[ic] );
                piece.uvs.Add( targUVs[ia] );
                piece.uvs.Add( targUVs[ib] );
                piece.uvs.Add( targUVs[ic] );
                piece.triGroups[ triGroupIndex ].Add( trisACount );
                piece.triGroups[ triGroupIndex ].Add( trisACount+1 );
                piece.triGroups[ triGroupIndex ].Add( trisACount+2 );
            }else{
                if( sideA == sideB ){
                    SliceTriangle( slice, triGroupIndex, sideC, b, c, a, ib, ic, ia );
                }else if( sideB == sideC ){
                    SliceTriangle( slice, triGroupIndex, sideA, c, a, b, ic, ia, ib );
                }else{  //sideC == sideA
                    SliceTriangle( slice, triGroupIndex, sideB, a, b, c, ia, ib, ic );
                }
            }
        }
    }
    private static float GetSegmentRatio( Vector3 min, Vector3 max, Vector3 inter ){
        if( min.x != max.x ){
            return (inter.x-min.x)/(max.x-min.x);
        }else if( min.y != max.y ){
            return (inter.y-min.y)/(max.y-min.y);
        }else{
            return (inter.z-min.z)/(max.z-min.z);
        }
    }
    private static Vector2 GetUVFromRatio( Vector2 uvA, Vector2 uvB, float ratio ){
        Vector2 ratioVec2 = new Vector2( ratio, ratio );
        if( uvA.x > uvB.x ){
            ratioVec2.x = 1.0f-ratioVec2.x;
        }
        if( uvA.y > uvB.y ){
            ratioVec2.y = 1.0f-ratioVec2.y;
        }
        Vector2 minUV = Vector2.Min( uvA, uvB );
        Vector2 maxUV = Vector2.Max( uvA, uvB );
        return minUV+(maxUV-minUV)*ratioVec2;
    }
    
    private static Vector3 GetNormalFromRatio( Vector3 normA, Vector3 normB, float ratio ){
        Vector3 minUV = Vector3.Min( normA, normB );
        Vector3 maxUV = Vector3.Max( normA, normB );
        return minUV+(maxUV-minUV)*ratio;
    }
    private static int GetSpatialHashCode( Vector3 a ){
        const float accuracy = 1000.0f;
        a.x = Mathf.Round( a.x*accuracy )/accuracy;
        a.y = Mathf.Round( a.y*accuracy )/accuracy;
        a.z = Mathf.Round( a.z*accuracy )/accuracy;
        return a.GetHashCode();
    }
    private static void SliceTriangle( Plane slice, int triGroupIndex, bool bIsPositive,
            Vector3 a, Vector3 b, Vector3 c,
            int ia, int ib, int ic ){
        
        Vector3 xab = PlaneAndSegmentCollision( slice, a, b );
        Vector3 xbc = PlaneAndSegmentCollision( slice, c, b );
        
        int xabHash = GetSpatialHashCode( xab );
        int xbcHash = GetSpatialHashCode( xbc );
        
        Vector3 normA = targNormals[ia];
        Vector3 normB = targNormals[ib];
        Vector3 normC = targNormals[ic];
        Vector2 uvA = targUVs[ia];
        Vector2 uvB = targUVs[ib];
        Vector2 uvC = targUVs[ic];
        float abRatio = GetSegmentRatio( a, b, xab );
        Vector2 uvXAB = GetUVFromRatio( uvA, uvB, abRatio );
        Vector3 normXAB = GetNormalFromRatio( normA, normB, abRatio );
        float bcRatio = GetSegmentRatio( b, c, xbc );
        Vector2 uvXBC = GetUVFromRatio( uvB, uvC, bcRatio );
        Vector3 normXBC = GetNormalFromRatio( normB, normC, bcRatio );
        //ADD CLOCKWISE
        SplitPiece pieceA;
        SplitPiece pieceB;
        if( bIsPositive ){
            pieceA = inPiece;
            pieceB = outPiece;
        }else{
            pieceA = outPiece;
            pieceB = inPiece;
        }
        
        int trisACount = pieceA.verts.Count;
        if( xabHash != xbcHash ){ //actually slice triangle if hashes are different (not same point)
            int trisBCount = pieceB.verts.Count;
            pieceA.CreateContour( trisACount, xabHash, xbcHash );
            pieceB.CreateContour( trisBCount, xbcHash, xabHash );
            pieceA.verts.Add( xab );
            pieceA.verts.Add( xbc );
            pieceA.verts.Add( c );
            pieceA.verts.Add( a );
            pieceA.normals.Add( normXAB );
            pieceA.normals.Add( normXBC );
            pieceA.normals.Add( normC );
            pieceA.normals.Add( normA );
            pieceA.uvs.Add( uvXAB );
            pieceA.uvs.Add( uvXBC );
            pieceA.uvs.Add( uvC );
            pieceA.uvs.Add( uvA );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount+1 );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount+2 );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount+2 );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount+3 );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount );
            pieceB.verts.Add( xbc );
            pieceB.verts.Add( xab );
            pieceB.verts.Add( b );
            pieceB.normals.Add( normXBC );
            pieceB.normals.Add( normXAB );
            pieceB.normals.Add( normB );
            pieceB.uvs.Add( uvXBC );
            pieceB.uvs.Add( uvXAB );
            pieceB.uvs.Add( uvB );
            pieceB.triGroups[ triGroupIndex ].Add( trisBCount );
            pieceB.triGroups[ triGroupIndex ].Add( trisBCount+1 );
            pieceB.triGroups[ triGroupIndex ].Add( trisBCount+2 );
        }else{
            pieceA.verts.Add(a);
            pieceA.verts.Add(xbc);
            pieceA.verts.Add(c);
            pieceA.normals.Add( targNormals[ia] );
            pieceA.normals.Add( targNormals[ib] );
            pieceA.normals.Add( targNormals[ic] );
            pieceA.uvs.Add( targUVs[ia] );
            pieceA.uvs.Add( targUVs[ib] );
            pieceA.uvs.Add( targUVs[ic] );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount+1 );
            pieceA.triGroups[ triGroupIndex ].Add( trisACount+2 );
        }
    }
    private static Vector3 PlaneAndSegmentCollision( Plane plane, Vector3 a, Vector3 b ){
        ray.origin = a;
        ray.direction = b-a;
        float ratio;
        plane.Raycast( ray, out ratio );
        return ray.origin+ray.direction*ratio;
    }
}