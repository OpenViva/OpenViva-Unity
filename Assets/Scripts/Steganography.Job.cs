using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;



namespace viva
{

    public partial class Steganography : MonoBehaviour
    {

        private struct UnpackClothingCardJob : IJob
        {

            public readonly NativeArray<byte> maskData;
            public readonly NativeArray<byte> nativeTargetByteArray;
            public NativeArray<byte> resultData;

            public UnpackClothingCardJob(NativeArray<byte> _maskData, NativeArray<byte> _nativeTargetByteArray)
            {
                maskData = _maskData;
                nativeTargetByteArray = _nativeTargetByteArray;
                resultData = new NativeArray<byte>(
                    Steganography.PACK_SIZE * Steganography.PACK_SIZE * 3,   //RGB return result
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory
                );
            }

            private int TargetBits()
            {
                return nativeTargetByteArray.Length * 8;
            }

            public void Execute()
            {

                BitArray targetBitArray;
                BitArray destBitArray = new BitArray(Steganography.PACK_SIZE * Steganography.PACK_SIZE * 3 * 8);
                {
                    byte[] targetByteArray = new byte[nativeTargetByteArray.Length];
                    for (int i = 0; i < targetByteArray.Length; i++)
                    {
                        targetByteArray[i] = nativeTargetByteArray[i];
                    }
                    targetBitArray = new BitArray(targetByteArray);
                }

                int destBitIndex = 0;
                int targetBitIndex = 0;
                bool coupleNextByte = false;
                for (int i = 0; i < maskData.Length; i++)
                {

                    if (targetBitIndex + 24 >= TargetBits() || destBitIndex + 32 >= destBitArray.Length)
                    {
                        break;
                    }

                    if (!UsesCompression(maskData[i]) && !coupleNextByte)
                    {
                        //unpack 4 bytes 6 bits each
                        for (int j = 0; j < 4; j++)
                        {

                            destBitArray.Set(destBitIndex++, true);
                            destBitArray.Set(destBitIndex++, true);
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                        }

                    }
                    else
                    {
                        //unpack 1.5 bytes
                        for (int j = 0; j < 3; j++)
                        {

                            if (!coupleNextByte)
                            {
                                destBitArray.Set(destBitIndex++, true);
                                destBitArray.Set(destBitIndex++, true);
                            }
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            targetBitIndex += 5;
                            coupleNextByte = !coupleNextByte;
                        }
                    }
                }
                byte[] resultDataByte = new byte[resultData.Length];
                destBitArray.CopyTo(resultDataByte, 0);
                resultData.CopyFrom(resultDataByte);
            }
        }

        public struct PackClothingCardJob : IJob
        {

            public readonly NativeArray<byte> nativeTargetARGB;
            public readonly NativeArray<byte> nativeMaskData;
            public readonly NativeArray<byte> nativeCadBorderAlphaArray;
            public readonly NativeArray<byte> nativeCadBorderGreyArray;
            public NativeArray<byte> nativeDestArray;



            public PackClothingCardJob(NativeArray<byte> _nativeTargetARGB,
                                        NativeArray<byte> _nativeMaskData,
                                        NativeArray<byte> _nativeCadBorderAlphaArray,
                                        NativeArray<byte> _nativeCadBorderGreyArray,
                                        NativeArray<byte> _nativeDestArray)
            {
                nativeTargetARGB = _nativeTargetARGB;
                nativeMaskData = _nativeMaskData;
                nativeCadBorderAlphaArray = _nativeCadBorderAlphaArray;
                nativeCadBorderGreyArray = _nativeCadBorderGreyArray;
                nativeDestArray = _nativeDestArray;
            }

