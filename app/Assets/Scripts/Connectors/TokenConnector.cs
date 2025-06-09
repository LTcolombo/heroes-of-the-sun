using System;
using System.Collections.Generic;
using System.IO;
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


        private string AssociatedTokenAccountSession
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

        public async Task<PublicKey> CreateToken(string name, string symbol, string metadataUrl)
        {
            var mint = new Account();
            var associatedTokenAccount = AssociatedTokenAccountProgram
                .DeriveAssociatedTokenAccount(Web3.Account, mint.PublicKey);

            var metadata = new Metadata()
            {
                name = name,
                symbol = symbol,
                uri = metadataUrl,
                sellerFeeBasisPoints = 0,
                creators = new List<Creator> { new(Web3.Account.PublicKey, 100, true) }
            };

            var minimumRent = await Web3.Rpc.GetMinimumBalanceForRentExemptionAsync(TokenProgram.MintAccountDataSize);


            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(await Web3.BlockHash(commitment: Commitment.Confirmed, useCache: false))
                .SetFeePayer(Web3.Account)
                .AddInstruction(
                    SystemProgram.CreateAccount(
                        Web3.Account,
                        mint.PublicKey,
                        minimumRent.Result,
                        TokenProgram.MintAccountDataSize,
                        TokenProgram.ProgramIdKey))
                .AddInstruction(
                    TokenProgram.InitializeMint(
                        mint.PublicKey,
                        0,
                        Web3.Account,
                        Web3.Account))
                .AddInstruction(
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        Web3.Account,
                        Web3.Account,
                        mint.PublicKey))
                .AddInstruction(
                    TokenProgram.MintTo(
                        mint.PublicKey,
                        associatedTokenAccount,
                        1,
                        Web3.Account))
                .AddInstruction(MetadataProgram.CreateMetadataAccount(
                    PDALookup.FindMetadataPDA(mint),
                    mint.PublicKey,
                    Web3.Account,
                    Web3.Account,
                    Web3.Account.PublicKey,
                    metadata,
                    TokenStandard.NonFungible,
                    true,
                    true,
                    null,
                    metadataVersion: MetadataVersion.V3))
                .AddInstruction(MetadataProgram.CreateMasterEdition(
                        maxSupply: null,
                        masterEditionKey: PDALookup.FindMasterEditionPDA(mint),
                        mintKey: mint,
                        updateAuthorityKey: Web3.Account,
                        mintAuthority: Web3.Account,
                        payer: Web3.Account,
                        metadataKey: PDALookup.FindMetadataPDA(mint),
                        version: CreateMasterEditionVersion.V3
                    )
                );

            var tx = Transaction.Deserialize(transaction.Build(new List<Account> { Web3.Account, mint }));
            var res = await Web3.Wallet.SignAndSendTransaction(tx);
            Debug.Log(res.Result);
            Debug.Log("mint: " + mint.PublicKey);
            
            return mint.PublicKey;
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