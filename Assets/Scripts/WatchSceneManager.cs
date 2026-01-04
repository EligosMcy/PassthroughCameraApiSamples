using System;
using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Scripts.Utility
{
    public class WatchSceneManager : MonoBehaviour
    {
        [SerializeField] private Transform _watchScene;

        [SerializeField] private Transform _floorMarker;

        [Space(20)]
        [SerializeField]
        private MRUK _mruk;

        [SerializeField]
        private bool _getFloorAnchor;

        private MRUKAnchor _floorAnchor;

        [SerializeField]
        private InputActionProperty _nextInputActionProperty;

        private int _currentChildIndex = 0;

        private int _childCount = 0;

        [Space(20)]
        public bool ShowStartButtonTooltip = true;

        [SerializeField] private GameObject m_tooltip;

        [SerializeField] private TextMesh _textMesh;

        private const float FORWARDTOOLTIPOFFSET = -0.05f;
        private const float UPWARDTOOLTIPOFFSET = -0.003f;

        private void Start()
        {
            _nextInputActionProperty.action.Enable();

            _nextInputActionProperty.action.performed += nextInputAction;

            _mruk.SceneLoadedEvent.AddListener(findFloorAnchor);

            foreach (Transform child in _watchScene)
            {
                child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                child.gameObject.SetActive(false);
            }

            _childCount = _watchScene.childCount;

            _currentChildIndex = 0;
        }

        private void nextInputAction(InputAction.CallbackContext obj)
        {
            showChildIndex(_currentChildIndex);

            _currentChildIndex++;

            if (_currentChildIndex >= _childCount)
            {
                _currentChildIndex = 0;
            }
        }

        private void Update()
        {
            if (ShowStartButtonTooltip)
            {
                var finalRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch) *
                                    Quaternion.Euler(45, 0, 0);
                var forwardOffsetPosition = finalRotation * Vector3.forward * FORWARDTOOLTIPOFFSET;
                var upwardOffsetPosition = finalRotation * Vector3.up * UPWARDTOOLTIPOFFSET;
                var finalPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch) +
                                    forwardOffsetPosition + upwardOffsetPosition;
                m_tooltip.transform.rotation = finalRotation;
                m_tooltip.transform.position = finalPosition;
            }
        }

        private void findFloorAnchor()
        {
            MRUKRoom mrukRoom = _mruk.GetCurrentRoom();

            if (mrukRoom.FloorAnchors.Count <= 0)
            {
                _getFloorAnchor = false;

                Debug.LogError("Floor Anchors is Null");
            }
            else
            {
                Debug.Log($"Floor Anchors Count: {mrukRoom.FloorAnchors.Count}");

                _getFloorAnchor = true;
                _floorAnchor = mrukRoom.FloorAnchors[0];
                _floorMarker.transform.SetParent(_floorAnchor.transform);
                _floorMarker.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                _watchScene.transform.SetParent(_floorAnchor.transform);
                _watchScene.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }

        private void showChildIndex(int index)
        {
            for (int i = 0; i < _watchScene.childCount; i++)
            {
                Transform child = _watchScene.GetChild(i);

                child.gameObject.SetActive(i == index);

                if (i == index)
                {
                    _textMesh.text = child.name;
                }
            }
        }
    }
}