            public void Execute()
            {

                //convert nativeTargetARGB to byte[] with alpha packed into blue channel
                byte[] targetData = new byte[nativeTargetARGB.Length * 3];
                int byteIndex = 0;
                int pixelCount = nativeTargetARGB.Length / 4;
                for (int i = 0; i < pixelCount; i++)
                {
                    targetData[byteIndex++] = nativeTargetARGB[i * 4 + 1];   //red
                    targetData[byteIndex++] = nativeTargetARGB[i * 4 + 2]; //green
                    byte blue = (byte)Mathf.Max(nativeTargetARGB[i * 4 + 3] - 5, 0);
                    byte alpha = nativeTargetARGB[i * 4];
                    targetData[byteIndex++] = (byte)Mathf.Min(blue + System.Convert.ToInt32(alpha < 127) * 255, 255);
                }

                //composite cardBorder on top of destData
                for (int i = 0; i < PACK_SIZE * CARD_HEIGHT; i++)
                {

                    byte alpha = nativeCadBorderAlphaArray[i];
                    byte grey = nativeCadBorderGreyArray[i];
                    float t = (float)alpha / 255.0f;
                    nativeDestArray[i * 3] = (byte)Mathf.LerpUnclamped(nativeDestArray[i * 3], grey, t);
                    nativeDestArray[i * 3 + 1] = (byte)Mathf.LerpUnclamped(nativeDestArray[i * 3 + 1], grey, t);
                    nativeDestArray[i * 3 + 2] = (byte)Mathf.LerpUnclamped(nativeDestArray[i * 3 + 2], grey, t);
                }

                //encode into bits
                System.Collections.BitArray targetBitArray = new BitArray(targetData);
                int targetBitIndex = 0;
                byteIndex = 0;
                bool coupleNextByte = false;
                for (int i = 0; i < nativeMaskData.Length; i++)
                {

                    if (targetBitIndex + 24 >= targetBitArray.Length)
                    {
                        break;
                    }
                    if (!UsesCompression(nativeMaskData[i]) && !coupleNextByte)
                    {
                        //pack 4 bytes
                        byte buffer = 0;
                        int bufferBitOffset = 0;
                        for (int k = 0; k < 12; k++)
                        {
                            if (k % 3 == 0)
                            {
                                targetBitIndex += 2;
                            }
                            int a = System.Convert.ToInt32(targetBitArray.Get(targetBitIndex++));
                            int b = System.Convert.ToInt32(targetBitArray.Get(targetBitIndex++));
                            buffer += (byte)(a << bufferBitOffset++);
                            buffer += (byte)(b << bufferBitOffset++);
                            if (k % 4 == 3)
                            {
                                nativeDestArray[byteIndex++] = buffer;
                                bufferBitOffset = 0;
                                buffer = 0;
                            }
                        }
                    }
                    else
                    {
                        //pack 1.5 bytes into 3 bytes
                        for (int j = 0; j < 3; j++)
                        {
                            if (!coupleNextByte)
                            {
                                targetBitIndex += 2;
                            }
                            nativeDestArray[byteIndex] = (byte)(ZeroLast3Bits(nativeDestArray[byteIndex++]) + Extract3Bits(targetBitArray, targetBitIndex));
                            targetBitIndex += 3;
                            coupleNextByte = !coupleNextByte;
                        }
                    }
                }
            }
        }


        private struct UnpackLosslessDataJob : IJob
        {

            private readonly int payloadLength;
            public readonly NativeArray<byte> nativeMaskData;
            public readonly NativeArray<byte> nativeTargetByteArray;
            public NativeArray<byte> resultData;

            public UnpackLosslessDataJob(int _payloadLength, NativeArray<byte> _nativeMaskData, NativeArray<byte> _nativeTargetByteArray)
            {
                payloadLength = _payloadLength;
                nativeMaskData = _nativeMaskData;
                nativeTargetByteArray = _nativeTargetByteArray;
                resultData = new NativeArray<byte>(
                    payloadLength,
                    Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory
                );
            }

            public void Execute()
            {

                BitArray targetBitArray;
                BitArray destBitArray = new BitArray((payloadLength + 3) * 8);
                {
                    byte[] targetByteArray = new byte[nativeTargetByteArray.Length];
                    for (int i = 0; i < targetByteArray.Length; i++)
                    {
                        targetByteArray[i] = nativeTargetByteArray[i];
                    }
                    targetBitArray = new BitArray(targetByteArray);
                }


                int destBitIndex = 0;
                int targetBitIndex = 0;
                for (int i = 0; i < nativeMaskData.Length; i++)
                {

                    if (destBitIndex >= payloadLength * 8)
                    {    //should match ==
                        break;
                    }

                    if (!UsesCompression(nativeMaskData[i]))
                    {
                        //unpack 3 bytes
                        for (int j = 0; j < 3; j++)
                        {

                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                        }
                    }
                    else
                    {
                        //unpack 9 bits
                        for (int j = 0; j < 3; j++)
                        {

                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            destBitArray.Set(destBitIndex++, targetBitArray.Get(targetBitIndex++));
                            targetBitIndex += 5;
                        }
                    }
                }
                //unpad result
                byte[] tempResultPadded = new byte[payloadLength + 3];
                destBitArray.CopyTo(tempResultPadded, 0);

                byte[] resultDataByte = new byte[payloadLength];
                for (int i = 0; i < payloadLength; i++)
                {
                    resultDataByte[i] = tempResultPadded[i];
                }
                resultData.CopyFrom(resultDataByte);

                if (destBitIndex - payloadLength * 8 > 24)
                {
                    Debug.LogError("ERROR DID NOT WRITE EXACT BYTES should be <24 +" + (destBitIndex - payloadLength * 8));
                }
                else
                {
                    // Debug.Log("[STEGANOGRAPHY] Successful data byte read <24 +"+(destBitIndex-payloadLength*8 ));
                }
            }
        }

