using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Solana.Unity.Wallet;

namespace Connectors
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SmartObjectLocationConnector : BaseComponentConnector<SmartObjectLocation.Accounts.SmartObjectLocation>
    {
        protected override SmartObjectLocation.Accounts.SmartObjectLocation DeserialiseBytes(byte[] value)
        {
            return SmartObjectLocation.Accounts.SmartObjectLocation.Deserialize(value);
        }

        public override PublicKey GetComponentProgramAddress()
        {
            return new PublicKey("5ewDDvpaTkYvoE7ZJJ9cDmZuqvGQt65hsZSJ9w73Fzr1");
        }

        public async Task<bool> Init(int x, int y)
        {
            var entity = new PublicKey(EntityPda).KeyBytes.Select(b => (int)b).ToArray();
            return await ApplySystem(new PublicKey("64Uk4oF6mNyviUdK2xHXE3VMCtbCMDgRr1DMJk777DJZ"),
                new { x, y, entity }, null, true);
        }
    }
}