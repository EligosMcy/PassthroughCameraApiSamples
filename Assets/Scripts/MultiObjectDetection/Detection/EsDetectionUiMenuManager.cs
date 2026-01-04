using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static MultiObjectDetection.EsDetectionUiMenuManager;

namespace MultiObjectDetection
{
    public class EsDetectionUiMenuManager : MonoBehaviour
    {
        [Header("Ui elements ref.")]
        [SerializeField]
        private GameObject m_loadingPanel;

        [SerializeField] private GameObject m_initialPanel;
        [SerializeField] private GameObject m_noPermissionPanel;
        [SerializeField] private Text m_labelInformation;
        [SerializeField] private AudioSource m_buttonSound;

        public bool IsInputActive { get; set; } = false;

        public UnityEvent<bool> OnPause;

        private bool m_initialMenu;

        // start menu
        private int m_objectsDetected = 0;
        private int m_objectsIdentified = 0;

        // pause menu
        public bool IsPaused { get; private set; } = true;

        public InputActionProperty _inputActionProperty;


        public delegate void OnInitialed();

        public event OnInitialed onInitialed;

        public delegate void OnNoPermission();

        public event OnNoPermission onNoPermission;

        public delegate void OnPauseChanged();

        public event OnPauseChanged onPausechanged;

        public delegate void OnWaitChanged();

        public event OnWaitChanged onWaitChanged;

        #region Unity Functions

        private IEnumerator Start()
        {
            _inputActionProperty.action.Enable();

            _inputActionProperty.action.performed += inputActionTest;

            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(false);
            m_loadingPanel.SetActive(false);

            // Wait for permissions
            OnNoPermissionMenu();
            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.Scene) ||
                   !OVRPermissionsRequester.IsPermissionGranted(
                       OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                onWaitChanged?.Invoke();
                yield return null;
            }

            OnInitialMenu();
        }

        private void inputActionTest(InputAction.CallbackContext obj)
        {
            if (m_initialMenu)
            {
                m_buttonSound?.Play();
                OnPauseMenu(false);
            }
        }

        #endregion

        #region Ui state: No permissions Menu

        private void OnNoPermissionMenu()
        {
            m_initialMenu = false;
            IsPaused = true;
            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(true);

            onNoPermission?.Invoke();
        }

        #endregion

        #region Ui state: Initial Menu

        private void OnInitialMenu()
        {
            m_initialMenu = true;
            IsPaused = true;
            m_initialPanel.SetActive(true);
            m_noPermissionPanel.SetActive(false);

            onInitialed?.Invoke();
        }


        private void OnPauseMenu(bool visible)
        {
            m_initialMenu = false;
            IsPaused = visible;

            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(false);

            OnPause?.Invoke(visible);
            onPausechanged?.Invoke();
        }

        #endregion

        #region Ui state: detection information

        private void UpdateLabelInformation()
        {
            m_labelInformation.text =
                $"Unity Sentis version: 2.1.3\nAI model: Yolo\nDetecting objects: {m_objectsDetected}\nObjects identified: {m_objectsIdentified}";
        }

        public void OnObjectsDetected(int objects)
        {
            m_objectsDetected = objects;
            UpdateLabelInformation();
        }

        public void OnObjectsIndentified(int objects)
        {
            if (objects < 0)
            {
                // reset the counter
                m_objectsIdentified = 0;
            }
            else
            {
                m_objectsIdentified += objects;
            }

            UpdateLabelInformation();
        }

        #endregion
    }
}
