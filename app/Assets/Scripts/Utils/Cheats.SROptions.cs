using Connectors;
using Model;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils.Injection;
using Utils;

namespace StompyRobot.SROptions
{
    public partial class SROptions
    {
        [Inject] private PlayerSettlementConnector _connector;
        [Inject] private SettlementModel _model;

        public SROptions()
        {
            Injector.Instance.Resolve(this);
        }
        
        public void ClearPreferences()
        {
            PlayerPrefs.DeleteAll();
            SceneManager.LoadScene("Loading");
        }

        public async void Reset()
        {
            if (await _connector.Reset())
                _model.Set(await _connector.LoadData());
        }

        public async void ClaimLoot()
        {
            var connector = (LootDistributionConnector)Injector.Instance.GetValue(typeof(LootDistributionConnector));
            await connector.Claim(0);
        }
    }
}