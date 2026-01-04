using UnityEngine;
using UnityEngine.UI;

namespace MultiObjectDetection
{
    public class EsDetectionCanvasMaker : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _canvasMakerRectTransform;

        public RectTransform CanvasMakerRectTransform => _canvasMakerRectTransform;

        [SerializeField]
        private RawImage _image;

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
    }
}