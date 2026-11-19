using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Renders an item's inspect prefab into a RawImage with its own camera and light, far away from the level.
    /// Drag with the mouse to rotate; scroll to zoom.
    /// </summary>
    public sealed class ItemInspector : MonoBehaviour, IDragHandler, IScrollHandler
    {
        private static readonly Vector3 StageOrigin = new(0f, -500f, 0f);

        [SerializeField] private RawImage target;
        [SerializeField] private int resolution = 768;

        private RenderTexture _rt;
        private Camera _camera;
        private GameObject _stage;
        private Transform _pivot;
        private GameObject _model;
        private float _distance = 0.9f;

        private void Awake() => EnsureStage();

        private void EnsureStage()
        {
            if (_stage != null) return;
            _stage = new GameObject("[ItemInspectStage]");
            _stage.transform.position = StageOrigin;
            _pivot = new GameObject("Pivot").transform;
            _pivot.SetParent(_stage.transform, false);

            var camGo = new GameObject("InspectCamera");
            camGo.transform.SetParent(_stage.transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.fieldOfView = 30f;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 10f;
            _camera.enabled = false;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;

            var key = new GameObject("Key").AddComponent<Light>();
            key.transform.SetParent(_stage.transform, false);
            key.type = LightType.Directional;
            key.color = new Color(1f, 0.85f, 0.7f);
            key.intensity = 1.6f;
            key.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            var rim = new GameObject("Rim").AddComponent<Light>();
            rim.transform.SetParent(_stage.transform, false);
            rim.type = LightType.Directional;
            rim.color = new Color(0.4f, 0.9f, 0.9f);
            rim.intensity = 1.1f;
            rim.transform.rotation = Quaternion.Euler(10f, 150f, 0f);
            // keep the stage lights from touching the level (they are far away anyway, but directional lights are global)
            key.cullingMask = rim.cullingMask = 1 << LayerMask.NameToLayer("UI");
            _camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            _stage.SetActive(false);
        }

        public void Show(GameObject prefab)
        {
            EnsureStage();
            Clear();
            if (prefab == null)
            {
                target.enabled = false;
                return;
            }
            if (_rt == null)
            {
                _rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                _camera.targetTexture = _rt;
            }
            target.texture = _rt;
            target.enabled = true;
            _stage.SetActive(true);
            _camera.enabled = true;

            _model = Instantiate(prefab, _pivot);
            _model.transform.localPosition = Vector3.zero;
            int uiLayer = LayerMask.NameToLayer("UI");
            foreach (var t in _model.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = uiLayer;
            foreach (var c in _model.GetComponentsInChildren<Collider>(true)) Destroy(c);

            // frame the model
            var renderers = _model.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                _model.transform.position -= b.center - _pivot.position;
                float halfFov = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
                _distance = Mathf.Max(0.35f, b.extents.magnitude / Mathf.Tan(halfFov) * 1.25f);
            }
            _pivot.localRotation = Quaternion.Euler(15f, -30f, 0f);
            UpdateCamera();
        }

        public void Clear()
        {
            if (_model != null) Destroy(_model);
            _model = null;
            if (_camera != null) _camera.enabled = false;
            if (_stage != null) _stage.SetActive(false);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_model == null) return;
            _pivot.Rotate(Vector3.up, -eventData.delta.x * 0.4f, Space.World);
            _pivot.Rotate(Vector3.right, eventData.delta.y * 0.4f, Space.World);
        }

        public void OnScroll(PointerEventData eventData)
        {
            _distance = Mathf.Clamp(_distance - eventData.scrollDelta.y * 0.05f, 0.2f, 3f);
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            _camera.transform.position = _pivot.position + new Vector3(0f, 0f, -_distance);
            _camera.transform.LookAt(_pivot.position);
        }

        private void OnDestroy()
        {
            if (_stage != null) Destroy(_stage);
            if (_rt != null) _rt.Release();
        }

#if UNITY_EDITOR
        public void EditorBind(RawImage image) => target = image;
#endif
    }
}
