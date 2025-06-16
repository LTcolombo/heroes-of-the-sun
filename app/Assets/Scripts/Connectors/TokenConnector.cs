using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TokenMinter;
using TokenMinter.Program;
using UnityEngine;
using Utils;
using Utils.Injection;


namespace Connectors
{
    [Singleton]
    public class TokenConnector : InjectableObject
    {
        [Inject] private TokenModel _model;

        public const string TokenMintPda = "Fn7ndp5EocCfzDkFMdWUZj5B55AoM7nA5o5cXSUbtDrn";
        private const string TokenMinterProgramID = "4ZxRnucEWC62kVktmx27cz9d1PzWWNgiZLT5VWFLbfB2";

        private string _ata;

        public string AssociatedTokenAccount => _ata ??=
            AssociatedTokenAccountProgram
                .DeriveAssociatedTokenAccount(Web3.Account, new PublicKey(TokenMintPda));


        public string AssociatedTokenAccountSession
        {
            get
            {
                var authority = Web3Utils.SessionToken == null
                    ? Web3.Wallet.Account
                    : Web3Utils.SessionWallet.Account;

                return AssociatedTokenAccountProgram
                    .DeriveAssociatedTokenAccount(authority, new PublicKey(TokenMintPda));
            }
        }

        public async Task LoadData()
        {
            var accountInfo = await Web3.Wallet.ActiveRpcClient.GetAccountInfoAsync(AssociatedTokenAccount);
            if (accountInfo.Result.Value != null)
                _model.Set(AccountLayout.DeserializeAccountLayout(accountInfo.Result.Value.Data[0]));
        }

        public async Task Subscribe(Action<SubscriptionState, ResponseValue<AccountInfo>, AccountInfo> callback)
        {
            await Web3.Wallet.ActiveStreamingRpcClient.SubscribeAccountInfoAsync(AssociatedTokenAccount,
                (s, accountInfo) => { _model.Set(AccountLayout.DeserializeAccountLayout(accountInfo.Value.Data[0])); },
                Commitment.Processed);
        }

        public AccountMeta[] GetCreateExtraAccounts(PublicKey mint, PublicKey system)
        {
            var authority = Web3Utils.SessionToken == null
                ? Web3.Wallet.Account
                : Web3Utils.SessionWallet.Account;

            var extraAccounts = new List<AccountMeta>
            {
                AccountMeta.Writable(authority, true),
                AccountMeta.Writable(mint, false),
                AccountMeta.Writable(PDALookup.FindMetadataPDA(mint), false),
                AccountMeta.Writable(Pda.GetMintAuthorityPDA(mint, system), false),
                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(MetadataProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(new PublicKey("SysvarRent111111111111111111111111111111111"), false),
            };

            if (Web3Utils.SessionWallet?.SessionTokenPDA != null)
            {
                extraAccounts.Add(AccountMeta.ReadOnly(Web3Utils.SessionWallet?.SessionTokenPDA, false));
            }

            return extraAccounts.ToArray();
        }

        public AccountMeta[] GetMintExtraAccounts()
        {
            var authority = Web3Utils.SessionToken == null
                ? Web3.Wallet.Account
                : Web3Utils.SessionWallet.Account;

            var mintExtraAccounts = new List<AccountMeta>
            {
                AccountMeta.Writable(authority, true),
                AccountMeta.Writable(new PublicKey(AssociatedTokenAccountSession), false),
                AccountMeta.Writable(new PublicKey(TokenMintPda), false),
                AccountMeta.ReadOnly(new PublicKey(TokenMinterProgramID), false),
                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false)
            };

            if (Web3Utils.SessionWallet?.SessionTokenPDA != null)
            {
                mintExtraAccounts.Add(AccountMeta.ReadOnly(Web3Utils.SessionWallet?.SessionTokenPDA, false));
            }

            return mintExtraAccounts.ToArray();
        }

        public AccountMeta[] GetBurnExtraAccounts()
        {
            var authority = true // Web3Utils.SessionToken == null
                ? Web3.Wallet.Account
                : Web3Utils.SessionWallet.Account;


            return new[]
            {
                AccountMeta.Writable(authority, true),
                AccountMeta.Writable(new PublicKey(AssociatedTokenAccount), false),
                AccountMeta.Writable(new PublicKey(TokenMintPda), false),
                AccountMeta.ReadOnly(new PublicKey(TokenMinterProgramID), false),
                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false)
            };
        }

        public async Task<MetadataAccountV3> LoadMetadata(PublicKey mintAddress)
        {
            var metadataPda = PDALookup.FindMetadataPDA(mintAddress);

            var metadataAccountInfo = await Web3.Rpc.GetAccountInfoAsync(metadataPda);
            if (!metadataAccountInfo.WasSuccessful || metadataAccountInfo.Result?.Value?.Data == null)
            {
                throw new Exception("Unable to fetch metadata account");
            }

            var rawData = Convert.FromBase64String(metadataAccountInfo.Result.Value.Data[0]);
            return MetadataAccountV3.Deserialize(rawData);
        }
    }
}