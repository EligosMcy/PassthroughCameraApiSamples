using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace CameraToGetImage
{
    public class WriteDataManager : MonoBehaviour
    {
        [Space]
        [SerializeField] private CameraGetImageManager _cameraManager;

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
            _cameraManager.OnSnapshotTakenDataStarted += cameraManagerOnSnapshotTakenDataStarted;

            _cameraManager.OnSnapshotTakenDataAdded += cameraManagerOnSnapshotTakenDataAdded;

            _cameraManager.OnSnapshotTakenDataCompleted += cameraManagerOnSnapshotTakenDataCompleted;
        }

        private void cameraManagerOnSnapshotTakenDataCompleted(int maxCount)
        {
            writeData(maxCount);
        }

        private void cameraManagerOnSnapshotTakenDataStarted()
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

        private void cameraManagerOnSnapshotTakenDataAdded(SnapshotData snapshotData)
        {
            _snapshots.SnapshotDatas.Add(snapshotData);

            _textMesh.text = "Count: / " + _snapshots.SnapshotDatas.Count;
        }

        //
        private async void writeData(int maxCount)
        {
            List<Task> tasks = new List<Task>();

            //
            int count = 0;

            int digits = maxCount.ToString().Length;

            foreach (SnapshotData snapshotData in _snapshots.SnapshotDatas)
            {
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
                await File.WriteAllBytesAsync(dirPath + "SavedImage" + count.ToString("D" + digits) + ".png", snapshotData.SnapshotTexture);

                Debug.Log("Image saved to: " + dirPath);

                snapshotData.SnapshotTexture = null;

                count++;
            }

            //Write Json
            string fileString = JsonUtility.ToJson(_snapshots);

#if UNITY_EDITOR
            string filepath = Application.dataPath + "/" + _fileName + ".json";
#else
            string filepath = Application.persistentDataPath + "/" + _fileName + ".json";
#endif
            TextWriter textWriter = new StreamWriter(filepath, false);

            await textWriter.WriteLineAsync(fileString);

            textWriter.Close();

            _textMesh.text = "Write File Completed / Count: " + _snapshots.SnapshotDatas.Count + " / " + fileString.Length;
        }
    }
}