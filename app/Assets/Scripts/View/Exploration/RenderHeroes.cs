using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Connectors;
using Hero.Program;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;
using Utils;
using Utils.Injection;

namespace View.Exploration
{
    public class RenderHeroes : InjectableBehaviour
    {
        [Inject] private PlayerConnector _player;
        [SerializeField] private RenderHero prefab;

        void Start()
        {
            _ = Init();
        }

        private async Task Init()
        {
            //load own hero first
            var renderHero = Instantiate(prefab, transform);
            await renderHero.SetEntity(_player.EntityPda);
            
            //load others after
            
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>
                { new() { Bytes = Hero.Accounts.Hero.ACCOUNT_DISCRIMINATOR_B58, Offset = 0 } };

            var accounts = (await Web3.Rpc.GetProgramAccountsAsync(
                new PublicKey(HeroProgram.ID), Commitment.Confirmed, memCmpList: list)).Result;

            //concat with rollup accounts            
            accounts = accounts.Concat((await Web3Utils.EphemeralWallet.ActiveRpcClient.GetProgramAccountsAsync(
                new PublicKey(HeroProgram.ID), Commitment.Confirmed, memCmpList: list)).Result).ToList();

            foreach (var account in accounts)
            {
                if (account.PublicKey == renderHero.DataAddress)
                    continue;
                
                try
                {
                    renderHero = Instantiate(prefab, transform);
                    await renderHero.SetDataAddress(account.PublicKey);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }
    }
}