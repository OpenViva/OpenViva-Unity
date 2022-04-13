using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace viva{

    public static class MeshBufferUtil{

        public static void BufferXZQuad( Vector2 topLeft, Vector2 size, int columns, int quadIndex, Vector3 quadStart, Vector2 quadSize, Vector3[] vertices, Vector2[] uvs, ref int index ){
            
            int sqy = quadIndex/columns;
            int sqx = quadIndex-sqy*columns;

            Vector2 p0 = topLeft+size*new Vector2( sqx, -sqy );
            p0.y = 1.0f-p0.y;
            vertices[ index ] = quadStart;
            uvs[ index++ ] = p0;
            vertices[ index ] = quadStart+new Vector3( quadSize.x, 0, 0 );
            uvs[ index++ ] = p0+Vector2.right*size;
            vertices[ index ] = quadStart+new Vector3( quadSize.x, 0, -quadSize.y );
            uvs[ index++ ] = p0+Vector2.one*size;
            vertices[ index ] = quadStart+new Vector3( 0, 0, -quadSize.y );
            uvs[ index++ ] = p0+Vector2.up*size;
        }
        public static void BufferXYQuad( Vector2 topLeft, Vector2 size, int columns, int quadIndex, Vector3 quadStart, Vector2 quadSize, Vector3[] vertices, Vector2[] uvs, ref int index ){
            
            int sqy = quadIndex/columns;
            int sqx = quadIndex-sqy*columns;

            Vector2 p0 = topLeft+size*new Vector2( sqx, -sqy );
            p0.y = 1.0f-p0.y;
            vertices[ index ] = quadStart;
            uvs[ index++ ] = p0;
            vertices[ index ] = quadStart+new Vector3( quadSize.x, 0, 0 );
            uvs[ index++ ] = p0+Vector2.right*size;
            vertices[ index ] = quadStart+new Vector3( quadSize.x, -quadSize.y, 0 );
            uvs[ index++ ] = p0+Vector2.one*size;
            vertices[ index ] = quadStart+new Vector3( 0, -quadSize.y, 0 );
            uvs[ index++ ] = p0+Vector2.up*size;
        }

        public static void BuildTrianglesFromQuadPoints( int[] indices, int quads, int indexOffset, int quadOffset ){
            
            for( int quad=0; quad<quads; quad++ ){
                indices[ indexOffset++ ] = quad*4+quadOffset;
                indices[ indexOffset++ ] = quad*4+1+quadOffset;
                indices[ indexOffset++ ] = quad*4+2+quadOffset;
                indices[ indexOffset++ ] = quad*4+quadOffset;
                indices[ indexOffset++ ] = quad*4+2+quadOffset;
                indices[ indexOffset++ ] = quad*4+3+quadOffset;
            }
        }
    }

}