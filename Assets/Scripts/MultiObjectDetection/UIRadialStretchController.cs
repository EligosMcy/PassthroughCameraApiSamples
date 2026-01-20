
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class UIRadialStretchController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public Material materialInstance; // 给这个脚本分配一个使用 UI/RadialStretch 的材质实例

    [Range(0f, 1.5f)] public float radius = 0.4f;
    [Range(-1f, 2f)] public float strength = 0.5f;
    [Range(0f, 1f)] public float falloff = 0.8f;

    RawImage raw;
    RectTransform rect;

    void Awake()
    {
        raw = GetComponent<RawImage>();
        rect = raw.rectTransform;

        // 确保不改到共享材质
        if (materialInstance != null)
        {
            // 绑定到 RawImage（不改 sharedMaterial，避免影响别的 UI）
            raw.material = new Material(materialInstance);
        }
        else if (raw.material != null)
        {
            raw.material = new Material(raw.material);
        }
    }

    void Start()
    {
        UpdateCommonParams();
        UpdateAspect();
        // 初始化中心点为中点
        SetCenter01(new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        // 尺寸可能在运行时变化，持续刷新宽高比
        UpdateAspect();
        UpdateCommonParams();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateCenterFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateCenterFromPointer(eventData);
    }

    void UpdateCenterFromPointer(PointerEventData eventData)
    {
        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out localPos))
        {
            // 把本地坐标（以 rect 中心为原点）转为 0..1
            Rect r = rect.rect;
            float u = Mathf.InverseLerp(r.xMin, r.xMax, localPos.x);
            float v = Mathf.InverseLerp(r.yMin, r.yMax, localPos.y);
            SetCenter01(new Vector2(u, v));
        }
    }

    void SetCenter01(Vector2 uv01)
    {
        if (raw.material != null)
        {
            raw.material.SetVector("_Center", uv01);
        }
    }

    void UpdateCommonParams()
    {
        if (raw.material == null) return;
        raw.material.SetFloat("_Radius", radius);
        raw.material.SetFloat("_Strength", strength);
        raw.material.SetFloat("_Falloff", falloff);
    }

    void UpdateAspect()
    {
        if (raw.material == null) return;
        // 以 Rect 实际像素尺寸计算宽高比
        var size = rect.rect.size;
        float aspect = (size.y == 0) ? 1f : (size.x / size.y);
        raw.material.SetFloat("_Aspect", aspect);
    }
}
