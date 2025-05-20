using System.Collections;
using System.Text;
using Connectors;
using Solana.Unity.SDK;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Utils.Injection;

namespace View.UI
{
    public class ManageTokenCreation : InjectableBehaviour
    {
        public Image icon;
        public InputField nameInput;
        public InputField symbolInput;
        public InputField descriptionInput;
        [Inject] private TokenConnector _token;
        
        // Pinata V3 Upload API + JWT
        private const string PinataJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySW5mb3JtYXRpb24iOnsiaWQiOiJmYTVjZGNjNS1hZTYxLTQzMjMtYmZmYi04MGI3MTllYzk0MmQiLCJlbWFpbCI6Im4uYmFseWFuaXRzYUBnbWFpbC5jb20iLCJlbWFpbF92ZXJpZmllZCI6dHJ1ZSwicGluX3BvbGljeSI6eyJyZWdpb25zIjpbeyJkZXNpcmVkUmVwbGljYXRpb25Db3VudCI6MSwiaWQiOiJGUkExIn1dLCJ2ZXJzaW9uIjoxfSwibWZhX2VuYWJsZWQiOmZhbHNlLCJzdGF0dXMiOiJBQ1RJVkUifSwiYXV0aGVudGljYXRpb25UeXBlIjoic2NvcGVkS2V5Iiwic2NvcGVkS2V5S2V5IjoiMTVmNzEzZGJkOTk5YjZkZDZmMTIiLCJzY29wZWRLZXlTZWNyZXQiOiIyODQwMzVkZWJlZjNiZmVkZmRiNGVmZmZiMDU5ODg4NzFmZjczNGJhY2RjYzU3MjRhODVjYjZjNzBhYTkzMmM1IiwiZXhwIjoxNzc5MTQwNjU2fQ.NtPtNGnlL72X_2GCFn-T-PkAZr4moD34fCVZmrssFOU";
        private const string PinataV3UploadUrl = "https://uploads.pinata.cloud/v3/files";

        [SerializeField] private Sprite[] icons;
        private int _index;

        private void OnEnable()
        {
            _index = 0;
            NextIcon();
        }

        public void NextIcon()
        {
            icon.sprite = icons[_index++ % icons.Length];
        }
    
        public void OnSubmit()
        {
            StartCoroutine(UploadImageAndMetadataV3());
        }

        private IEnumerator UploadImageAndMetadataV3()
        {
            var texture = icon.sprite.texture;
            var pngData = texture.EncodeToPNG();
            string imageIpfsUrl = null;
            yield return UploadFileToPinataV3(pngData, "image.png", "image/png", cid => imageIpfsUrl = cid);

            var metadata = new SolanaMetadata
            {
                name = nameInput.text,
                symbol = symbolInput.text,
                description = descriptionInput.text,
                image = imageIpfsUrl,
                properties = new MetadataProperties
                {
                    category = "image",
                    creators = new[]
                    {
                        new Creator { address = Web3.Wallet.Account.PublicKey, share = 100 }
                    }
                }
            };

            var metadataJson = JsonUtility.ToJson(metadata, true);
            var metadataBytes = Encoding.UTF8.GetBytes(metadataJson);
            string metadataIpfsUrl = null;
            yield return UploadFileToPinataV3(metadataBytes, "metadata.json", "application/json", cid => metadataIpfsUrl = cid);

            _ = _token.CreateToken(metadataIpfsUrl);
        }

        private IEnumerator UploadFileToPinataV3(byte[] fileData, string fileName, string contentType, System.Action<string> onSuccess)
        {
            var boundary = "----UnityFormBoundary" + System.Guid.NewGuid().ToString("N");
            var body = new System.Collections.Generic.List<byte>();

            void Append(string str) => body.AddRange(Encoding.UTF8.GetBytes(str));
            void AppendBytes(byte[] bytes) => body.AddRange(bytes);

            Append($"--{boundary}\r\n");
            Append($"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n");
            Append($"Content-Type: {contentType}\r\n\r\n");
            AppendBytes(fileData);
            Append($"\r\n");

            Append($"--{boundary}\r\n");
            Append($"Content-Disposition: form-data; name=\"network\"\r\n\r\n");
            Append("public\r\n");
            Append($"--{boundary}--\r\n");

            var request = new UnityWebRequest(PinataV3UploadUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(body.ToArray());
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", $"Bearer {PinataJwt}");
            request.SetRequestHeader("Content-Type", $"multipart/form-data; boundary={boundary}");

            yield return request.SendWebRequest();

            Debug.Log("Status Code: " + request.responseCode);
            Debug.Log("Response: " + request.downloadHandler.text);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Upload failed (v3): {request.error}");
                yield break;
            }

            var cid = ExtractPinataCid(request.downloadHandler.text);
            var ipfsUrl = $"ipfs://{cid}";
            Debug.Log($"✅ Uploaded to IPFS (v3): {ipfsUrl}");

            onSuccess?.Invoke(ipfsUrl);
        }

        private string ExtractPinataCid(string json)
        {
            var result = JsonUtility.FromJson<PinataResponse>(json);
            return result.data.cid;
        }

        [System.Serializable]
        private class PinataResponseData
        {
            public string cid;
        }
        
        [System.Serializable]
        private class PinataResponse
        {
            public PinataResponseData data;
        }

        [System.Serializable]
        private class SolanaMetadata
        {
            public string name;
            public string symbol;
            public string description;
            public string image;
            public MetadataProperties properties;
        }

        [System.Serializable]
        private class MetadataProperties
        {
            public string category;
            public Creator[] creators;
        }

        [System.Serializable]
        private class Creator
        {
            public string address;
            public int share;
        }
    }
}