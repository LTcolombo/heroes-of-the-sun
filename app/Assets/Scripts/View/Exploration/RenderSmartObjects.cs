using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Connectors;
using Model;
using Notifications;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using UnityEngine;
using Utils.Injection;

namespace View.Exploration
{
    public class RenderSmartObjects : InjectableBehaviour
    {
        [Inject] private PathfindingModel _pathfinding;
        [Inject] private SmartObjectLocationConnector _connector;
        
        [Inject] private RedrawSmartObjects _redraw;

        [SerializeField] private RenderSmartObject prefab;

        private void Start()
        {
            _ = Redraw();
            _redraw.Add(OnRedrawRequest);
        }

        private void OnRedrawRequest()
        {
            _ = Redraw();
        }

        private async Task Redraw()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            var list = new List<Solana.Unity.Rpc.Models.MemCmp>
                { new() { Bytes = SmartObjectLocation.Accounts.SmartObjectLocation.ACCOUNT_DISCRIMINATOR_B58, Offset = 0 } };

            var accounts = (await Web3.Rpc.GetProgramAccountsAsync(
                _connector.GetComponentProgramAddress(), Commitment.Confirmed, memCmpList: list)).Result;

            foreach (var account in accounts)
            {
                var renderSmartObject = Instantiate(prefab, transform);
                await renderSmartObject.SetDataAddress(account.PublicKey);
            }
        }

        private void OnDestroy()
        {
            _redraw.Remove(OnRedrawRequest);
        }
    }
}