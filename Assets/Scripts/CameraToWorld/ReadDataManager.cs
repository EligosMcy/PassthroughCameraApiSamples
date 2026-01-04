using System.IO;
using Scripts.Utility;
using UnityEngine;

namespace CameraToWorld
{
    public class ReadDataManager : MonoBehaviour
    {
        [SerializeField] private Transform _unityScene;

        [SerializeField] private GameObject _floorCubePrefab;

        [SerializeField] private GameObject _cameraCubePrefab;

        [SerializeField] private GameObject _headCubePrefab;

        [SerializeField] private GameObject _canvasCubePrefab;

        private void Start()
        {
            FileInfo[] fileinfos = null;

            WriteAndReadFile.ReadFileCreateObject(ref fileinfos);

            SnapshotDataList snapshotDataList = WriteAndReadFile.CreateObject<SnapshotDataList>(fileinfos[0]);

            setupCubeByMarkerData(_unityScene, snapshotDataList.SnapshotDatas[0].FloorMarkerData);

            spawnCube(_floorCubePrefab, snapshotDataList.SnapshotDatas[0].FloorMarkerData, _unityScene);

            int count = 0;

            foreach (SnapshotData snapshotData in snapshotDataList.SnapshotDatas)
            {
                GameObject gameObject = new GameObject($"Camera_{count}");

                gameObject.transform.SetParent(_unityScene);

                setupCubeByMarkerData(gameObject.transform, snapshotData.CameraMarkerData);

                spawnCube(_cameraCubePrefab, snapshotData.CameraMarkerData, gameObject.transform);
                spawnCube(_headCubePrefab, snapshotData.HeadMarkerData, gameObject.transform);
                spawnCube(_canvasCubePrefab, snapshotData.CanvasMarkerData, gameObject.transform);

                count++;
            }
        }

        private Transform spawnCube(GameObject prefab, MarkerData markerData, Transform parent)
        {
            Transform cube = Instantiate(prefab, parent).transform;

            setupCubeByMarkerData(cube, markerData);

            return cube;
        }

        private void setupCubeByMarkerData(Transform cube, MarkerData markerData)
        {
            cube.position = markerData.Position;
            cube.rotation = markerData.Rotation;
        }
    }
}