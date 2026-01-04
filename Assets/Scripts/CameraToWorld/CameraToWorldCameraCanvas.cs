using System.Collections;
using Meta.XR;
using UnityEngine;
using UnityEngine.UI;

namespace CameraToWorld
{
    public class CameraToWorldCameraCanvas : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess _cameraAccess;
        [SerializeField] private Text _debugText;
        [SerializeField] private RawImage _image;

        private Texture2D _cameraSnapshot;

        public Texture2D CameraSnapshot => _cameraSnapshot;


        private void Start()
        {
            StartCoroutine(startCameraCanvas());
        }

        public void MakeCameraSnapshot()
        {
            if (!_cameraAccess.IsPlaying)
            {
                Debug.LogError("!_cameraAccess.IsPlaying");
                return;
            }

            if (_cameraSnapshot == null)
            {
                var size = _cameraAccess.CurrentResolution;
                _cameraSnapshot = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
            }

            var pixels = _cameraAccess.GetColors();
            _cameraSnapshot.LoadRawTextureData(pixels);
            _cameraSnapshot.Apply();

            StopResumeStreamingFromCamera();

            _image.texture = _cameraSnapshot;
        }

        public void StopResumeStreamingFromCamera()
        {
            StopCoroutine(resumeStreamingFromCameraCor());
        }

        public void StartResumeStreamingFromCamera()
        {
            StartCoroutine(resumeStreamingFromCameraCor());
        }

        private IEnumerator resumeStreamingFromCameraCor()
        {
            while (!_cameraAccess.IsPlaying)
            {
                yield return null;
            }

            _image.texture = _cameraAccess.GetTexture();
        }



        private IEnumerator startCameraCanvas()
        {
            _debugText.text = "No permission granted.";
            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                yield return null;
            }
            _debugText.text = "Permission granted.";

            while (!_cameraAccess.IsPlaying)
            {
                yield return null;
            }

            StartResumeStreamingFromCamera();
        }

    }
}