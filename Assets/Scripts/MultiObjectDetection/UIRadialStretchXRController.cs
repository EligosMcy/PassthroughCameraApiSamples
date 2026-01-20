
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
// 如果用 Meta XR 的 OVRInput，取消下一行注释
// using static OVRInput;

[RequireComponent(typeof(RawImage))]
public class UIRadialStretchXRController : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("材质（使用 UI/RadialStretchXR Shader）")]
    public Material materialTemplate;

    [Header("参数")]
    [Range(0f, 1.5f)] public float radius = 0.4f;
    [Range(-1f, 2f)] public float strength = 0.5f;
    [Range(0f, 1f)] public float falloff = 0.8f;

    private RawImage raw;
    private RectTransform rect;

    public InputActionProperty _editorTestInputActionProperty;

    // 可选：用控制器或手势的 3D 射线来驱动
    [Header("（可选）从 3D 射线驱动")]
    public Transform rayOrigin; // 比如 RightHandAnchor

    void Awake()
    {
        raw = GetComponent<RawImage>();
        rect = raw.rectTransform;

        // 确保每个 RawImage 使用**独立实例**材质，避免全局污染
        if (materialTemplate != null)
            raw.material = new Material(materialTemplate);
        else if (raw.material != null)
            raw.material = new Material(raw.material);
    }

    void Start()
    {
        UpdateAspect();
        UpdateParams();
        SetCenter01(new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        UpdateAspect();
        UpdateParams();

        // ——（可选）在 Editor/PC 下用鼠标射线测试 —— 
        if (rayOrigin != null && _editorTestInputActionProperty.action.IsPressed())
        {
            var ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (TryGetUVFromRay(ray, out var uv01))
                SetCenter01(uv01);
        }

        //——（示例）如果用 OVRInput：按下扳机时用右手射线 —— 
        // if (rayOrigin != null && OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
        // {
        //     var ray = new Ray(rayOrigin.position, rayOrigin.forward);
        //     if (TryGetUVFromRay(ray, out var uv01))
        //         SetCenter01(uv01);
        // }
    }

    public void OnPointerDown(PointerEventData eventData) => UpdateCenterFromPointer(eventData);
    public void OnDrag(PointerEventData eventData) => UpdateCenterFromPointer(eventData);

    void UpdateCenterFromPointer(PointerEventData eventData)
    {
        Vector2 localPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, eventData.position, eventData.pressEventCamera, out localPos))
        {
            Rect r = rect.rect;
            float u = Mathf.InverseLerp(r.xMin, r.xMax, localPos.x);
            float v = Mathf.InverseLerp(r.yMin, r.yMax, localPos.y);
            SetCenter01(new Vector2(u, v));
        }
    }

    // 从 3D 射线（控制器/手指）求交并换算成 UV（0..1）
    public bool TryGetUVFromRay(Ray ray, out Vector2 uv01)
    {
        // 以 RawImage 的平面计算交点
        Plane plane = new Plane(rect.forward, rect.position);
        if (plane.Raycast(ray, out float enter))
        {
            Vector3 hit = ray.GetPoint(enter);
            Vector3 local = rect.InverseTransformPoint(hit);
            Rect r = rect.rect;

            float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
            float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
            uv01 = new Vector2(u, v);
            return (u >= 0f && u <= 1f && v >= 0f && v <= 1f);
        }
        uv01 = default;
        return false;
    }

    void SetCenter01(Vector2 uv01)
    {
        if (raw.material != null)
            raw.material.SetVector("_Center", uv01);
    }

    void UpdateParams()
    {
        if (raw.material == null) return;
        raw.material.SetFloat("_Radius", radius);
        raw.material.SetFloat("_Strength", strength);
        raw.material.SetFloat("_Falloff", falloff);
    }

    void UpdateAspect()
    {
        if (raw.material == null) return;
        var size = rect.rect.size; // 局部像素尺寸即可，跟世界缩放无关
        float aspect = (size.y == 0) ? 1f : (size.x / size.y);
        raw.material.SetFloat("_Aspect", aspect);
    }
}