        public struct PackLosslessDataJob : IJob
        {

            public readonly NativeArray<byte> nativeTargetData;
            public readonly NativeArray<byte> nativeMaskData;
            public readonly NativeArray<byte> nativeCadBorderAlphaArray;
            public readonly NativeArray<byte> nativeCadBorderGreyArray;
            public NativeArray<byte> nativeDestArray;

            public PackLosslessDataJob(NativeArray<byte> _nativeTargetData,
                                        NativeArray<byte> _nativeMaskData,
                                        NativeArray<byte> _nativeCadBorderAlphaArray,
                                        NativeArray<byte> _nativeCadBorderGreyArray,
                                        NativeArray<byte> _nativeDestArray)
            {
                nativeTargetData = _nativeTargetData;
                nativeMaskData = _nativeMaskData;
                nativeCadBorderAlphaArray = _nativeCadBorderAlphaArray;
                nativeCadBorderGreyArray = _nativeCadBorderGreyArray;
                nativeDestArray = _nativeDestArray;
            }

            public void Execute()
            {

                //pad with 3 bytes so it never goes out of bounds
                int padding = 3;
                byte[] targetData = new byte[nativeTargetData.Length + padding];
                for (int i = 0; i < nativeTargetData.Length; i++)
                {
                    targetData[i] = nativeTargetData[i];
                }
                System.Collections.BitArray targetBitArray = new BitArray(targetData);
                int byteIndex = 0;

                //composite cardBorder on top of destData
                for (int i = 0; i < PACK_SIZE * CARD_HEIGHT; i++)
                {

                    byte alpha = nativeCadBorderAlphaArray[i];
                    byte grey = nativeCadBorderGreyArray[i];
                    float t = (float)alpha / 255.0f;
                    nativeDestArray[i * 3] = (byte)Mathf.LerpUnclamped(nativeDestArray[i * 3], grey, t);
                    nativeDestArray[i * 3 + 1] = (byte)Mathf.LerpUnclamped(nativeDestArray[i * 3 + 1], grey, t);
                    nativeDestArray[i * 3 + 2] = (byte)Mathf.LerpUnclamped(nativeDestArray[i * 3 + 2], grey, t);
                }

                //encode into bits
                int targetBitIndex = 0;
                byteIndex = 0;
                for (int i = 0; i < nativeMaskData.Length; i++)
                {

                    if (targetBitIndex >= nativeTargetData.Length * 8)
                    {  //should match ==
                        break;
                    }
                    if (!UsesCompression(nativeMaskData[i]))
                    {
                        //pack 1 byte per channel (3 bytes)
                        for (int j = 0; j < 3; j++)
                        {
                            nativeDestArray[byteIndex++] = Extract8Bits(targetBitArray, targetBitIndex);
                            targetBitIndex += 8;
                        }
                    }
                    else
                    {
                        //pack 3 bits per channel (9 bits)
                        for (int j = 0; j < 3; j++)
                        {
                            nativeDestArray[byteIndex] = (byte)(ZeroLast3Bits(nativeDestArray[byteIndex++]) + Extract3Bits(targetBitArray, targetBitIndex));
                            targetBitIndex += 3;
                        }
                    }
                }

                if (targetBitIndex - nativeTargetData.Length * 8 > 24)
                {
                    Debug.LogError("ERROR DID NOT WRITE EXACT BYTES should be <24 +" + (targetBitIndex - nativeTargetData.Length * 8));
                }
                else
                {
                    // Debug.Log("[STEGANOGRAPHY] Successful data byte write <24 +"+(targetBitIndex-nativeTargetData.Length*8 ));
                }
            }
        }

    }


}