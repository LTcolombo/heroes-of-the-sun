using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeityBot;
using DeityBot.Accounts;
using Smartobjecttokenlauncher.Accounts;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Utils;
using Utils.Injection;
using View;

namespace Connectors
{
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SmartObjectTokenLauncherConnector : BaseComponentConnector<SmartObjectTokenLauncher>
    {
       
        protected override SmartObjectTokenLauncher DeserialiseBytes(byte[] value)
        {
            return SmartObjectTokenLauncher.Deserialize(value);
        }

        public override PublicKey GetComponentProgramAddress()
        {
            return new PublicKey("8va4yKEBACkT49C9wo94gS8ZaTdUrq2ipLgZvSNxWbd3");
        }

        public async Task<bool> Init(PublicKey mintPublicKey)
        {
            return await ApplySystem(new PublicKey("AdrPpoYr67ZcDZsQxsPgeosE3sQbZxercbUn8i1dcvap"),
                new { mint = mintPublicKey.KeyBytes.Select(b => (int)b).ToArray()}, null, true);
        }
    }
}