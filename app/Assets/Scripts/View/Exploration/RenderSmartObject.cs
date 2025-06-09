using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Connectors;
using Model;
using Solana.Unity.Wallet;
using UnityEngine;
using Utils.Injection;
using View.Exploration.SmartObjectTypes;
using World.Program;

namespace View.Exploration
{
    [Serializable]
    public class ComponentRenderer
    {
        public string componentAddress;
        public GameObject prefab;
    }

    public class RenderSmartObject : InjectableBehaviour
    {
        [Inject] private SmartObjectLocationConnector _connector;
        [Inject] private PathfindingModel _pathfinding;
        [Inject] private SmartObjectModel _model;

        private SmartObjectLocation.Accounts.SmartObjectLocation _data;


        [Inject] private SmartObjectDeityConnector _deity;
        [Inject] private SmartObjectTokenLauncherConnector _tokenLauncher;

        [SerializeField] private ComponentRenderer[] renderers;


        public async Task SetDataAddress(string value)
        {
            _connector.SetDataAddress(value);
            var data = await _connector.LoadData();

            var componentFound = false;
            componentFound |= await TryInitSmartObject(data.Entity, _deity);
            
            //if already found this wont execute :thumbs up:
            componentFound |= await TryInitSmartObject(data.Entity, _tokenLauncher);


            //no match
            if (!componentFound)
            {
                Destroy(gameObject);
                return;
            }

            foreach (var smartObject in gameObject.GetComponentsInChildren<ISmartObject>())
                await smartObject.SetEntity(data.Entity);

            OnDataUpdate(data);
            await _connector.Subscribe(OnDataUpdate);
        }

        private async Task<bool> TryInitSmartObject<T>(PublicKey entity, BaseComponentConnector<T> connector)
        {
            await connector.SetEntityPda(entity, false);
            var smartObjectData = await connector.LoadData();
            if (smartObjectData == null)
                return false;
            var smartObjectRenderer =
                renderers.FirstOrDefault(r => r.componentAddress == connector.GetComponentProgramAddress());
            if (smartObjectRenderer == null)
                return false;
            Instantiate(smartObjectRenderer.prefab, transform);
            return true;
        }

        private void OnDataUpdate(SmartObjectLocation.Accounts.SmartObjectLocation value)
        {
            _data = value;
            _model.Set(new Vector2Int(_data.X, _data.Y), _data.Entity);

            StopAllCoroutines();
            StartCoroutine(UpdatePosition());
        }

        private IEnumerator UpdatePosition()
        {
            while (true)
            {
                var pos = ConfigModel.GetWorldCellPosition(_data.X, _data.Y);
                pos.y = _pathfinding.GetY(new Vector2Int(_data.X, _data.Y)) + ConfigModel.CellSize;

                transform.localPosition = pos;

                yield return new WaitForSeconds(1);
            }
        }
    }
}