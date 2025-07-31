using System;
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
        private bool _metadataLoaded;

        protected override void Awake()
        {
            base.Awake();
            CacheDir = System.IO.Path.Combine(Application.persistentDataPath, "cache");
        }
        
        public void SetData(MetadataAccountV3 value, ResourceBalance cost, float goldCost)
        {
            _resourceCost = cost;
            _goldCost = goldCost;

            if (!_metadataLoaded)
                StartCoroutine(LoadMetadata(value.Uri));

            OnQuantityUpdated();
            
            _player.Updated.Add(OnQuantityUpdated);
            _token.Updated.Add(OnQuantityUpdated);
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

        private static string CacheDir;

        private IEnumerator LoadMetadata(string url)
        {
            if (!System.IO.Directory.Exists(CacheDir))
                System.IO.Directory.CreateDirectory(CacheDir);

            bool isWebGL = Application.platform == RuntimePlatform.WebGLPlayer;

            var cid = ExtractCidFromUrl(url);
            var jsonPath = System.IO.Path.Combine(CacheDir, $"{cid}.meta.json");
            string json;

            if (!isWebGL && System.IO.File.Exists(jsonPath))
            {
                json = System.IO.File.ReadAllText(jsonPath);
            }
            else
            {
                using var request = UnityWebRequest.Get(url);
                request.downloadHandler = new DownloadHandlerBuffer();
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to load metadata: " + request.error);
                    yield break;
                }

                json = request.downloadHandler.text;

                if (!isWebGL)
                {
                    try
                    {
                        System.IO.File.WriteAllText(jsonPath, json);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("Failed to cache metadata: " + e.Message);
                    }
                }
            }

            var metadata = JsonUtility.FromJson<ManageTokenCreation.SolanaMetadata>(json);
            nameText.text = $"{metadata.name} <color=yellow>({metadata.symbol})</color>";
            descText.text = metadata.description;

            var imgCid = ExtractCidFromUrl(metadata.image);
            var imgPath = System.IO.Path.Combine(CacheDir, $"{imgCid}.image.png");
            Texture2D texture;

            if (!isWebGL && System.IO.File.Exists(imgPath))
            {
                var imgBytes = System.IO.File.ReadAllBytes(imgPath);
                texture = new Texture2D(2, 2);
                texture.LoadImage(imgBytes);
            }
            else
            {
                using var imgRequest = UnityWebRequestTexture.GetTexture(metadata.image);
                yield return imgRequest.SendWebRequest();

                if (imgRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("Failed to load image: " + imgRequest.error);
                    yield break;
                }

                texture = ((DownloadHandlerTexture)imgRequest.downloadHandler).texture;

                if (!isWebGL)
                {
                    try
                    {
                        var pngBytes = texture.EncodeToPNG();
                        System.IO.File.WriteAllBytes(imgPath, pngBytes);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning("Failed to cache image: " + e.Message);
                    }
                }
            }

            iconImage.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            _metadataLoaded = true;
        }

        private string ExtractCidFromUrl(string url)
        {
            var parts = url.Split(new[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
            var ipfsIndex = System.Array.IndexOf(parts, "ipfs");
            if (ipfsIndex >= 0 && ipfsIndex + 1 < parts.Length)
            {
                return parts[ipfsIndex + 1];
            }
            Debug.LogWarning("Could not extract CID from URL: " + url);
            return System.IO.Path.GetFileNameWithoutExtension(url); // fallback
        }

        private void OnDestroy()
        {
            _player.Updated.Remove(OnQuantityUpdated);
            _token.Updated.Remove(OnQuantityUpdated);
        }
    }
}