using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace viva
{

    public static partial class Tools
    {

        public static bool EnsureFolder(string directory)
        {
            if (!System.IO.Directory.Exists(directory))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                catch (System.Exception e)
                {
                    return false;
                }
            }
            return true;
        }

        public static T LoadJson<T>(string filepath, object overwriteTarget = null) where T : class
        {
            if (File.Exists(filepath))
            {
                try
                {
                    if (overwriteTarget == null)
                    {
                        return JsonUtility.FromJson(File.ReadAllText(filepath), typeof(T)) as T;
                    }
                    else
                    {
                        JsonUtility.FromJsonOverwrite(File.ReadAllText(filepath), overwriteTarget);
                        return overwriteTarget as T;
                    }
                }
                catch (System.Exception e)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static bool SaveJson(object obj, bool prettyPrint, string path)
        {
            try
            {
                var json = JsonUtility.ToJson(obj, prettyPrint);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    byte[] data = Tools.UTF8ToByteArray(json);
                    stream.Write(data, 0, data.Length);
                    stream.Close();
                }
                return true;
            }
            catch (System.Exception e)
            {
                return false;
            }
        }

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