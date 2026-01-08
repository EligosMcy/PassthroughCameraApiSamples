using System;
using System.IO;
using Scripts.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CameraToWorld
{
    public class WriteDataManager : MonoBehaviour
    {
        [Space]
        [SerializeField] private CameraToWorldGetImageManager _cameraToWorldManager;

        [SerializeField]
        private SnapshotDataList _snapshots = new SnapshotDataList();

        public bool ShowStartButtonTooltip = true;

        [SerializeField] private GameObject m_tooltip;

        [SerializeField] private TextMesh _textMesh;

        private string _createTimeStr;
        private string _dirPath;
        private string _fileName;

        private const float FORWARDTOOLTIPOFFSET = -0.05f;
        private const float UPWARDTOOLTIPOFFSET = -0.003f;

        private void Start()
        {
            _cameraToWorldManager.OnSnapshotTakenDataStarted += _cameraToWorldManager_OnSnapshotTakenDataStarted;

            _cameraToWorldManager.OnSnapshotTakenDataAdded += _cameraToWorldManager_OnSnapshotTakenDataAdded;

            _cameraToWorldManager.OnSnapshotTakenDataCompleted += _cameraToWorldManager_OnSnapshotTakenDataCompleted;
        }

        private void _cameraToWorldManager_OnSnapshotTakenDataCompleted()
        {
            writeData();
        }

        private void _cameraToWorldManager_OnSnapshotTakenDataStarted()
        {
            _createTimeStr = DateTime.Now.ToString("HH_mm_ss");

            _textMesh.text = $"Start Snapshot / {_createTimeStr}";

            _dirPath = $"/{_createTimeStr}_SavedImages/";

            _fileName = $"{_createTimeStr}_SnapshotFile";

            _snapshots.SnapshotDatas.Clear();
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


        private void _cameraToWorldManager_OnSnapshotTakenDataAdded(SnapshotData snapshotData, int maxCount)
        {
            int count = _snapshots.SnapshotDatas.Count;

            int digits = maxCount.ToString().Length;

            // Define the save path

#if UNITY_EDITOR
            string dirPath = Application.dataPath + _dirPath;
#else
            string dirPath = Application.persistentDataPath + _dirPath;
#endif

            // Ensure the directory exists
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            // Write the bytes to a file
            File.WriteAllBytes(dirPath + "SavedImage" + count.ToString("D" + digits) + ".png", snapshotData.SnapshotTexture);

            Debug.Log("Image saved to: " + dirPath);

            _textMesh.text = "Count: / " + count;

            snapshotData.SnapshotTexture = null;

            _snapshots.SnapshotDatas.Add(snapshotData);
        }

        private void writeData()
        {
            string fileString = JsonUtility.ToJson(_snapshots);

#if UNITY_EDITOR
            string filepath = Application.dataPath + "/" + _fileName + ".json";
#else
            string filepath = Application.persistentDataPath + "/" + _fileName + ".json";
#endif
            TextWriter textWriter = new StreamWriter(filepath, false);

            textWriter.WriteLine(fileString);

            textWriter.Close();

            _textMesh.text = "Write File Count: " + _snapshots.SnapshotDatas.Count + " / " + fileString.Length;
        }
    }
}