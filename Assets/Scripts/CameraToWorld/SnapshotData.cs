using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CameraToWorld
{
    [Serializable]
    public class SnapshotDataList
    {
        public List<SnapshotData> SnapshotDatas = new List<SnapshotData>();
    }

    [Serializable]
    public class SnapshotData
    {
        public byte[] SnapshotTexture;

        public MarkerData FloorMarkerData;

        public MarkerData HeadMarkerData;

        public MarkerData CameraMarkerData;

        public MarkerData CanvasMarkerData;

        public Extrinsic Extrinsic;

        public Intrinsic Intrinsic;
        public override string ToString() => $"{nameof(SnapshotTexture)}: {SnapshotTexture.Length}, {nameof(HeadMarkerData)}: {HeadMarkerData}, {nameof(CameraMarkerData)}: {CameraMarkerData}, {nameof(CanvasMarkerData)}: {CanvasMarkerData}";
    }

    [Serializable]
    public class ImageData
    {
        public string FileName;

        public float[] ExtrinsicArray = new float[16];

        public float[] IntrinsicArray = new float[9];
    }

    [Serializable]
    public class MarkerData
    {
        public Vector3 Position;

        public Quaternion Rotation;

        public MarkerData(Transform marker)
        {
            Position = marker.position;
            Rotation = marker.rotation;
        }

        public MarkerData(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public override string ToString() => $"{nameof(Position)}: {Position}, {nameof(Rotation)}: {Rotation}";
    }

    [Serializable]
    public class Extrinsic
    {
        public Matrix4x4 ExtrinsicMatrix;

        public Matrix4x4 WorldToMatrix;

        public Matrix4x4 WorldToLocalMatrix;
    }

    [Serializable]
    public class Intrinsic
    {
        public Matrix4x4 IntrinsicsMatrix;
    }
}