using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


namespace viva
{


    public partial class Steganography : MonoBehaviour
    {

        public class PackLosslessDataRequest
        {

            public string error;
            public readonly string name;
            public readonly byte[] targetData;
            public readonly Texture2D destination;
            public readonly bool useCharacterMask;
            public Texture2D result;

            public PackLosslessDataRequest(string _name, byte[] _targetData, Texture2D _destination, bool _useCharacterMask)
            {
                name = _name;
                targetData = _targetData;
                destination = _destination;
                useCharacterMask = _useCharacterMask;
            }
        }

        public class UnpackLosslessDataRequest
        {

            public readonly Texture2D cardTexture;
            public byte[] result;
            public string error = null;
            public readonly bool useCharacterMask;

            public UnpackLosslessDataRequest(Texture2D _cardTexture, bool _useCharacterMask)
            {
                cardTexture = _cardTexture;
                useCharacterMask = _useCharacterMask;
            }
        }

        [SerializeField]
        public Texture2D characterPackingMask;
        [SerializeField]
        public Texture2D characterCardBorderAlpha;
        [SerializeField]
        public Texture2D characterCardBorderGrey;
        [SerializeField]
        public Texture2D skinPackingMask;
        [SerializeField]
        public Texture2D skinCardBorderAlpha;
        [SerializeField]
        public Texture2D skinCardBorderGrey;



        private readonly int MAX_CHARACTER_CARD_BYTES = 2363271;    //DO NOT CHANGE ( 1256171*9+316693*24 )/8

        public delegate void OnFinishDataPacking(byte[] data, Texture2D result, string error);


        public IEnumerator ExecuteUnpackCharacter(UnpackLosslessDataRequest request)
        {

            request.error = null;
            request.result = null;

            if (request.cardTexture == null)
            {
                request.error = "ERROR texture cannot be null!";
                yield break;
            }

            Texture2D packingMask;
            if (request.useCharacterMask)
            {
                packingMask = characterPackingMask;
            }
            else
            {
                packingMask = skinPackingMask;
            }


            string error = null;
            bool valid = IsValidCardFormat(request.cardTexture, new TextureFormat[] { TextureFormat.ARGB32, TextureFormat.RGB24 }, ref error);
            valid &= IsValidCardFormat(packingMask, new TextureFormat[] { TextureFormat.Alpha8 }, ref error);

            if (!valid)
            {
                request.error = error;
                yield break;
            }

            //ensure data is RGB
            byte[] targetData = request.cardTexture.GetRawTextureData();
            byte[] targetByteArray = null;
            if (request.cardTexture.format == TextureFormat.ARGB32)
            {
                byte[] rgbTargetData = new byte[(targetData.Length * 3) / 4];
                int RGBByteIndex = 0;
                int RGBAByteIndex = 0;
                while (RGBByteIndex < rgbTargetData.Length)
                {
                    RGBAByteIndex++;    //skip alpha
                    rgbTargetData[RGBByteIndex++] = targetData[RGBAByteIndex++];
                    rgbTargetData[RGBByteIndex++] = targetData[RGBAByteIndex++];
                    rgbTargetData[RGBByteIndex++] = targetData[RGBAByteIndex++];
                }
                targetByteArray = rgbTargetData;
            }
            else
            {   //RGB24
                targetByteArray = targetData;
            }

            //read last 4 bytes as metadata
            byte[] metadata = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                metadata[i] = targetByteArray[targetByteArray.Length - 5 + i];
            }
            int payloadLength = System.BitConverter.ToInt32(metadata, 0);

            if (payloadLength > MAX_CHARACTER_CARD_BYTES)
            {
                request.error = "Could not read character card! Payload length metadata too large!";
                yield break;
            }

