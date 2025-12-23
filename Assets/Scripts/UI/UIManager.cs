using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("Root")]
    [SerializeField] private Canvas _uiRootCanvas;
    [SerializeField] private RectTransform _popupRoot;

    private readonly List<PopupRequest> _pending = new();
    private readonly Stack<UIPopupBase> _stack = new();

    private int _sequenceCounter;
    private bool _processScheduled;

    protected override void OnInitialize()
    {
        EnsureCanvasRoot();
        EnsurePopupRoot();
    }

    private void LateUpdate()
    {
        if (_processScheduled == false)
        {
            return;
        }

        _processScheduled = false;
        ProcessPending();
    }

    public void RequestPopup(UIPopupBase popupPrefab, EPopupPriority priority, object payload = null, bool unique = true)
    {
        if (popupPrefab == null)
        {
            return;
        }

        if (unique == true)
        {
            if (IsAlreadyQueued(popupPrefab) == true)
            {
                return;
            }

            if (IsAlreadyOpen(popupPrefab) == true)
            {
                return;
            }
        }

        PopupRequest req = new PopupRequest();
        req.prefab = popupPrefab;
        req.priority = priority;
        req.payload = payload;
        req.unique = unique;
        req.sequence = _sequenceCounter;
        _sequenceCounter++;

        _pending.Add(req);
        _processScheduled = true;
    }

    public void CloseTopPopup()
    {
        if (_stack.Count <= 0)
        {
            return;
        }

        UIPopupBase top = _stack.Pop();
        if (top == null)
        {
            return;
        }

        top.OnClose();
        Destroy(top.gameObject);
    }

    public void ClosePopup(UIPopupBase target)
    {
        if (target == null)
        {
            return;
        }

        if (_stack.Count <= 0)
        {
            Destroy(target.gameObject);
            return;
        }

        UIPopupBase top = _stack.Peek();
        if (top != target)
        {
            return;
        }

        CloseTopPopup();
    }

    private void ProcessPending()
    {
        if (_pending.Count <= 0)
        {
            return;
        }

        SortPending();

        for (int i = 0; i < _pending.Count; i++)
        {
            PopupRequest req = _pending[i];
            if (req == null || req.prefab == null)
            {
                continue;
            }

            OpenPopupInternal(req);
        }

        _pending.Clear();
    }

    private void OpenPopupInternal(PopupRequest req)
    {
        if (_popupRoot == null)
        {
            EnsurePopupRoot();
        }

        UIPopupBase instance = Instantiate(req.prefab, _popupRoot);
        if (instance == null)
        {
            return;
        }

        DisableInnerCanvases(instance.transform);

        RectTransform rt = instance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        instance.SetPopupType(req.prefab.GetType());

        instance.transform.SetAsLastSibling();

        _stack.Push(instance);
        instance.OnOpen(req.payload);
    }

    private void EnsureCanvasRoot()
    {
        if (_uiRootCanvas == null)
        {
            _uiRootCanvas = Object.FindFirstObjectByType<Canvas>();
        }

        if (_uiRootCanvas == null)
        {
            GameObject go = new GameObject("[CanvasRoot]");
            go.layer = LayerMask.NameToLayer("UI");

            _uiRootCanvas = go.AddComponent<Canvas>();
            _uiRootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsurePopupRoot()
    {
        if (_popupRoot != null)
        {
            return;
        }

        Transform existing = _uiRootCanvas.transform.Find("[PopupRoot]");
        if (existing != null)
        {
            _popupRoot = existing as RectTransform;
        }

        if (_popupRoot == null)
        {
            GameObject go = new GameObject("[PopupRoot]");
            go.layer = LayerMask.NameToLayer("UI");

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(_uiRootCanvas.transform, false);

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            _popupRoot = rt;
        }
    }

    private void DisableInnerCanvases(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null)
            {
                continue;
            }

            c.enabled = false;
        }

        CanvasScaler[] scalers = root.GetComponentsInChildren<CanvasScaler>(true);
        for (int i = 0; i < scalers.Length; i++)
        {
            CanvasScaler s = scalers[i];
            if (s == null)
            {
                continue;
            }

            s.enabled = false;
        }

        GraphicRaycaster[] raycasters = root.GetComponentsInChildren<GraphicRaycaster>(true);
        for (int i = 0; i < raycasters.Length; i++)
        {
            GraphicRaycaster r = raycasters[i];
            if (r == null)
            {
                continue;
            }

            r.enabled = false;
        }
    }

    private void SortPending()
    {
        _pending.Sort(CompareRequest);
    }

    private int CompareRequest(PopupRequest a, PopupRequest b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        int pa = (int)a.priority;
        int pb = (int)b.priority;

        if (pa > pb) return -1;
        if (pa < pb) return 1;

        if (a.sequence < b.sequence) return -1;
        if (a.sequence > b.sequence) return 1;

        return 0;
    }

    private bool IsAlreadyQueued(UIPopupBase prefab)
    {
        for (int i = 0; i < _pending.Count; i++)
        {
            PopupRequest r = _pending[i];
            if (r == null)
            {
                continue;
            }

            if (r.prefab == prefab)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAlreadyOpen(UIPopupBase prefab)
    {
        if (prefab == null)
        {
            return false;
        }

        System.Type t = prefab.GetType();

        foreach (UIPopupBase p in _stack)
        {
            if (p == null)
            {
                continue;
            }

            if (p.PopupType == t)
            {
                return true;
            }
        }

        return false;
    }
}
