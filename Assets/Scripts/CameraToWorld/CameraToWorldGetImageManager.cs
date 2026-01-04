using System;
using System.Collections;
using Meta.XR;
using Meta.XR.MRUtilityKit;
using PassthroughCameraSamples.CameraToWorld;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CameraToWorld
{
    public class CameraToWorldGetImageManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess _cameraAccess;

        [SerializeField] private GameObject _centerEyeAnchor;

        [SerializeField] private GameObject _headMarker;

        [SerializeField] private GameObject _cameraMarker;

        [SerializeField] private GameObject _floorMarker;

        [SerializeField] private Camera _mainCamera;

        [SerializeField]
        private GameObject _rayGo1, _rayGo2, _rayGo3, _rayGo4;

        [Space(20)]

        [SerializeField] private CameraToWorldCameraCanvas _cameraCanvas;

        [SerializeField] private float _canvasDistance = 1f;


        [Space(20)]
        [SerializeField]
        private MRUK _mruk;

        [SerializeField]
        private bool _getFloorAnchor;



        [Space(20)]
        [SerializeField]
        private InputActionProperty _startSnapshotInputActionProperty;

        [SerializeField]
        private InputActionProperty _addSnapshotMaxCountInputActionProperty;

        [SerializeField]
        private InputActionProperty _subSnapshotMaxCountInputActionProperty;

        [SerializeField]
        private InputActionProperty _resetSnapshotMaxCountInputActionProperty;

        [SerializeField]
        private InputActionProperty _changeSnapshotWaitTimeInputActionProperty;

        private readonly int _maxImageCount = 40;

        private readonly int _minImageCount = 10;

        private readonly int _defaultImageCount = 30;

        [SerializeField]
        [Range(10, 40)]
        private int _imageCount = 10;

        [SerializeField]
        private TextMesh _countTextMesh;


        [SerializeField]
        [Range(0, 10f)]
        private float _snapshotTime = 0.2f;

        [SerializeField]
        [Range(0, 10f)]
        private float _snapshotWaitTime = 0.2f;

        [SerializeField]
        [Range(0, 1)]
        private float _snapshotChangeTime = 0.1f;

        [Range(0, 2)]
        [SerializeField]
        private float _snapshotMaxTime = 1;

        [Range(0, 0.5f)]
        [SerializeField]
        private float _snapshotMinTime = 0.1f;

        [SerializeField]
        private Image _snapshotWaitImage;

        //
        private bool m_snapshotTaken;

        private MRUKAnchor _floorAnchor;

        private OVRPose m_snapshotHeadPose;

        public delegate void SnapshotTakenDataAddEvent(SnapshotData snapshotData);

        public event SnapshotTakenDataAddEvent OnSnapshotTakenDataAdded;

        public delegate void SnapshotTakenDataStartEvent();

        public event SnapshotTakenDataStartEvent OnSnapshotTakenDataStarted;

        public delegate void SnapshotTakenDataCompletedEvent();

        public event SnapshotTakenDataCompletedEvent OnSnapshotTakenDataCompleted;

        private void OnEnable() => OVRManager.display.RecenteredPose += recenterCallBack;

        private void OnDisable() => OVRManager.display.RecenteredPose -= recenterCallBack;

        private void Start()
        {
            _startSnapshotInputActionProperty.action.Enable();

            _startSnapshotInputActionProperty.action.performed += startSnapshot;


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

            //
            _mruk.SceneLoadedEvent.AddListener(findFloorAnchor);

            StartCoroutine(nameof(cameraInitCoroutine));

            _imageCount = _defaultImageCount;
            updateCountText();
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

            updateCountText();
        }

        private void addSnapshotMaxCount(InputAction.CallbackContext obj)
        {
            _imageCount++;

            _imageCount = Mathf.Clamp(_imageCount, _minImageCount, _maxImageCount);

            updateCountText();
        }

        private void subSnapshotMaxCount(InputAction.CallbackContext obj)
        {
            _imageCount--;

            _imageCount = Mathf.Clamp(_imageCount, _minImageCount, _maxImageCount);

            updateCountText();
        }

        private void resetMaxCount(InputAction.CallbackContext obj)
        {
            _imageCount = _defaultImageCount;
            updateCountText();
        }

        private void updateCountText()
        {
            _countTextMesh.text = $"Count: {_imageCount} / WaitTime: {_snapshotWaitTime}";
        }

        private void startSnapshot(InputAction.CallbackContext obj)
        {
            StartCoroutine(nameof(snapshotTakenGetImage));
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
            }
        }

        private IEnumerator cameraInitCoroutine()
        {
            if (_cameraAccess == null)
            {
                Debug.LogError($"PCA: {nameof(_cameraAccess)} field is required "
                               + $"for the component {nameof(CameraToWorldManager)} to operate properly");
                enabled = false;
                yield break;
            }

            Assert.IsTrue(_cameraAccess.enabled, "_cameraAccess.enabled");
            while (!_cameraAccess.IsPlaying)
            {
                yield return null;
            }

            scaleCameraCanvas();
            updateRaysRendering();
        }

        private void Update()
        {
            if (!m_snapshotTaken)
            {
                updateMarkerPoses();
            }
        }

        private void snapshotTaken()
        {
            m_snapshotTaken = !m_snapshotTaken;
            if (m_snapshotTaken)
            {
                // Asking the canvas to make a snapshot before stopping the camera access
                _cameraCanvas.MakeCameraSnapshot();
                m_snapshotHeadPose = _centerEyeAnchor.transform.ToOVRPose();
                updateMarkerPoses();
                _cameraAccess.enabled = false;

                var textureBytes = _cameraCanvas.CameraSnapshot.EncodeToPNG();

                var snapshotData = new SnapshotData
                {
                    SnapshotTexture = textureBytes,
                    FloorMarkerData = new MarkerData(_floorMarker.transform),
                    CameraMarkerData = new MarkerData(_cameraMarker.transform),
                    CanvasMarkerData = new MarkerData(_cameraCanvas.transform),
                    HeadMarkerData = new MarkerData(_headMarker.transform),
                    Extrinsic = exportExtrinsic(),
                    Intrinsic = exportIntrinsic(),
                };

                //
                OnSnapshotTakenDataAdded?.Invoke(snapshotData);
            }
            else
            {
                _cameraAccess.enabled = true;
                _cameraCanvas.StartResumeStreamingFromCamera();
            }

            updateRaysRendering();
        }

        private IEnumerator snapshotTakenGetImage()
        {
            OnSnapshotTakenDataStarted?.Invoke();

            for (int i = 0; i < _imageCount; i++)
            {
                snapshotTaken();
                yield return new WaitForSeconds(_snapshotTime);
                _snapshotWaitImage.fillAmount = 1;
                snapshotTaken();
                yield return StartCoroutine(snapshotTakenWait());
            }

            OnSnapshotTakenDataCompleted?.Invoke();
        }

        private IEnumerator snapshotTakenWait()
        {
            float time = _snapshotWaitTime;

            while (time > 0)
            {
                time -= Time.deltaTime;
                _snapshotWaitImage.fillAmount = time / _snapshotWaitTime;
                yield return 0;
            }

            _snapshotWaitImage.fillAmount = 0;
            yield return 0;
        }

        /// <summary>
        /// Calculate the dimensions of the canvas based on the distance from the camera origin and the camera resolution
        /// </summary>
        private void scaleCameraCanvas()
        {
            var cameraCanvasRectTransform = _cameraCanvas.GetComponentInChildren<RectTransform>();
            var leftSidePointInCamera = _cameraAccess.ViewportPointToRay(new Vector2(0f, 0.5f));
            var rightSidePointInCamera = _cameraAccess.ViewportPointToRay(new Vector2(1f, 0.5f));
            var horizontalFoVDegrees = Vector3.Angle(leftSidePointInCamera.direction, rightSidePointInCamera.direction);
            var horizontalFoVRadians = horizontalFoVDegrees / 180 * Math.PI;
            var newCanvasWidthInMeters = 2 * _canvasDistance * Math.Tan(horizontalFoVRadians / 2);
            var localScale = (float)(newCanvasWidthInMeters / cameraCanvasRectTransform.sizeDelta.x);
            cameraCanvasRectTransform.localScale = new Vector3(localScale, localScale, localScale);
        }

        private void updateRaysRendering()
        {
            // Hide rays' middle segments and rendering only their tips
            // when rays' origins are too close to the headset. Otherwise, it looks ugly
            foreach (var rayGo in new[] { _rayGo1, _rayGo2, _rayGo3, _rayGo4 })
            {
                var rayRenderer = rayGo.GetComponent<CameraToWorldRayRenderer>();
                foreach (var debugSegment in rayRenderer.m_debugSegments)
                {
                    debugSegment.SetActive(m_snapshotTaken);
                }
            }
        }

        private void updateMarkerPoses()
        {
            if (!_cameraAccess.IsPlaying)
            {
                return;
            }
            var headPose = OVRPlugin.GetNodePoseStateImmediate(OVRPlugin.Node.Head).Pose.ToOVRPose();
            _headMarker.transform.position = headPose.position;
            _headMarker.transform.rotation = headPose.orientation;

            var cameraPose = _cameraAccess.GetCameraPose();
            _cameraMarker.transform.position = cameraPose.position;
            _cameraMarker.transform.rotation = cameraPose.rotation;

            // Position the canvas in front of the camera
            _cameraCanvas.transform.position = cameraPose.position + cameraPose.rotation * (Vector3.forward * _canvasDistance);
            _cameraCanvas.transform.rotation = cameraPose.rotation;

            // Position the rays pointing to 4 corners of the canvas / image
            var rays = new[]
            {
                new { rayGo = _rayGo1, u = 0f, v = 0f },
                new { rayGo = _rayGo2, u = 0f, v = 1f },
                new { rayGo = _rayGo3, u = 1f, v = 1f },
                new { rayGo = _rayGo4, u = 1f, v = 0f }
            };

            foreach (var item in rays)
            {
                var rayInWorld = _cameraAccess.ViewportPointToRay(new Vector2(item.u, item.v));
                item.rayGo.transform.position = rayInWorld.origin;
                item.rayGo.transform.LookAt(rayInWorld.origin + rayInWorld.direction);

                var angleWithCameraForwardDegree =
                    Vector3.Angle(item.rayGo.transform.forward, cameraPose.rotation * Vector3.forward);
                // The original size of the ray GameObject along z axis is 0.5f. Hardcoding it here for simplicity
                var zScale = (float)(_canvasDistance / Math.Cos(angleWithCameraForwardDegree / 180 * Math.PI) / 0.5);
                item.rayGo.transform.localScale = new Vector3(item.rayGo.transform.localScale.x, item.rayGo.transform.localScale.y, zScale);

                var label = item.rayGo.GetComponentInChildren<Text>();
                label.text = $"({item.u:F0}, {item.v:F0})";
            }

            // Move the updated markers forward to better see them
            _headMarker.SetActive(m_snapshotTaken);
            _cameraMarker.SetActive(m_snapshotTaken);
        }

        private void recenterCallBack()
        {
            if (m_snapshotTaken)
            {
                m_snapshotTaken = false;
                _cameraAccess.enabled = true;
                _cameraCanvas.StartResumeStreamingFromCamera();
                updateRaysRendering();
            }
        }

        private Extrinsic exportExtrinsic()
        {
            Matrix4x4 worldToCamera = _mainCamera.worldToCameraMatrix;

            Matrix4x4 worldToLocalCamera = _mainCamera.transform.worldToLocalMatrix;

            //
            Matrix4x4 cvAdjust = Matrix4x4.identity;

            cvAdjust[1, 1] = -1;
            cvAdjust[2, 2] = -1;

            Matrix4x4 extrinsicMatrix4X4 = cvAdjust * worldToCamera;

            Extrinsic exportExtrinsic = new Extrinsic
            {
                ExtrinsicMatrix = extrinsicMatrix4X4,
                WorldToMatrix = worldToCamera,
                WorldToLocalMatrix = worldToLocalCamera,
            };

            return exportExtrinsic;
        }

        private Intrinsic exportIntrinsic()
        {
            int width = _mainCamera.pixelWidth;
            int height = _mainCamera.pixelHeight;
            float fovVertical = _mainCamera.fieldOfView * Mathf.Deg2Rad; // 转为弧度
            float aspect = (float)width / height;

            float fy = height / (2 * Mathf.Tan(fovVertical / 2));
            float fx = fy * aspect;
            float cx = width / 2.0f;
            float cy = height / 2.0f;

            Matrix4x4 intrinsicMatrix4X4 = Matrix4x4.zero;
            intrinsicMatrix4X4[0, 0] = fx; // fx
            intrinsicMatrix4X4[1, 1] = fy; // fy
            intrinsicMatrix4X4[0, 2] = cx; // cx
            intrinsicMatrix4X4[1, 2] = cy; // cy
            intrinsicMatrix4X4[2, 2] = 1;  // 缩放因子

            Intrinsic intrinsic = new Intrinsic { IntrinsicsMatrix = intrinsicMatrix4X4 };

            return intrinsic;
        }
    }
}