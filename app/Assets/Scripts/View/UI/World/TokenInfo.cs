using System.Collections;
using Model;
using Smartobjecttokenlauncher.Types;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Utils;
using Utils.Injection;
using View.UI.Building;

namespace View.UI.World
{
    public class TokenInfo : AnchoredUIPanel
    {
        [Inject] private PlayerHeroModel _player;
        [Inject] private TokenModel _token;

        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descText;
        [SerializeField] Image iconImage;

        [SerializeField] private TMP_Text resourceFood;
        [SerializeField] private TMP_Text resourceWood;
        [SerializeField] private TMP_Text resourceWater;
        [SerializeField] private TMP_Text resourceStone;

        [SerializeField] private TMP_Text goldPrice;

        [SerializeField] private NumericStepper numericStepper;
        [SerializeField] private Button confirmButton;

        private float _goldCost;
        private ResourceBalance _resourceCost;

        public void SetData(MetadataAccountV3 value, ResourceBalance cost, float goldCost)
        {
            _resourceCost = cost;
            _goldCost = goldCost;

            StartCoroutine(LoadMetadata(value.Uri));

            OnQuantityUpdated();
        }

        public void OnQuantityUpdated()
        {
            var backpack = _player.Get()?.Backpack;
            var goldBalance = _token.Get();

            var value = numericStepper.Value;
            int foodCost = 0, woodCost = 0, waterCost = 0, stoneCost = 0;
            if (_resourceCost != null && backpack != null)
            {
                foodCost = _resourceCost.Food * value;
                woodCost = _resourceCost.Wood * value;
                waterCost = _resourceCost.Water * value;
                stoneCost = _resourceCost.Stone * value;

                resourceFood.text = $"x{foodCost}";
                resourceFood.color = backpack.Food >= foodCost ? Color.white : Color.red;

                resourceWood.text = $"x{woodCost}";
                resourceWood.color = backpack.Wood >= woodCost ? Color.white : Color.red;

                resourceWater.text = $"x{waterCost}";
                resourceWater.color = backpack.Water >= waterCost ? Color.white : Color.red;

                resourceStone.text = $"x{stoneCost}";
                resourceStone.color = backpack.Stone >= stoneCost ? Color.white : Color.red;
            }

            goldPrice.text = $"{_goldCost * value:0.00}";
            goldPrice.color = goldBalance >= _goldCost * value ? Color.yellow : Color.red;

            // Determine if all costs are satisfied and set confirmButton.interactable
            var hasEnoughFood = backpack != null && backpack.Food >= foodCost;
            var hasEnoughWood = backpack != null && backpack.Wood >= woodCost;
            var hasEnoughWater = backpack != null && backpack.Water >= waterCost;
            var hasEnoughStone = backpack != null && backpack.Stone >= stoneCost;
            var hasEnoughGold = goldBalance >= _goldCost * value;

            confirmButton.interactable =
                hasEnoughFood && hasEnoughWood && hasEnoughWater && hasEnoughStone && hasEnoughGold;
        }

        private IEnumerator LoadMetadata(string url)
        {
            // Step 1: Load metadata JSON
            using var request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load metadata: " + request.error);
                yield break;
            }

            var json = request.downloadHandler.text;
            var metadata = JsonUtility.FromJson<ManageTokenCreation.SolanaMetadata>(json);

            // Update UI with name and description
            nameText.text = $"{metadata.name} <color=yellow>({metadata.symbol})</color>";
            descText.text = metadata.description;

            // Step 2: Load image
            using var imgRequest = UnityWebRequestTexture.GetTexture(metadata.image);
            yield return imgRequest.SendWebRequest();

            if (imgRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load image: " + imgRequest.error);
                yield break;
            }

            var texture = ((DownloadHandlerTexture)imgRequest.downloadHandler).texture;
            iconImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );
        }
    }
}