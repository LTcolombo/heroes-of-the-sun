using System.Collections.Generic;
using System.Threading.Tasks;
using Connectors;
using Model;
using Solana.Unity.Wallet;
using UnityEngine;
using UnityEngine.UI;
using Utils.Injection;

namespace View.UI
{
    public class ManageBackpack : InjectableBehaviour
    {
        [Inject] private PlayerConnector _player;
        [Inject] private PlayerSettlementConnector _settlement;
        [Inject] private PlayerHeroModel _hero;
        [Inject] private HeroConnector _connector;

        [SerializeField] private Text resourceFood;
        [SerializeField] private Text resourceWood;
        [SerializeField] private Text resourceWater;
        [SerializeField] private Text resourceStone;

        private void Start()
        {
            _hero.Updated.Add(UpdateValues);
            _ = InitConnector();
        }

        async Task InitConnector()
        {
            await _connector.SetEntityPda(_player.EntityPda);
            //this is needed for connector to know its data account is delegated
            await _connector.LoadData();
            UpdateValues();
        }

        private void UpdateValues()
        {
            resourceFood.text = _hero.Get().Backpack.Food.ToString();
            resourceWood.text = _hero.Get().Backpack.Wood.ToString();
            resourceWater.text = _hero.Get().Backpack.Water.ToString();
            resourceStone.text = _hero.Get().Backpack.Stone.ToString();
        }

        public void OnSubmit()
        {
            _ = _connector.ChangeBackpack(
                int.Parse(resourceFood.text) - _hero.Get().Backpack.Food,
                int.Parse(resourceWood.text) - _hero.Get().Backpack.Wood,
                int.Parse(resourceWater.text) - _hero.Get().Backpack.Water,
                int.Parse(resourceStone.text) - _hero.Get().Backpack.Stone,
                new Dictionary<PublicKey, PublicKey>()
                {
                    {
                        new PublicKey(_settlement.EntityPda), _settlement.GetComponentProgramAddress()
                    }
                }
            );
        }

        private void OnDestroy()
        {
            _hero.Updated.Remove(UpdateValues);
        }
    }
}