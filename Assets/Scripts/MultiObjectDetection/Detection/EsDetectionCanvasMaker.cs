using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MultiObjectDetection
{
    public class EsDetectionCanvasMaker : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _canvasMakerRectTransform;

        [SerializeField]
        private RectTransform _imageMakerRectTransform;

        [SerializeField]
        private RawImage _image;

        [SerializeField]
        private List<Transform> _outLineRayMakerList;

        [SerializeField]
        private List<Text> _outLineRayMakerTextList;

        public RectTransform CanvasMakerRectTransform => _canvasMakerRectTransform;
        public RectTransform ImageMakerRectTransform => _imageMakerRectTransform;

        private string _className;

        public void SetTexture2D(Texture2D texture2D)
        {
            _image.texture = texture2D;
        }

        public void SetYoloClassName(string name)
        {
            _className = name;
        }

        public string GetYoloClassName()
        {
            return _className;
        }

        public void SetOutLine(Vector3[] worldQuad)
        {
            if (_outLineRayMakerList.Count != 4)
            {
                Debug.LogError($"Out Line Ray Maker Length Error: {_outLineRayMakerList.Count}");
                return;
            }

            if (_outLineRayMakerTextList.Count != 4)
            {
                Debug.LogError($"Out Line Ray Maker Text Length Error: {_outLineRayMakerList.Count}");
                return;
            }

            if (worldQuad.Length != 4)
            {
                Debug.LogError($"world Quad Length Error: {worldQuad.Length}");
                return;
            }

            // 按屏幕上从左上顺时针顺序排列
            for (int i = 0; i < 4; i++)
            {
                _outLineRayMakerList[i].position = worldQuad[i];
                _outLineRayMakerTextList[i].text = worldQuad[i].ToString();
            }
        }
    }
}