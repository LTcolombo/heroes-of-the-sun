using System;
using System.Threading.Tasks;
using Connectors;
using Smartobjecttokenlauncher.Accounts;
using UnityEngine;
using Utils.Injection;
using View.UI.World;

namespace View.Exploration.SmartObjectTypes
{
    public class RenderSmartObjectTokenLauncher : InjectableBehaviour, ISmartObject
    {
        [Inject] private SmartObjectTokenLauncherConnector _connector;
        [Inject] private TokenConnector _token;

        [SerializeField] private TokenInfo tokenInfo;
        private SmartObjectTokenLauncher _data;

        public async Task SetEntity(string value)
        {
            await _connector.SetEntityPda(value, false);
            _data = await _connector.LoadData();

            try
            {
                var data = await _token.LoadMetadata(_data.Mint);
                tokenInfo.SetData(data);
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
            _ = _connector.Unsubscribe();
        }
    }
}