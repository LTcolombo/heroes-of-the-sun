namespace Utils
{
    using System;
    using System.IO;
    using System.Text;
    using Solana.Unity.Programs.Utilities;
    using Solana.Unity.Wallet;

    public class MetadataAccountV3
    {
        public byte Key;
        public PublicKey UpdateAuthority;
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