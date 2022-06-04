using UnityEngine;


namespace viva
{

    public class CardTextureSerializer
    {
        public class CardTextureFormat
        {

            public readonly int requiredSize;
            public readonly bool useAlpha;

            public CardTextureFormat(int _requiredSize, bool _useAlpha)
            {
                requiredSize = _requiredSize;
                useAlpha = _useAlpha;
            }
        }

        private readonly CardTextureFormat[] formats;
        public string error;

        public CardTextureSerializer(CardTextureFormat[] _formats)
        {
            formats = _formats;
        }

        public byte[] GetCompressedFormattedTextureData(Texture2D texture, CardTextureFormat cardFormat)
        {

            if (texture.mipmapCount != 1)
            {
                error = "ERROR Texture has mipmaps!";
                return null;
            }
            if (texture.width != texture.height)
            {
                error = "ERROR Texture must be square! " + texture.width + "x" + texture.height;
                return null;
            }
            if (texture.width != cardFormat.requiredSize)
            {
                error = "ERROR Texture must match required size " + cardFormat.requiredSize;
                return null;
            }
            if (texture.format != TextureFormat.ARGB32 && texture.format != TextureFormat.RGB24)
            {
                if (cardFormat.useAlpha)
                {
                    if (texture.format == TextureFormat.DXT5)
                    {
                        return texture.GetRawTextureData();
                    }
                }
                else if (texture.format == TextureFormat.DXT1)
                {
                    return texture.GetRawTextureData();
                }
                error = "ERROR Texture format not supported for serialization! " + texture.format;
                return null;
            }

            Texture2D temp;
            byte[] data;
            if (cardFormat.useAlpha)
            {
                data = texture.GetRawTextureData();
                if (texture.format != TextureFormat.ARGB32)
                {

                    //RGB24 pad alpha
                    byte[] argb = new byte[texture.width * texture.width * 4];
                    int targIndex = 0;
                    int destIndex = 0;
                    for (int i = 0; i < texture.width * texture.width; i++)
                    {
                        argb[destIndex++] = 255;  //add alpha
                        argb[destIndex++] = data[targIndex++];
                        argb[destIndex++] = data[targIndex++];
                        argb[destIndex++] = data[targIndex++];
                    }
                    data = argb;
                }
                temp = new Texture2D(cardFormat.requiredSize, cardFormat.requiredSize, TextureFormat.ARGB32, false, true);

            }
            else
            {  //dont use alpha
                data = texture.GetRawTextureData();
                if (texture.format != TextureFormat.RGB24)
                {
                    //ARGB32 remove alpha
                    byte[] rgb = new byte[texture.width * texture.width * 3];
                    int targIndex = 0;
                    int destIndex = 0;
                    for (int i = 0; i < texture.width * texture.width; i++)
                    {
                        targIndex++;    //skip alpha
                        rgb[destIndex++] = data[targIndex++];
                        rgb[destIndex++] = data[targIndex++];
                        rgb[destIndex++] = data[targIndex++];
                    }
                    data = rgb;
                }
                temp = new Texture2D(cardFormat.requiredSize, cardFormat.requiredSize, TextureFormat.RGB24, false, true);
            }
            temp.LoadRawTextureData(data);
            temp.Apply();
            temp.Compress(false);
            byte[] compressedFormatted = temp.GetRawTextureData();
            GameDirector.Destroy(temp);
            return compressedFormatted;
        }

        public byte[] Serialize(Texture2D[] textures)
        {

            error = null;
            if (textures == null || formats == null)
            {
                error = "ERROR One or more parameters are null!";
                return null;
            }
            if (textures.Length != formats.Length)
            {
                error = "ERROR Texture array must match card texture format array!";
                return null;
            }

            byte[][] dataTable = new byte[formats.Length][];
            int payloadLength = 0;
            for (int i = 0; i < dataTable.Length; i++)
            {

                CardTextureFormat serializeFormat = formats[i];
                if (serializeFormat == null)
                {
                    error = "ERROR CardTextureFormat is null!";
                    return null;
                }
                Texture2D texture = textures[i];
                payloadLength += 4; //4 bytes for texture data length metadata
                if (texture == null)
                {
                    continue;   //keep dataTable entry is null
                }

                byte[] data = GetCompressedFormattedTextureData(texture, serializeFormat);
                if (data == null)
                {
                    return null;
                }
                payloadLength += data.Length;
                dataTable[i] = data;
            }
            //build payload
            byte[] payload = new byte[payloadLength + 1];   //1 byte for texture count
            payload[0] = (byte)formats.Length;    //metadata
            int payloadIndex = 1;
            for (int i = 0; i < dataTable.Length; i++)
            {
                byte[] data = dataTable[i];
                int dataLength;
                if (data == null)
                {
                    dataLength = 0;
                }
                else
                {
                    dataLength = data.Length;
                }
                byte[] metadata = System.BitConverter.GetBytes(dataLength);
                System.Array.Copy(metadata, 0, payload, payloadIndex, 4);
                payloadIndex += 4;
                if (data != null)
                {
                    System.Array.Copy(data, 0, payload, payloadIndex, data.Length);
                    payloadIndex += data.Length;
                }
            }
            return payload;
        }

        public Texture2D[] Deserialize(byte[] data)
        {

            error = null;
            if (formats == null || data == null)
            {
                error = "ERROR One or more parameters are null!";
                return null;
            }
            int byteIndex = 0;
            Texture2D[] textures = new Texture2D[data[byteIndex++]];  //read texture count metadata in first byte
            if (formats.Length != textures.Length)
            {
                error = "ERROR Card texture formats must match deserialized texture count " + formats.Length + "/" + textures.Length;
                return null;
            }
            for (int i = 0; i < textures.Length; i++)
            {
                CardTextureFormat serializeFormat = formats[i];
                byte[] dataLengthBytes = new byte[]{
                data[ byteIndex++ ],
                data[ byteIndex++ ],
                data[ byteIndex++ ],
                data[ byteIndex++ ]
            };

                int dataLength = System.BitConverter.ToInt32(dataLengthBytes, 0);
                if (dataLength == 0)
                {
                    continue;   //leave texture entry as null
                }
                TextureFormat format;
                if (serializeFormat.useAlpha)
                {
                    format = TextureFormat.DXT5;
                }
                else
                {
                    format = TextureFormat.DXT1;
                }
                Texture2D texture = new Texture2D(serializeFormat.requiredSize, serializeFormat.requiredSize, format, false, false);
                texture.wrapMode = TextureWrapMode.Clamp;

                byte[] textureData = new byte[dataLength];
                for (int j = 0; j < dataLength; j++)
                {
                    textureData[j] = data[byteIndex++];
                }
                texture.LoadRawTextureData(textureData);
                texture.Apply();

                textures[i] = texture;
            }
            return textures;
        }
    }


}