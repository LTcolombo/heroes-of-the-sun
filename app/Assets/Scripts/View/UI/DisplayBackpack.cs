using Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils.Injection;
using View.UI.Building;

namespace View.UI
{
    public class DisplayBackpack : AnchoredUIPanel
    {
        [Inject] private PlayerHeroModel _hero;

        [SerializeField] private TMP_Text resourceFood;
        [SerializeField] private TMP_Text resourceWood;
        [SerializeField] private TMP_Text resourceWater;
        [SerializeField] private TMP_Text resourceStone;

        protected override void Start()
        {
            base.Start();
            _hero.Updated.Add(UpdateValues);
            UpdateValues();
        }

        private void UpdateValues()
        {
            if (_hero.HasData)
            {
                resourceFood.text = $"x{_hero.Get().Backpack.Food}";
                resourceWood.text = $"x{_hero.Get().Backpack.Wood}";
                resourceWater.text = $"x{_hero.Get().Backpack.Water}";
                resourceStone.text = $"x{_hero.Get().Backpack.Stone}";
            }
        }

        private void OnDestroy()
        {
            _hero.Updated.Remove(UpdateValues);
        }
    }
}