            ///Initialize Job
            NativeArray<byte> nativeMaskData = ConvertToNativeArray(packingMask.GetRawTextureData());
            NativeArray<byte> nativeTargetArray = ConvertToNativeArray(targetByteArray);
            var unpackTextureData = new UnpackLosslessDataJob(payloadLength, nativeMaskData, nativeTargetArray);
            JobHandle jobHandle = unpackTextureData.Schedule();

            while (true)
            {
                if (!jobHandle.IsCompleted)
                {
                    yield return null;
                    continue;
                }
                break;
            }
            jobHandle.Complete();

            byte[] resultData = new byte[payloadLength];
            unpackTextureData.resultData.CopyTo(resultData);

            nativeMaskData.Dispose();
            nativeTargetArray.Dispose();
            unpackTextureData.resultData.Dispose();

            request.result = resultData;
        }

        public IEnumerator ExecutePackLosslessData(PackLosslessDataRequest request)
        {

            request.error = null;
            request.result = null;

            int maxBytes = MAX_CHARACTER_CARD_BYTES + 4;    //allocate 4 bytes for metadata
            if (request.targetData == null || request.targetData.Length > maxBytes)
            {
                request.error = "\nERROR metadata must be at most " + maxBytes + " bytes! Given: " + request.targetData.Length;
                yield break;
            }

            Texture2D packingMask;
            Texture2D cardBorderAlpha;
            Texture2D cardBorderGrey;
            if (request.useCharacterMask)
            {
                packingMask = characterPackingMask;
                cardBorderAlpha = characterCardBorderAlpha;
                cardBorderGrey = characterCardBorderGrey;
            }
            else
            {
                packingMask = skinPackingMask;
                cardBorderAlpha = skinCardBorderAlpha;
                cardBorderGrey = skinCardBorderGrey;
            }

            bool valid = true;
            valid &= IsValidCardFormat(request.destination, new TextureFormat[] { TextureFormat.RGB24 }, ref request.error);
            valid &= IsValidCardFormat(cardBorderAlpha, new TextureFormat[] { TextureFormat.Alpha8 }, ref request.error);
            valid &= IsValidCardFormat(cardBorderGrey, new TextureFormat[] { TextureFormat.Alpha8 }, ref request.error);
            valid &= IsValidCardFormat(packingMask, new TextureFormat[] { TextureFormat.Alpha8 }, ref request.error);

            if (!valid)
            {
                yield break;
            }
            NativeArray<byte> nativeTargetColorArray = ConvertToNativeArray(request.targetData);
            NativeArray<byte> nativeMaskData = ConvertToNativeArray(packingMask.GetRawTextureData());
            NativeArray<byte> nativeDestArray = ConvertToNativeArray(request.destination.GetRawTextureData());
            NativeArray<byte> nativeCadBorderAlphaArray = ConvertToNativeArray(cardBorderAlpha.GetRawTextureData());
            NativeArray<byte> nativeCadBorderGreyArray = ConvertToNativeArray(cardBorderGrey.GetRawTextureData());
            var packTextureData = new PackLosslessDataJob(
                nativeTargetColorArray,
                nativeMaskData,
                nativeCadBorderAlphaArray,
                nativeCadBorderGreyArray,
                nativeDestArray
            );
            JobHandle jobHandle = packTextureData.Schedule();

            while (true)
            {
                if (!jobHandle.IsCompleted)
                {
                    yield return null;
                    continue;
                }
                break;
            }
            jobHandle.Complete();

            byte[] destData = new byte[nativeDestArray.Length];
            packTextureData.nativeDestArray.CopyTo(destData);

            nativeTargetColorArray.Dispose();
            nativeMaskData.Dispose();
            nativeDestArray.Dispose();
            nativeCadBorderAlphaArray.Dispose();
            nativeCadBorderGreyArray.Dispose();

            //append 4-byte metadata at the last pixels
            byte[] metadata = System.BitConverter.GetBytes(request.targetData.Length);
            System.Array.Copy(metadata, 0, destData, destData.Length - 5, 4);

            request.result = CreateCard(destData, request.name);
        }
    }

}