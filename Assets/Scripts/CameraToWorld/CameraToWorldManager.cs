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
    public class CameraToWorldManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess _cameraAccess;

        [SerializeField] private GameObject _centerEyeAnchor;

        [SerializeField] private GameObject _headMarker;

        [SerializeField] private GameObject _cameraMarker;

        [SerializeField]
        private GameObject _rayGo1, _rayGo2, _rayGo3, _rayGo4;

        [Space(20)]

        [SerializeField] private CameraToWorldCameraCanvas _cameraCanvas;

        [SerializeField] private float _canvasDistance = 1f;

        [SerializeField] private Vector3 _headSpaceDebugShift = new(0, -.15f, .4f);

        [SerializeField]
        private bool _isDebugOn;

        [Space(20)]
        [SerializeField]
        private InputActionProperty _changeDistanceInputActionProperty;

        [SerializeField]
        private float _changeMinFloat = 0.5f;

        [SerializeField]
        private float _changeMaxFloat = 2;

        [SerializeField]
        private float _changeDeltaFloat = 1;

        [Space(20)]
        [SerializeField]
        private InputActionProperty _changeOffsetInputActionProperty;

        [SerializeField]
        private InputActionProperty _resetOffsetInputActionProperty;

        [SerializeField]
        private Vector3 _canvasOffset = Vector3.zero;

        [SerializeField]
        private float _offsetMinFloat = 0.5f;

        [SerializeField]
        private float _offsetMaxFloat = 2;

        [SerializeField]
        private float _offsetDeltaFloat = 1;

        [Space(20)]

        public bool ShowStartButtonTooltip = true;

        [SerializeField] private GameObject m_tooltip;

        [SerializeField] private TextMesh _textMesh;

        private const float FORWARDTOOLTIPOFFSET = -0.05f;
        private const float UPWARDTOOLTIPOFFSET = -0.003f;

        //
        private bool m_snapshotTaken;

        private MRUKAnchor _floorAnchor;

        private OVRPose m_snapshotHeadPose;

        private void OnEnable() => OVRManager.display.RecenteredPose += recenterCallBack;

        private void OnDisable() => OVRManager.display.RecenteredPose -= recenterCallBack;

        private void Start()
        {
            _changeDistanceInputActionProperty.action.Enable();

            _changeOffsetInputActionProperty.action.Enable();

            _resetOffsetInputActionProperty.action.Enable();

            _resetOffsetInputActionProperty.action.performed += resetOffset;

            StartCoroutine(nameof(cameraInitCoroutine));

        }

        private void resetOffset(InputAction.CallbackContext obj)
        {
            _canvasOffset = Vector3.zero;
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
            updateSnapshotTaken();

            changeCanvasOffset();

            updateToolTip();
        }

        private void updateSnapshotTaken()
        {
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                m_snapshotTaken = !m_snapshotTaken;
                if (m_snapshotTaken)
                {
                    // Asking the canvas to make a snapshot before stopping the camera access
                    _cameraCanvas.MakeCameraSnapshot();
                    m_snapshotHeadPose = _centerEyeAnchor.transform.ToOVRPose();
                    updateMarkerPoses();
                    _cameraAccess.enabled = false;
                }
                else
                {
                    _cameraAccess.enabled = true;
                    _cameraCanvas.StartResumeStreamingFromCamera();
                }

                updateRaysRendering();
            }

            if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                _isDebugOn = !_isDebugOn;
                Debug.Log($"PCA: SpatialSnapshotManager: DEBUG mode is {(_isDebugOn ? "ON" : "OFF")}");
                updateRaysRendering();
            }

            if (!m_snapshotTaken)
            {
                updateMarkerPoses();
            }
        }

        private void changeCanvasOffset()
        {
            var vector2 = _changeDistanceInputActionProperty.action.ReadValue<Vector2>();

            var y = vector2.y;

            _canvasDistance += y * _changeDeltaFloat * Time.deltaTime;

            _canvasDistance = Mathf.Clamp(_canvasDistance, _changeMinFloat, _changeMaxFloat);

            var vector2Offset = _changeOffsetInputActionProperty.action.ReadValue<Vector2>();
            var offsetY = vector2Offset.y;
            var offsetX = vector2Offset.x;

            _canvasOffset.x += offsetX * _offsetDeltaFloat * Time.deltaTime;

            _canvasOffset.x = Mathf.Clamp(_canvasOffset.x, _offsetMinFloat, _offsetMaxFloat);


            //
            _canvasOffset.y += offsetY * _offsetDeltaFloat * Time.deltaTime;

            _canvasOffset.y = Mathf.Clamp(_canvasOffset.y, _offsetMinFloat, _offsetMaxFloat);

            _textMesh.text = y + " / " + _canvasDistance + " / " + _canvasOffset + exportIntrinsicByPassthroughCameraAccess() + " / " + exportExtrinsicByPassthroughCameraAccess();
        }

        private void updateToolTip()
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
                    debugSegment.SetActive(m_snapshotTaken || _isDebugOn);
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
            _cameraCanvas.transform.position = cameraPose.position + cameraPose.rotation * (Vector3.forward * _canvasDistance + _canvasOffset);
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
            _headMarker.SetActive(_isDebugOn || m_snapshotTaken);
            _cameraMarker.SetActive(_isDebugOn || m_snapshotTaken);
            var gameObjects = new[]
            {
                _headMarker, _cameraMarker, _cameraCanvas.gameObject, _rayGo1, _rayGo2, _rayGo3, _rayGo4
            };

            var direction = m_snapshotTaken ? m_snapshotHeadPose.orientation : _centerEyeAnchor.transform.rotation;

            foreach (var go in gameObjects)
            {
                go.transform.position += direction * _headSpaceDebugShift * (_isDebugOn ? 1 : 0);
            }
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


        private Matrix4x4 exportExtrinsicByPassthroughCameraAccess()
        {
            // ===== 2) Pose -> 4x4 W2C =====
            // GetCameraPose() is camera pose in world space at pca.Timestamp
            Pose camPoseWorld = _cameraAccess.GetCameraPose();

            // Unity TRS gives Camera-to-World (C2W)
            Matrix4x4 C2W = Matrix4x4.TRS(camPoseWorld.position, camPoseWorld.rotation, Vector3.one);

            // Invert to get World-to-Camera (W2C)
            Matrix4x4 W2C = C2W.inverse;

            // Optional coordinate conversion: flip Z axis (common when exporting to right-handed CV pipelines)
            Matrix4x4 flipZ = Matrix4x4.identity;
            flipZ[2, 2] = -1f;
            Matrix4x4 w2Ce = flipZ * W2C;

            return w2Ce;
        }

        private Matrix4x4 exportIntrinsicByPassthroughCameraAccess()
        {
            var cameraAccessIntrinsics = _cameraAccess.Intrinsics;

            float fx = cameraAccessIntrinsics.FocalLength.x;
            float fy = cameraAccessIntrinsics.FocalLength.y;
            float cx = cameraAccessIntrinsics.PrincipalPoint.x;
            float cy = cameraAccessIntrinsics.PrincipalPoint.y;

            Matrix4x4 intrinsicMatrix4X4 = Matrix4x4.zero;

            intrinsicMatrix4X4[0, 0] = fx; // fx
            intrinsicMatrix4X4[1, 1] = fy; // fy
            intrinsicMatrix4X4[0, 2] = cx; // cx
            intrinsicMatrix4X4[1, 2] = cy; // cy
            intrinsicMatrix4X4[2, 2] = 1;  // 缩放因子

            return intrinsicMatrix4X4;
        }

    }
}