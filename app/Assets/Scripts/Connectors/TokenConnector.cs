using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Model;
using Newtonsoft.Json;
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
            var authority = Web3Utils.SessionToken == null
                ? Web3.Wallet.Account
                : Web3Utils.SessionWallet.Account;
            
            return new[]
            {
                AccountMeta.Writable(authority, true),
                AccountMeta.Writable(new PublicKey(AssociatedTokenAccountSession), false),
                AccountMeta.Writable(new PublicKey(TokenMintPda), false),
                AccountMeta.ReadOnly(new PublicKey(TokenMinterProgramID), false),
                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false),
                AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false)
            };
        }

        public async Task<MetadataAccountV3> LoadMetadata(PublicKey mintAddress)
        {
            var metadata = GetCachedMetadata(mintAddress);
            if (metadata != null)
                return metadata;

            var metadataPda = PDALookup.FindMetadataPDA(mintAddress);

            var metadataAccountInfo = await Web3.Rpc.GetAccountInfoAsync(metadataPda, Commitment.Processed);
            if (!metadataAccountInfo.WasSuccessful || metadataAccountInfo.Result?.Value?.Data == null)
            {
                Debug.LogWarning("[Token Launcher] Unable to fetch metadata account: " + mintAddress);
                return null;
            }

            var rawData = Convert.FromBase64String(metadataAccountInfo.Result.Value.Data[0]);
            metadata = MetadataAccountV3.Deserialize(rawData);

            AddMetadataToCache(mintAddress, metadata);
            return metadata;
        }


        private const string Tag = "METADATA_CACHE";

        private static void AddMetadataToCache(PublicKey mintAddress, MetadataAccountV3 metadata)
        {
            PlayerPrefs.SetString($"{Tag}_{mintAddress}", JsonConvert.SerializeObject(metadata));
        }

        private static MetadataAccountV3 GetCachedMetadata(PublicKey mintAddress)
        {
            var metadataJson = PlayerPrefs.GetString($"{Tag}_{mintAddress}", null);
            return metadataJson == null ? null : JsonConvert.DeserializeObject<MetadataAccountV3>(metadataJson);
        }

        /// <summary>
        /// Returns the extra accounts needed for a hard currency transfer, matching the Rust program's requirements.
        /// </summary>
        /// <param name="paymentMint">The mint of the payment token.</param>
        /// <param name="vaultPda">The vault PDA.</param>
        /// <returns>Array of AccountMeta objects representing the required accounts.</returns>
        public AccountMeta[] GetTransferExtraAccounts(PublicKey vaultPda)
        {
            var authority = Web3Utils.SessionToken == null
                ? Web3.Wallet.Account
                : Web3Utils.SessionWallet.Account;

            var userAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(authority, new(TokenMintPda));
            var vaultAta = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(vaultPda, new(TokenMintPda));

            var accounts = new List<AccountMeta>
            {
                AccountMeta.Writable(new(TokenMintPda), false), // payment_mint_account
                AccountMeta.Writable(userAta, false), // payment_token_account
                AccountMeta.Writable(authority, true), // payment_token_authority
                AccountMeta.Writable(vaultAta, false), // destination_token_account
                AccountMeta.ReadOnly(vaultPda, false), // destination_pda
                AccountMeta.ReadOnly(TokenProgram.ProgramIdKey, false), // token_program
                AccountMeta.ReadOnly(AssociatedTokenAccountProgram.ProgramIdKey, false), // associated_token_program
                AccountMeta.ReadOnly(SystemProgram.ProgramIdKey, false) // system_program
            };

            return accounts.ToArray();
        }

        public async Task EnsureVaultAtaExists(PublicKey vaultPda)
        {
            var ata = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(vaultPda, new(TokenMintPda));

            var ataInfo = await Web3.Rpc.GetAccountInfoAsync(ata);
            if (ataInfo.Result?.Value == null)
            {
                Debug.Log($"Creating ATA for vault: {ata}");
                var blockHash = await Web3.Rpc.GetLatestBlockHashAsync();
                var tx = new TransactionBuilder()
                    .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                    .SetFeePayer(Web3.Wallet.Account)
                    .AddInstruction(
                        AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                            Web3.Wallet.Account, // payer
                            vaultPda, // owner (PDA)
                            new(TokenMintPda) // mint
                        )
                    )
                    .Build(new[] { Web3.Wallet.Account });

                var result = await Web3.Wallet.ActiveRpcClient.SendTransactionAsync(tx);
                if (!result.WasSuccessful)
                    throw new Exception($"ATA creation failed: {result.Reason}");
            }
        }

        public static float CalculateTokenPrice(ulong supply)
        {
            const float basePrice = 1f;
            const float coefficient = 0.05f;
            var fullTokens = supply / 1_000_000_000f;
            return basePrice + coefficient * fullTokens * fullTokens;
        }
    }
}