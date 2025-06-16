using System;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Solana.Unity.Programs;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
using World;
using World.Program;

namespace Connectors
{
    
    public class Pda
    {
        public static PublicKey FindWorldPda(int world)
        {
            PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("world"), BitConverter.GetBytes((ulong)world).Reverse().ToArray()
            }, new PublicKey(WorldProgram.ID), out var pda, out _);
            return pda;
        }

        public static PublicKey FindEntityPda(int world, int entity, string extraSeed = "")
        {
            PublicKey.TryFindProgramAddress(new[]
            {
                Encoding.UTF8.GetBytes("entity"), BitConverter.GetBytes((ulong)world).Reverse().ToArray(),
                BitConverter.GetBytes((ulong)entity).Reverse().ToArray(), Encoding.UTF8.GetBytes(extraSeed)
            }, new PublicKey(WorldProgram.ID), out var pda, out _);
            return pda;
        }

        public static PublicKey FindComponentPda(PublicKey entity, PublicKey componentProgram)
        {
            return FindComponentPda(null, entity, componentProgram);
        }

        private static PublicKey FindComponentPda(
            [CanBeNull] string componentId,
            PublicKey entity,
            PublicKey componentProgram)
        {
            if (string.IsNullOrEmpty(componentId))
            {
                PublicKey.TryFindProgramAddress(new[]
                {
                    entity.KeyBytes
                }, componentProgram, out var pda, out _);
                return pda;
            }
            else
            {
                PublicKey.TryFindProgramAddress(new[]
                {
                    Encoding.UTF8.GetBytes(componentId), entity.KeyBytes
                }, componentProgram, out var pda, out _);
                return pda;
            }
        }
        
        public static (PublicKey pda, byte[] seed, byte bump) GetPDAFromCid(string cid)
        {
            // 2. Decode base58 CID
            byte[] cidBytes = Encoding.UTF8.GetBytes(cid);
        
            // 3. Use a prefix to namespace the seed
            byte[] prefix = Encoding.UTF8.GetBytes("cid");
        
            // 4. Truncate CID to 32 bytes if needed
            byte[] seed = new byte[32];
            Array.Copy(cidBytes, seed, Math.Min(cidBytes.Length, 32));
        
            // 5. Compute PDA
            PublicKey.TryFindProgramAddress(
                new[] { prefix, seed },
                new("DUW1KczxcpeTEY7j9nkvcuAdWGNWoadTeDBKN5Z9xhst"),
                out PublicKey pda, out  byte bump
            );
        
            Console.WriteLine($"Derived PDA: {pda}");
            Console.WriteLine($"Bump: {bump}");
        
            return (pda, seed, bump);
        }
        
        public static PublicKey GetMintAuthorityPDA(PublicKey mint, PublicKey system)
        {
           
            PublicKey.TryFindProgramAddress(
                new[] { Encoding.UTF8.GetBytes("authority"), mint.KeyBytes },
                system,
                out PublicKey pda, out  byte bump
            );
        
            Console.WriteLine($"Derived PDA: {pda.KeyBytes}");
            Console.WriteLine($"Bump: {bump}");
        
            return pda;
        }

    }
}