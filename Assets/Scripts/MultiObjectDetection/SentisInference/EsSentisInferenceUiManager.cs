using System.Collections.Generic;
using Meta.XR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MultiObjectDetection
{
    public class EsSentisInferenceUiManager : MonoBehaviour
    {
        [SerializeField] private EsDetectionManager m_detectionManager;

        [Header("Placement configuration")]
        [SerializeField] private EsEnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;

        [SerializeField]
        private RectTransform m_detectionBoxPrefab;
        [SerializeField]
        private EsDetectionCanvasMaker m_canvasMaker;

        [Space(10)]
        public UnityEvent<int> OnObjectsDetected;

        internal readonly List<BoundingBoxData> m_boxDrawn = new();
        private string[] m_labels;
        private readonly List<BoundingBoxData> m_boxPool = new();



        internal class BoundingBoxData
        {
            public string ClassName;
            public int ClassId;
            public RectTransform BoxRectTransform;
            public EsDetectionCanvasMaker CanvasMaker;
            public float lastUpdateTime;
            public Vector2 Size;
        }

        private void Awake() => m_detectionBoxPrefab.gameObject.SetActive(false);

        private void Update()
        {
            // Remove boxes that haven't been updated recently
            for (int i = m_boxDrawn.Count - 1; i >= 0; i--)
            {
                var box = m_boxDrawn[i];
                const float timeToPersistBoxes = 3f;
                if (Time.time - box.lastUpdateTime > timeToPersistBoxes)
                {
                    ReturnToPool(box);
                    m_boxDrawn.RemoveAt(i);
                }
            }
        }

        public void SetLabels(TextAsset labelsAsset)
        {
            // Parse neural net labels
            m_labels = labelsAsset.text.Split('\n');
        }

        public void DrawUIBoxes(List<(int classId, Vector4 boundingBox)> detections, Vector2 inputSize, Pose cameraPose)
        {
            Vector2 currentResolution = m_cameraAccess.CurrentResolution;

            if (detections.Count == 0)
            {
                OnObjectsDetected?.Invoke(0);
                return;
            }

            OnObjectsDetected?.Invoke(detections.Count);

            // 获取相机原始纹理
            Texture2D cameraTexture = getCameraSnapshot(); // 假设m_cameraAccess有获取纹理的方法

            // Draw the bounding boxes
            for (var i = 0; i < detections.Count; i++)
            {
                var detection = detections[i];

                if (detection.classId != 66)
                {
                    continue;
                }


                float x1 = detection.boundingBox[0];
                float y1 = detection.boundingBox[1];
                float x2 = detection.boundingBox[2];
                float y2 = detection.boundingBox[3];
                Rect rect = new Rect(x1, y1, x2 - x1, y2 - y1);
                // Rect rect = Rect.MinMaxRect(x1, y1, x2, y2); // todo

                Vector2 normalizedCenter = rect.center / inputSize;
                Vector2 center = currentResolution * (normalizedCenter - Vector2.one * 0.5f);

                // Get the object class name
                var classname = m_labels[detection.classId].Replace(" ", "_");

                // Get the 3D marker world position using Depth Raycast
                var ray = m_cameraAccess.ViewportPointToRay(new Vector2(normalizedCenter.x, 1.0f - normalizedCenter.y), cameraPose);
                var worldPos = m_environmentRaycast.Raycast(ray);
                var normRect = new Rect(
                    rect.x / inputSize.x,
                    1f - rect.yMax / inputSize.y,
                    rect.width / inputSize.x,
                    rect.height / inputSize.y
                );

                // Calculate distance and center point first
                float distance = worldPos.HasValue ? Vector3.Distance(cameraPose.position, worldPos.Value) : 1f;
                var worldSpaceCenter = m_cameraAccess.ViewportPointToRay(normRect.center, cameraPose).GetPoint(distance);
                var normal = (worldSpaceCenter - cameraPose.position).normalized;

                // Intersect corner rays with the plane perpendicular to the camera view
                var plane = new Plane(normal, worldSpaceCenter);
                var minRay = m_cameraAccess.ViewportPointToRay(normRect.min, cameraPose);
                var maxRay = m_cameraAccess.ViewportPointToRay(normRect.max, cameraPose);
                plane.Raycast(minRay, out float intersectionDistanceMin);
                plane.Raycast(maxRay, out float intersectionDistanceMax);
                var min = minRay.GetPoint(intersectionDistanceMin);
                var max = maxRay.GetPoint(intersectionDistanceMax);

                // Transform world-space positions to camera's local space to get 2D size
                var topLeftLocal = Quaternion.Inverse(cameraPose.rotation) * (min - cameraPose.position);
                var bottomRightLocal = Quaternion.Inverse(cameraPose.rotation) * (max - cameraPose.position);
                var size = new Vector2(
                    Mathf.Abs(bottomRightLocal.x - topLeftLocal.x),
                    Mathf.Abs(bottomRightLocal.y - topLeftLocal.y));

                var boxData = GetOrCreateBoundingBoxData(detection.classId, worldSpaceCenter, size);

                var boxRectTransform = boxData.BoxRectTransform;
                boxRectTransform.GetComponentInChildren<Text>().text = $"Id: {detection.classId} Class: {classname} Center (px): {center:0.0} Center (%): {normalizedCenter:0.0}";
                boxRectTransform.SetPositionAndRotation(worldSpaceCenter, Quaternion.LookRotation(normal));
                boxRectTransform.sizeDelta = size;

                var canvasMaker = boxData.CanvasMaker.CanvasMakerRectTransform;
                canvasMaker.SetPositionAndRotation(worldSpaceCenter, Quaternion.LookRotation(normal));
                canvasMaker.sizeDelta = size;
                //
                // === 新增：截取识别区域的纹理 ===
                if (cameraTexture != null)
                {
                    // 计算归一化边界框（注意Y轴翻转）
                    Rect normCropRect = new Rect(
                        x: rect.x / inputSize.x,
                        y: 1.0f - (rect.y + rect.height) / inputSize.y, // Unity纹理坐标原点在左下
                        width: rect.width / inputSize.x,
                        height: rect.height / inputSize.y
                    );

                    // 转换为像素坐标
                    int texWidth = cameraTexture.width;
                    int texHeight = cameraTexture.height;
                    int cropX = (int)(normCropRect.x * texWidth);
                    int cropY = (int)(normCropRect.y * texHeight);
                    int cropWidth = (int)(normCropRect.width * texWidth);
                    int cropHeight = (int)(normCropRect.height * texHeight);

                    // 确保不越界
                    cropX = Mathf.Clamp(cropX, 0, texWidth - 1);
                    cropY = Mathf.Clamp(cropY, 0, texHeight - 1);
                    cropWidth = Mathf.Clamp(cropWidth, 1, texWidth - cropX);
                    cropHeight = Mathf.Clamp(cropHeight, 1, texHeight - cropY);

                    // 创建新纹理并复制区域
                    Texture2D croppedTex = new Texture2D(cropWidth, cropHeight);
                    Color[] pixels = cameraTexture.GetPixels(
                        cropX,
                        cropY,
                        cropWidth,
                        cropHeight
                    );
                    croppedTex.SetPixels(pixels);
                    croppedTex.Apply();

                    boxData.CanvasMaker.SetTexture2D(croppedTex);
                }
                // === 截取结束 ===

                //
                boxData.Size = size;
                boxData.lastUpdateTime = Time.time;
            }
        }


        private Texture2D getCameraSnapshot()
        {
            var size = m_cameraAccess.CurrentResolution;
            Texture2D cameraSnapshot = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);

            var pixels = m_cameraAccess.GetColors();
            cameraSnapshot.LoadRawTextureData(pixels);
            cameraSnapshot.Apply();

            return cameraSnapshot;
        }


        private BoundingBoxData GetOrCreateBoundingBoxData(int classId, Vector3 worldSpaceCenter, Vector2 worldSpaceSize)
        {
            BoundingBoxData reusedBox = null;
            for (int i = m_boxDrawn.Count - 1; i >= 0; i--)
            {
                var box = m_boxDrawn[i];
                var localPos = box.BoxRectTransform.InverseTransformPoint(worldSpaceCenter);
                var newBox = new Vector4(
                    localPos.x - worldSpaceSize.x * 0.5f,
                    localPos.y - worldSpaceSize.y * 0.5f,
                    localPos.x + worldSpaceSize.x * 0.5f,
                    localPos.y + worldSpaceSize.y * 0.5f
                );

                var sizeDelta = box.BoxRectTransform.sizeDelta;
                var currentBox = new Vector4(
                    -sizeDelta.x * 0.5f,
                    -sizeDelta.y * 0.5f,
                    sizeDelta.x * 0.5f,
                    sizeDelta.y * 0.5f);

                if (box.ClassId == classId)
                {
                    // If the new box overlaps with an existing one of the same class, reuse it
                    if (EsSentisInferenceRunManager.CalculateIoU(newBox, currentBox) > 0f)
                    {
                        if (reusedBox == null)
                        {
                            reusedBox = box;
                        }
                        else
                        {
                            // Same overlapping class - remove the existing box
                            ReturnToPool(box);
                            m_boxDrawn.RemoveAt(i);
                        }
                    }
                }
                // If the new box's IoU with another class is significant, remove the existing box
                else if (EsSentisInferenceRunManager.CalculateIoU(newBox, currentBox) > 0.1f)
                {
                    // Different overlapping class - remove the existing box
                    ReturnToPool(box);
                    m_boxDrawn.RemoveAt(i);
                }
            }

            if (reusedBox != null)
            {
                return reusedBox;
            }

            // Create a new box
            var newData = GetBoxFromPoolOrCreate();
            newData.ClassId = classId;
            newData.ClassName = m_labels[classId].Replace(" ", "_");
            m_boxDrawn.Add(newData);
            return newData;
        }

        private BoundingBoxData GetBoxFromPoolOrCreate()
        {
            if (m_boxPool.Count > 0)
            {
                var pooled = m_boxPool[m_boxPool.Count - 1];
                pooled.BoxRectTransform.gameObject.SetActive(true);
                pooled.CanvasMaker.gameObject.SetActive(true);
                m_boxPool.RemoveAt(m_boxPool.Count - 1);
                return pooled;
            }

            var boxRectTransform = Instantiate(m_detectionBoxPrefab, ContentParent);
            var canvasMaker = Instantiate(m_canvasMaker, ContentParent);

            boxRectTransform.gameObject.SetActive(true);
            return new BoundingBoxData
            {
                BoxRectTransform = boxRectTransform,
                CanvasMaker = canvasMaker,
            };
        }

        internal Transform ContentParent => m_detectionBoxPrefab.parent;

        private void ReturnToPool(BoundingBoxData box)
        {
            box.BoxRectTransform.gameObject.SetActive(false);
            box.CanvasMaker.gameObject.SetActive(false);
            m_boxPool.Add(box);
        }

        internal void ClearAnnotations()
        {
            foreach (var box in m_boxDrawn)
            {
                ReturnToPool(box);
            }
            m_boxDrawn.Clear();
        }
    }
}