using System.Collections.Generic;
using Meta.XR;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace MultiObjectDetection
{
    public class EsSentisInferenceUiManager : MonoBehaviour
    {
        [SerializeField] private EsDetectionManager m_detectionManager;

        [Header("Placement configuration")]
        [SerializeField] private EsEnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;

        [SerializeField]
        private Transform _contentParent;

        public Transform ContentParent => _contentParent;

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
            public EsDetectionCanvasMaker CanvasMaker;
            public float lastUpdateTime;
            public Vector2 Size;
        }

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

        public void DrawUIBoxes(List<(int classId, Vector4 boundingBox)> detections, Texture targetTexture, Vector2 inputSize, Pose cameraPose)
        {
            Vector2 currentResolution = m_cameraAccess.CurrentResolution;

            if (detections.Count == 0)
            {
                OnObjectsDetected?.Invoke(0);
                return;
            }

            OnObjectsDetected?.Invoke(detections.Count);

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

                var boxRectTransform = boxData.CanvasMaker.CanvasMakerRectTransform;
                boxRectTransform.GetComponentInChildren<Text>().text = $"Id: {detection.classId} Class: {classname} Center (px): {center:0.0} Center (%): {normalizedCenter:0.0}";
                boxRectTransform.SetPositionAndRotation(worldSpaceCenter, Quaternion.LookRotation(normal));

                var imageRectTransform = boxData.CanvasMaker.ImageMakerRectTransform;
                imageRectTransform.sizeDelta = size;

                // 1) 构造平面（法线来自世界中心指向相机的反方向已归一化）
                var reallyplane = new Plane(normal, worldSpaceCenter);

                // 2) 计算四个角的 Viewport 坐标
                Vector2 vpBL = normRect.min;                        // (xMin, yMin)
                Vector2 vpTR = normRect.max;                        // (xMax, yMax)
                Vector2 vpTL = new Vector2(vpBL.x, vpTR.y);         // (xMin, yMax)
                Vector2 vpBR = new Vector2(vpTR.x, vpBL.y);         // (xMax, yMin)

                // 3) 四角射线
                var rayBL = m_cameraAccess.ViewportPointToRay(vpBL, cameraPose);
                var rayTL = m_cameraAccess.ViewportPointToRay(vpTL, cameraPose);
                var rayTR = m_cameraAccess.ViewportPointToRay(vpTR, cameraPose);
                var rayBR = m_cameraAccess.ViewportPointToRay(vpBR, cameraPose);

                // 4) 射线与平面求交
                Vector3 pBL = default, pTL = default, pTR = default, pBR = default;
                if (reallyplane.Raycast(rayBL, out float dBL)) pBL = rayBL.GetPoint(dBL);
                if (reallyplane.Raycast(rayTL, out float dTL)) pTL = rayTL.GetPoint(dTL);
                if (reallyplane.Raycast(rayTR, out float dTR)) pTR = rayTR.GetPoint(dTR);
                if (reallyplane.Raycast(rayBR, out float dBR)) pBR = rayBR.GetPoint(dBR);

                // 现在就有四个世界坐标点（按屏幕顺序：TL, TR, BR, BL）
                Vector3[] worldQuad = new[] { pTL, pTR, pBR, pBL };

                Debug.Log($"Set Out Line: {worldQuad[0]} {worldQuad[1]} {worldQuad[2]} {worldQuad[3]} - worldSpaceCenter{worldSpaceCenter}");

                boxData.CanvasMaker.SetOutLine(worldQuad);
                //
                // 2) 构建平面内坐标轴（先做一次平面投影，避免测量噪声带来的法线分量干扰）
                Vector3 xApprox = Vector3.ProjectOnPlane(pTR - pTL, normal); // 右向（TL->TR）
                Vector3 yApprox = Vector3.ProjectOnPlane(pTL - pBL, normal); // 上向（BL->TL）

                // 3) 正交化并保证右手系：z=normal, x=normalize(cross(y,z)), y=normalize(cross(z,x))
                Vector3 zAxis = normal.sqrMagnitude > 0 ? normal.normalized : Vector3.forward;
                Vector3 xAxis = Vector3.Cross(yApprox, zAxis);
                if (xAxis.sqrMagnitude < 1e-8f)
                {
                    // 退化处理：若 TL-TR 与 T-B 接近共线，尝试用另一条边
                    xApprox = Vector3.ProjectOnPlane(pBR - pBL, zAxis);
                    xAxis = Vector3.Cross(yApprox, zAxis);
                }
                xAxis = xAxis.normalized;

                Vector3 yAxis = Vector3.Cross(zAxis, xAxis).normalized;

                // 4) 生成旋转（LookRotation 的第一个参数是 forward，这里用 zAxis；第二个参数是 up，用 yAxis）
                Quaternion worldRot = Quaternion.LookRotation(zAxis, yAxis);


                Quaternion localRotFull = Quaternion.Inverse(boxData.CanvasMaker.CanvasMakerRectTransform.rotation) * worldRot;

                // 新计算得到的目标 local yaw（Z）
                Vector3 targetEuler = new Vector3(0, 0, localRotFull.eulerAngles.z);

                // 5) 应用到 RectTransform（World Space Canvas）
                boxData.CanvasMaker.ImageMakerRectTransform.localEulerAngles = targetEuler;

                // 获取相机原始纹理
                Texture2D cameraTexture = null;
                //RenderTexture
                if (targetTexture is RenderTexture rt)
                {
                    Debug.Log("Is RenderTexture");
                    cameraTexture = readFromRT(rt, false);
                }
                else
                {
                    Debug.Log("Other Texture");
                    cameraTexture = getCameraSnapshot();
                }

                // === 新增：截取识别区域的纹理 ===
                if (cameraTexture != null)
                {
                    // 转换为像素坐标
                    int texWidth = cameraTexture.width;
                    int texHeight = cameraTexture.height;
                    int cropX = (int)(normRect.x * texWidth);
                    int cropY = (int)(normRect.y * texHeight);
                    int cropWidth = (int)(normRect.width * texWidth);
                    int cropHeight = (int)(normRect.height * texHeight);

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

                boxData.Size = size;
                boxData.lastUpdateTime = Time.time;
            }
        }

        private Texture2D readFromRT(RenderTexture rt, bool linear)
        {
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, linear);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply(false, false);
            RenderTexture.active = prev;
            return tex;
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
                var localPos = box.CanvasMaker.CanvasMakerRectTransform.InverseTransformPoint(worldSpaceCenter);
                var newBox = new Vector4(
                    localPos.x - worldSpaceSize.x * 0.5f,
                    localPos.y - worldSpaceSize.y * 0.5f,
                    localPos.x + worldSpaceSize.x * 0.5f,
                    localPos.y + worldSpaceSize.y * 0.5f
                );

                var sizeDelta = box.CanvasMaker.ImageMakerRectTransform.sizeDelta;
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
                pooled.CanvasMaker.gameObject.SetActive(true);
                m_boxPool.RemoveAt(m_boxPool.Count - 1);
                return pooled;
            }

            var canvasMaker = Instantiate(m_canvasMaker, _contentParent);

            return new BoundingBoxData
            {
                CanvasMaker = canvasMaker,
            };
        }



        private void ReturnToPool(BoundingBoxData box)
        {
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