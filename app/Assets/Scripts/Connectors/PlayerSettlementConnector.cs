using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Model;
using Notifications;
using Solana.Unity.Rpc.Models;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;
using Utils.Injection;
using Utils;

// ReSharper disable InconsistentNaming

namespace Connectors
{
    [Singleton]
    public class PlayerSettlementConnector : SettlementConnector
    {
        [Inject] private StopFtueSequence _stopFtue;
        [Inject] private TokenConnector _token;

        protected override UniTask<bool> ApplySystem(PublicKey systemAddress, object args,
            Dictionary<PublicKey, PublicKey> extraEntities = null,
            AccountMeta[] accounts = null, bool forceMainWalletSigner = false)
        {
            _stopFtue.Dispatch();
            return base.ApplySystem(systemAddress, args, extraEntities, accounts, forceMainWalletSigner);
        }

        public async UniTask<bool> Build(byte x, byte y, byte type, int worker_index)
        {
            return await ApplySystem(new PublicKey("fkiWK1Wn6ouGcHb3icX4XGKynef5MpsTQ478ZMdgB1g"),
                new { x, y, config_index = type, worker_index });
        }

        public async UniTask<bool> Wait(int time)
        {
            return await ApplySystem(new PublicKey("9F6qiZPUWN3bCnr5uVBwSmEDf8QcAFHNSVDH8L7AkZe4"),
                new { time });
        }

        public async UniTask<bool> AssignWorker(int worker_index, int building_index)
        {
            return await ApplySystem(new PublicKey("BExuAEwcKxKeqHSN8C1WetUAd6Tm71cZEiP8EBSrH55T"),
                new { worker_index, building_index });
        }


        public async UniTask<bool> Repair(int index)
        {
            return await ApplySystem(new PublicKey("5xPJt6GDcmGphNAs6qU3hWAvzLwXuSqhTco6RtoAR9aY"), new { index });
        }

        public async UniTask<bool> Upgrade(int index, int worker_index)
        {
            return await ApplySystem(new PublicKey("76wsz7SjNtvoFK8aUvojEyfjep5pMSaHQihGVxcjc1EA"),
                new { index, worker_index });
        }

        public async UniTask<bool> ClaimTime()
        {
            return await base.ApplySystem(new PublicKey("4XXA1mX5aN4Fd62FBgNxCU7FzKDYS3KSxFX3RdJYoWPj"), new { });
        }

        public async UniTask<bool> Research(int research_type)
        {
            return await ApplySystem(new PublicKey("3ZJ7mgXYhqQf7EsM8q5Ea5YJWA712TFyWGvrj9mRL2gP"),
                new { research_type }); //, null, false, _token.GetBurnExtraAccounts());
        }

        public async UniTask<bool> Sacrifice(int index)
        {
            return await ApplySystem(new PublicKey("6JwZJNAtkciXVGenFSoa99VBNcxyb2W8mvzcMK1vTWKs"),
                new { index });
        }

        public async UniTask<bool> Reset()
        {
            return await ApplySystem(new PublicKey("3VEXJoAZkYxDXigSWso8FnJY8z6C6inpPxU798vqc9um"),
                new { });
        }

        public async UniTask<bool> Exchange(int tokens_for_food, int tokens_for_water, int tokens_for_wood,
            int tokens_for_stone)
        {
            //undelegate
            await Undelegate();

            // if (Web3Utils.SessionWallet != null)
            // {
            //     var tokensTotal = 1000000000 *
            //                       (ulong)(tokens_for_food + tokens_for_water + tokens_for_wood + tokens_for_stone);
            //     var transfer = await Web3.Wallet.Transfer(Web3Utils.SessionWallet.Account.PublicKey,
            //         new PublicKey(TokenConnector.TokenMintPda), tokensTotal);
            //
            //     Debug.Log($"transfer.Result: {transfer.Result}");
            // }

            //2. apply
            var result = await ApplySystem(new PublicKey("Csna3V2jUMdQEQKUCxLsQEnYThAGPSWcPCxW9vea1S8d"),
                new { tokens_for_food, tokens_for_water, tokens_for_wood, tokens_for_stone }, null,
                _token.GetBurnExtraAccounts(), true);

            //re-delegate
            await Delegate();

            //claim time (to copy latest state to ER)
            await ClaimTime();

            return result;
        }

        public async UniTask<bool> ClaimQuest(int index)
        {
            return await ApplySystem(new PublicKey("7nsn4z8U1nVCVHud9CLmYLy5ZHK2bSMge6u7YgmssdaA"), new { index });
        }


        public override UniTask CloneToRollup()
        {
            return ClaimTime();
        }
    }
}