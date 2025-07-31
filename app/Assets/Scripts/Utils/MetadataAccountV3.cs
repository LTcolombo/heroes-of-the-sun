using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Utils
{
    using System;
    using System.IO;
    using System.Text;
    using Solana.Unity.Programs.Utilities;
    using Solana.Unity.Wallet;

    public class PublicKeyConverter : JsonConverter<PublicKey>
    {
        public override PublicKey ReadJson(JsonReader reader, Type objectType, PublicKey existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            // Handles case where JSON is a full object: { "Key": "...", "KeyBytes": "..." }
            if (token.Type == JTokenType.Object && token["Key"] != null)
            {
                var keyString = token["Key"].ToString();
                return new PublicKey(keyString);
            }

            // Handles simple string key case (if needed)
            if (token.Type == JTokenType.String)
            {
                return new PublicKey(token.ToString());
            }

            throw new JsonSerializationException("Unexpected token when parsing PublicKey.");
        }

        public override void WriteJson(JsonWriter writer, PublicKey value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Key");
            writer.WriteValue(value.Key);
            writer.WritePropertyName("KeyBytes");
            writer.WriteValue(Convert.ToBase64String(value.KeyBytes));
            writer.WriteEndObject();
        }
    }
    
    public class MetadataAccountV3
    {
        public byte Key;
        
        [JsonConverter(typeof(PublicKeyConverter))]
        public PublicKey UpdateAuthority;
        
        [JsonConverter(typeof(PublicKeyConverter))]
        public PublicKey Mint;
        public string Name;
        public string Symbol;
        public string Uri;
        public ushort SellerFeeBasisPoints;
        public bool PrimarySaleHappened;
        public bool IsMutable;
        public byte EditionNonce;

        public static MetadataAccountV3 Deserialize(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));

            var result = new MetadataAccountV3();

            result.Key = reader.ReadByte();
            result.UpdateAuthority = new PublicKey(reader.ReadBytes(32));
            result.Mint = new PublicKey(reader.ReadBytes(32));

            result.Name = ReadRustString(reader);
            result.Symbol = ReadRustString(reader);
            result.Uri = ReadRustString(reader);

            result.SellerFeeBasisPoints = reader.ReadUInt16();

            var creatorsPresent = reader.ReadByte();
            if (creatorsPresent == 1)
            {
                var creatorsLength = reader.ReadInt32();
                for (int i = 0; i < creatorsLength; i++)
                {
                    reader.ReadBytes(32); // address
                    reader.ReadByte();    // verified
                    reader.ReadByte();    // share
                }
            }

            result.PrimarySaleHappened = reader.ReadByte() == 1;
            result.IsMutable = reader.ReadByte() == 1;
            result.EditionNonce = reader.ReadByte();

            // Optionally skip tokenStandard and collection fields for now

            return result;
        }

        private static string ReadRustString(BinaryReader reader)
        {
            int length = reader.ReadInt32();
            byte[] bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        }
    }
}