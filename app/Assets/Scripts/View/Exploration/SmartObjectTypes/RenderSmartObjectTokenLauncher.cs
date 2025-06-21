using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Connectors;
using Model;
using Notifications;
using Smartobjecttokenlauncher.Accounts;
using Solana.Unity.Metaplex.NFT.Library;
using Solana.Unity.Metaplex.Utilities;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;
using Utils.Injection;
using View.UI.World;

namespace View.Exploration.SmartObjectTypes
{
    public class RenderSmartObjectTokenLauncher : InjectableBehaviour, ISmartObject
    {
        [Inject] private SmartObjectTokenLauncherConnector _connector;
        [Inject] private TokenConnector _token;
        [Inject] private RequestInteractionWithSmartObject _interact;
        [Inject] private PlayerHeroModel _playerHero;
        [Inject] private HeroConnector _heroConnector;

        [SerializeField] private TokenInfo tokenInfo;
        private SmartObjectTokenLauncher _data;
        private string _cid;

        private void Start()
        {
            _interact.Add(OnInteractionRequest);
        }

        private void OnInteractionRequest(PublicKey value)
        {
            if (_connector.EntityPda != value) return;

            _ = Interact(value);
        }

        private async Task Interact(PublicKey value)
        {
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(await Web3.BlockHash(commitment: Commitment.Confirmed, useCache: false))
                .SetFeePayer(Web3.Account)
                
                .AddInstruction(
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        Web3.Account,
                        Web3.Account,
                        _data.Mint));
                
            var tx = Transaction.Deserialize(transaction.Build(new List<Account> { Web3.Account }));
            var res = await Web3.Wallet.SignAndSendTransaction(tx, true);
            Debug.Log("CreateAssociatedTokenAccount: " + res.Result);
            
            await _connector.Interact(1, _data.Mint);

            // var hero = _playerHero.Get();
            // _connector.Interact(1,  new Dictionary<PublicKey, PublicKey>()
            // {
            //     { hero.Owner, _heroConnector.GetComponentProgramAddress() }
            // });
        }

        public async Task SetEntity(string value)
        {
            await _connector.SetEntityPda(value, false);
            _data = await _connector.LoadData();

            try
            {
                Debug.Log("_data.Mint: " + _data.Mint);
                var data = await _token.LoadMetadata(_data.Mint);
                _cid = data.Uri.Split('/').Last();
                tokenInfo.SetData(data, _data.Recipe);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            await _connector.Subscribe(OnDataUpdate);
        }

        private void OnDataUpdate(SmartObjectTokenLauncher value)
        {
            _data = value;
        }

        private void OnDestroy()
        {
            _interact.Remove(OnInteractionRequest);
            _ = _connector.Unsubscribe();
        }
    }
}