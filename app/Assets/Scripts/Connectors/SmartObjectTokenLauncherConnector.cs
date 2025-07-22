using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Model;
using Smartobjecttokenlauncher.Accounts;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Utils;
using Utils.Injection;

namespace Connectors
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class SmartObjectTokenLauncherConnector : BaseComponentConnector<SmartObjectTokenLauncher>
    {
        [Inject] private TokenConnector _token;
        [Inject] private HeroConnector _hero;
        [Inject] private PlayerHeroModel _playerHero;
        [Inject] private PlayerConnector _player;

        protected override SmartObjectTokenLauncher DeserialiseBytes(byte[] value)
        {
            return SmartObjectTokenLauncher.Deserialize(value);
        }

        protected override TransactionInstruction GetUndelegateIx(PublicKey playerDataPda)
        {
            throw new System.NotImplementedException();
        }

        public override PublicKey GetComponentProgramAddress()
        {
            return new PublicKey("8va4yKEBACkT49C9wo94gS8ZaTdUrq2ipLgZvSNxWbd3");
        }

        public async Task<bool> Init(string token_name, string token_symbol, string token_uri, PublicKey mintPublicKey,
            ushort recipe_food, ushort recipe_water, ushort recipe_wood, ushort recipe_stone)
        {
            var systemAddress = new PublicKey("AdrPpoYr67ZcDZsQxsPgeosE3sQbZxercbUn8i1dcvap");
            return await ApplySystem(systemAddress,
                new { token_name, token_symbol, token_uri, recipe_food, recipe_water, recipe_wood, recipe_stone }, null,
                _token.GetCreateExtraAccounts(mintPublicKey, systemAddress));
        }

        public async Task<bool> Interact(int quantity, PublicKey mint)
        {
            await _token.EnsureVaultAtaExists(new(DataAddress));

            await _hero.SetEntityPda(_player.EntityPda, false);
            var data = await _hero.LoadData();

            //undelegate
            await _hero.Undelegate();

            var systemAddress = new PublicKey("DUW1KczxcpeTEY7j9nkvcuAdWGNWoadTeDBKN5Z9xhst");

            var result = await ApplySystem(systemAddress,
                new { quantity }, new Dictionary<PublicKey, PublicKey>()
                {
                    {
                        new PublicKey(_hero.EntityPda), _hero.GetComponentProgramAddress()
                    }
                }, GetMintExtraAccounts(systemAddress, mint)
                    .Concat(_token.GetTransferExtraAccounts(new(DataAddress))).ToArray());


            //re-delegate
            await _hero.Delegate();

            //copy to ER
            await _hero.Move(data.X, data.Y);

            return result;
        }

        private AccountMeta[] GetMintExtraAccounts(PublicKey systemAddress, PublicKey mint)
        {
            var authority = Web3Utils.SessionToken == null
                ? Web3.Wallet.Account
                : Web3Utils.SessionWallet.Account;

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

            return mintExtraAccounts.ToArray();
        }
    }
}