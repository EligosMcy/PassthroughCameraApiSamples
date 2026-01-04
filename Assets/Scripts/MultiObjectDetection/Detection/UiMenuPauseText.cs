using System;
using UnityEngine;

namespace MultiObjectDetection
{
    public class UiMenuPauseText : MonoBehaviour
    {
        public bool ShowStartButtonTooltip = true;

        [SerializeField] private GameObject m_tooltip;

        [SerializeField] private TextMesh _textMesh;

        [SerializeField] private EsDetectionUiMenuManager _esDetectionUiMenuManager;

        private const float FORWARDTOOLTIPOFFSET = -0.05f;
        private const float UPWARDTOOLTIPOFFSET = -0.003f;

        private void Start()
        {
            _esDetectionUiMenuManager.onInitialed += _esDetectionUiMenuManager_onInitialed;

            _esDetectionUiMenuManager.onNoPermission += _esDetectionUiMenuManager_onNoPermission;

            _esDetectionUiMenuManager.onWaitChanged += _esDetectionUiMenuManager_onWaitChanged;

            _esDetectionUiMenuManager.onPausechanged += _esDetectionUiMenuManager_onPausechanged;
        }

        private void _esDetectionUiMenuManager_onPausechanged()
        {
            _textMesh.text = "Pause Changed:";
        }

        private void _esDetectionUiMenuManager_onWaitChanged()
        {
            _textMesh.text = "Wait Changed:";
        }

        private void _esDetectionUiMenuManager_onNoPermission()
        {
            _textMesh.text = "No Permission :";
        }

        private void _esDetectionUiMenuManager_onInitialed()
        {
            _textMesh.text = "Initialed";
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
    }
}