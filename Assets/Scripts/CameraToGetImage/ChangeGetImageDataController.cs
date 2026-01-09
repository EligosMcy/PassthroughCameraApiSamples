using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CameraToGetImage
{
    public class ChangeGetImageDataController : MonoBehaviour
    {
        [SerializeField] private CameraGetImageManager _cameraGetImageManager;

        [SerializeField] private InputActionProperty _addSnapshotMaxCountInputActionProperty;

        [SerializeField] private InputActionProperty _subSnapshotMaxCountInputActionProperty;

        [SerializeField] private InputActionProperty _resetSnapshotMaxCountInputActionProperty;

        [SerializeField] private InputActionProperty _changeSnapshotWaitTimeInputActionProperty;

        [Space]
        [Range(10, 100)]
        [SerializeField]
        private int _maxImageCount = 40;

        [Range(1, 20)][SerializeField] private int _minImageCount = 10;

        [Range(10, 100)][SerializeField] private int _defaultImageCount = 30;

        [SerializeField][Range(10, 40)] private int _imageCount = 10;

        [Space]
        [SerializeField]
        [Range(0, 10f)]
        private float _snapshotWaitTime = 0.2f;

        [SerializeField][Range(0, 2)] private float _snapshotChangeTime = 0.1f;

        [Range(0, 10)][SerializeField] private float _snapshotMaxTime = 1;

        [Range(0, 1f)][SerializeField] private float _snapshotMinTime = 0.1f;


        private void Start()
        {
            //
            _resetSnapshotMaxCountInputActionProperty.action.Enable();

            _resetSnapshotMaxCountInputActionProperty.action.performed += resetMaxCount;

            //
            _addSnapshotMaxCountInputActionProperty.action.Enable();

            _addSnapshotMaxCountInputActionProperty.action.performed += addSnapshotMaxCount;

            _subSnapshotMaxCountInputActionProperty.action.Enable();

            _subSnapshotMaxCountInputActionProperty.action.performed += subSnapshotMaxCount;

            _changeSnapshotWaitTimeInputActionProperty.action.Enable();
            _changeSnapshotWaitTimeInputActionProperty.action.performed += changeSnapshotWaitTime;

            _imageCount = _defaultImageCount;
            _cameraGetImageManager.SetImageCount(_imageCount);
        }

        private void changeSnapshotWaitTime(InputAction.CallbackContext obj)
        {
            Vector2 changeVector2 = obj.ReadValue<Vector2>();

            float changeY = changeVector2.y;

            if (changeY > 0.7)
            {
                _snapshotWaitTime += _snapshotChangeTime * Time.deltaTime;
            }
            else if (changeY < -0.7f)
            {
                _snapshotWaitTime -= _snapshotChangeTime * Time.deltaTime;

            }

            _snapshotWaitTime = Mathf.Clamp(_snapshotWaitTime, _snapshotMinTime, _snapshotMaxTime);

            _cameraGetImageManager.SetSnapshotWaitTime(_snapshotWaitTime);
        }

        private void addSnapshotMaxCount(InputAction.CallbackContext obj)
        {
            _imageCount++;

            _imageCount = Mathf.Clamp(_imageCount, _minImageCount, _maxImageCount);

            _cameraGetImageManager.SetImageCount(_imageCount);
        }

        private void subSnapshotMaxCount(InputAction.CallbackContext obj)
        {
            _imageCount--;

            _imageCount = Mathf.Clamp(_imageCount, _minImageCount, _maxImageCount);

            _cameraGetImageManager.SetImageCount(_imageCount);
        }

        private void resetMaxCount(InputAction.CallbackContext obj)
        {
            _imageCount = _defaultImageCount;
            _cameraGetImageManager.SetImageCount(_imageCount);
        }
    }
}