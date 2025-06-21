using System.Collections;
using Smartobjecttokenlauncher.Types;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Utils;
using View.UI.Building;

namespace View.UI.World
{
    public class TokenInfo : AnchoredUIPanel
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] TMP_Text descText;
        [SerializeField] Image iconImage;
        
        [SerializeField] private TMP_Text resourceFood;
        [SerializeField] private TMP_Text resourceWood;
        [SerializeField] private TMP_Text resourceWater;
        [SerializeField] private TMP_Text resourceStone;

        public void SetData(MetadataAccountV3 value, ResourceBalance cost)
        {
            StartCoroutine(LoadMetadata(value.Uri));

            if (cost != null)
            {
                resourceFood.text = $"x{cost.Food}";
                resourceWood.text = $"x{cost.Wood}";
                resourceWater.text = $"x{cost.Water}";
                resourceStone.text = $"x{cost.Stone}";
            }
            
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
            nameText.text =$"{metadata.name} <color=yellow>({metadata.symbol})</color>" ;
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
