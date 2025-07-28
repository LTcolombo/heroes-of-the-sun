using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Connectors;
using Hero.Program;
using Solana.Unity.Rpc.Models;
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

            var loaded = new HashSet<string>() { renderHero.DataAddress };

            var list = new List<MemCmp>
                { new() { Bytes = Hero.Accounts.Hero.ACCOUNT_DISCRIMINATOR_B58, Offset = 0 } };

            var accounts = new List<AccountKeyPair>();

            foreach (var hero in (await Web3Utils.EphemeralWallet.ActiveRpcClient.GetProgramAccountsAsync(
                         new PublicKey(HeroProgram.ID), Commitment.Confirmed, memCmpList: list)).Result)
            {
                if (loaded.Contains(hero.PublicKey))
                    continue;
                
                accounts.Add(hero);
            }
            
            foreach (var hero in (await Web3.Wallet.ActiveRpcClient.GetProgramAccountsAsync(
                         new PublicKey(HeroProgram.ID), Commitment.Confirmed, memCmpList: list)).Result)
            {
                if (loaded.Contains(hero.PublicKey))
                    continue;
                
                accounts.Add(hero);
            }

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
                    Destroy(renderHero);
                    Debug.LogError(e);
                }
            }
        }
    }
}