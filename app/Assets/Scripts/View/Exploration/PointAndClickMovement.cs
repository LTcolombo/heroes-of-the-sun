using System;
using Connectors;
using Model;
using Notifications;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils.Injection;

namespace View.Exploration
{
    public class PointAndClickMovement : InjectableBehaviour
    {
        [Inject] private HeroConnector _connector;
        [Inject] private SmartObjectModel _smartObjects;
        [Inject] private GridInteractionStateModel _gridInteraction;
        [Inject] private HideInteractionWithSmartObject _hideInteractionWithSmartObject;

        private float _mouseDownTime;
        private EventSystem _eventSystem;

        private void Start()
        {
            _eventSystem = EventSystem.current;
        }

        public void SetDataAddress(string value)
        {
            _connector.SetDataAddress(value);
        }

        private void Update()
        {
            if (_gridInteraction.State != GridInteractionState.Idle)
                return;

            if (_eventSystem.IsPointerOverGameObject())
                return;
            
            if (Input.GetMouseButtonDown(0))
                _mouseDownTime = Time.time;

            if (Input.GetMouseButtonUp(0) && Time.time - _mouseDownTime < .5f)
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                var mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (!Physics.Raycast(mouseRay, out var info, 1000f)) return;

                var tile = info.collider.GetComponent<RenderTile>();
                var tileLocation = tile.Location;
                
                if (_smartObjects.HasSmartObjectAt(tileLocation))
                    tileLocation += Vector2Int.up; //should be the tile closer to the hero position

                _hideInteractionWithSmartObject.Dispatch();
                _ = _connector.Move(tileLocation.x, tileLocation.y);
            }
        }
    }
}