using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace viva
{

    public static partial class Tools
    {

        public class FileTextureRequest
        {

            public readonly string filename;
            public Texture2D result;
            public string error = null;
            public readonly Vector2Int[] targetSizes;
            public readonly string targetSizeError;
            public int targetSizeIndex = -1;

            public FileTextureRequest(string _filename, Vector2Int[] _targetSizes = null, string _targetSizeError = null)
            {
                filename = _filename;
                targetSizes = _targetSizes;
                targetSizeError = _targetSizeError;
            }
        }


        public static IEnumerator LoadFileTexture(FileTextureRequest request)
        {
            Debug.Log("[FILE TEXTURE] " + request.filename);
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(request.filename))
            {
                yield return uwr.SendWebRequest();
                if (uwr.isNetworkError || uwr.isHttpError)
                {
                    Debug.Log("[FILE TEXTURE] Could not load [" + request.filename + "] " + uwr.error);
                    yield break;
                }
                request.result = DownloadHandlerTexture.GetContent(uwr);
                if (request.result == null)
                {
                    Debug.Log("[FILE TEXTURE] Could not read from handle [" + request.filename + "] " + uwr.error);
                    yield break;
                }
                if (request.result.width == 8 && request.result.height == 8)
                {
                    GameDirector.Destroy(request.result);
                    request.result = null;
                    Debug.Log("[FILE TEXTURE] Could not load [" + request.filename + "] " + uwr.error);
                    yield break;
                }
                request.result.name = request.filename.Split('/').Last().Split('\\').Last();
                request.result.wrapMode = TextureWrapMode.Clamp;

                if (request.targetSizes != null)
                {
                    for (int i = 0; i < request.targetSizes.Length; i++)
                    {
                        if (request.targetSizes[i] == new Vector2Int(request.result.width, request.result.height))
                        {
                            request.targetSizeIndex = i;
                            break;
                        }
                    }
                    if (request.targetSizeIndex == -1)
                    {
                        GameDirector.Destroy(request.result);
                        request.result = null;
                        if (request.targetSizeError != null)
                        {
                            request.error = request.targetSizeError;
                        }
                        else
                        {
                            request.error = "Invalid image size!";
                        }
                    }
                }
            }
        }
    }
}