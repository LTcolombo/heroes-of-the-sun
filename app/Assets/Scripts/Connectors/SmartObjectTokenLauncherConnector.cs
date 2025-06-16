using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeityBot;
using DeityBot.Accounts;
using Smartobjecttokenlauncher.Accounts;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
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
        [Inject] private TokenConnector _token;

        protected override SmartObjectTokenLauncher DeserialiseBytes(byte[] value)
        {
            return SmartObjectTokenLauncher.Deserialize(value);
        }

        public override PublicKey GetComponentProgramAddress()
        {
            return new PublicKey("8va4yKEBACkT49C9wo94gS8ZaTdUrq2ipLgZvSNxWbd3");
        }

        public async Task<bool> Init(string token_name, string token_symbol, string token_uri, PublicKey mintPublicKey)
        {
            var systemAddress = new PublicKey("AdrPpoYr67ZcDZsQxsPgeosE3sQbZxercbUn8i1dcvap");
            return await ApplySystem(systemAddress,
                new { token_name, token_symbol, token_uri }, null, true,
                _token.GetCreateExtraAccounts(mintPublicKey, systemAddress));
        }

        public async Task<bool> Interact(int quantity, PublicKey mint)
        {
            var authority = Web3Utils.SessionToken == null
                ? Web3.Wallet.Account
                : Web3Utils.SessionWallet.Account;

            var systemAddress = new PublicKey("DUW1KczxcpeTEY7j9nkvcuAdWGNWoadTeDBKN5Z9xhst");
            var mintExtraAccounts = new List<AccountMeta>
            {
                AccountMeta.Writable(authority, true),
                AccountMeta.Writable(AssociatedTokenAccountProgram
                    .DeriveAssociatedTokenAccount(authority, mint), false),
                AccountMeta.Writable(mint, false),
                AccountMeta.Writable(Pda.GetMintAuthorityPDA(mint, systemAddress), false),
                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
            };

            if (Web3Utils.SessionWallet?.SessionTokenPDA != null)
            {
                mintExtraAccounts.Add(AccountMeta.ReadOnly(Web3Utils.SessionWallet?.SessionTokenPDA, false));
            }

            return await ApplySystem(systemAddress,
                new { quantity }, null, false, mintExtraAccounts.ToArray());
        }
    }
}