using System.Collections;
using Meta.XR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CameraView : MonoBehaviour
{
    [SerializeField] private PassthroughCameraAccess _passthroughCameraAccess;

    [SerializeField] private TextMeshProUGUI _debugText;

    [SerializeField] private RawImage _rawImage;

    private void Start()
    {
        startUpdateCameraView();
    }

    private void startUpdateCameraView()
    {
        StartCoroutine(nameof(updateCameraView));
    }

    private IEnumerator updateCameraView()
    {
        var supportedResolutions =
            PassthroughCameraAccess.GetSupportedResolutions(PassthroughCameraAccess.CameraPositionType.Left);

        Debug.Log($"PassthroughCameraAccess.GetSupportedResolutions(): {string.Join(",", supportedResolutions)}");

        var texture = _passthroughCameraAccess.GetTexture();

        _rawImage.texture = texture;

        yield return 0;
    }

    private void Update()
    {
        updateDebugText();
    }

    private void updateDebugText()
    {
        bool permission = OVRPermissionsRequester.IsPermissionGranted(
            OVRPermissionsRequester.Permission.PassthroughCameraAccess);

        _debugText.text = permission ? "Permission granted." : "No permission granted.";
    }

}
