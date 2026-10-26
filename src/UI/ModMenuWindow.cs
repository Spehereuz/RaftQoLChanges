using System;
using UnityEngine;
using UnityEngine.UI;

namespace QoLChanges.UI
{
    /// <summary>Window cloned from Raft's settings panel: title, close button and a scrollable list.</summary>
    internal sealed class ModMenuWindow
    {
        private readonly GameObject _canvas;
        private readonly Text _title;
        private readonly ScrollRect _scroll;

        private ModMenuWindow(GameObject canvas, Text title, ScrollRect scroll, Transform content)
        {
            _canvas = canvas;
            _title = title;
            _scroll = scroll;
            Content = content;
        }

        public Transform Content { get; }

        public bool IsAlive => _canvas != null;

        public bool IsOpen => _canvas != null && _canvas.activeSelf;

        public static ModMenuWindow Create(RaftUiTemplates templates, int sortingOrder, Action onClose)
        {
            GameObject canvas = CreateCanvas(templates.SettingsCanvas, sortingOrder);

            GameObject panel = RaftUi.Clone(templates.SettingsPanel, "Panel");
            Transform panelTransform = panel.transform;

            RaftUi.DestroyChild(panelTransform, "TabContainer");
            RaftUi.DestroyChild(panelTransform, "LB");
            RaftUi.DestroyChild(panelTransform, "RB");
            RaftUi.HideChild(panelTransform, "GamepadBack");

            Transform tabContent = panelTransform.Find(templates.SettingsScrollView.transform.parent.name);
            Transform scrollView = tabContent != null ? tabContent.Find(templates.SettingsScrollView.name) : null;
            if (scrollView == null)
            {
                UnityEngine.Object.Destroy(canvas);
                UnityEngine.Object.Destroy(panel);
                throw new InvalidOperationException("Settings scroll view not found in the cloned panel.");
            }

            for (int i = tabContent.childCount - 1; i >= 0; i--)
            {
                Transform child = tabContent.GetChild(i);
                if (child != scrollView)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            scrollView.gameObject.SetActive(true);
            ScrollRect scroll = scrollView.GetComponent<ScrollRect>();
            Transform content = scroll.content;
            RaftUi.DestroyChildren(content);
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text title = SetupTitle(panelTransform);
            SetupCloseButton(panelTransform, onClose);

            RectTransform panelRect = (RectTransform)panelTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            RaftUi.Place(panel, canvas.transform);
            canvas.SetActive(false);
            return new ModMenuWindow(canvas, title, scroll, content);
        }

        public void Show(string title)
        {
            RaftUi.SetText(_title, title);
            _canvas.SetActive(true);
        }

        public void Hide()
        {
            if (_canvas != null)
            {
                _canvas.SetActive(false);
            }
        }

        public void SetTitle(string title)
        {
            RaftUi.SetText(_title, title);
        }

        public void ClearContent()
        {
            RaftUi.DestroyChildren(Content);
        }

        public void ScrollToTop()
        {
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 1f;
        }

        public void Destroy()
        {
            if (_canvas != null)
            {
                UnityEngine.Object.Destroy(_canvas);
            }
        }

        private static GameObject CreateCanvas(Canvas source, int sortingOrder)
        {
            GameObject canvasObject = new GameObject("QoLChanges_ModMenu", typeof(RectTransform));
            canvasObject.layer = source.gameObject.layer;

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = source.renderMode;
            canvas.worldCamera = source.worldCamera;
            canvas.planeDistance = source.planeDistance;
            canvas.pixelPerfect = source.pixelPerfect;
            canvas.sortingLayerID = source.sortingLayerID;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler sourceScaler = source.GetComponent<CanvasScaler>();
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            if (sourceScaler != null)
            {
                scaler.uiScaleMode = sourceScaler.uiScaleMode;
                scaler.referenceResolution = sourceScaler.referenceResolution;
                scaler.screenMatchMode = sourceScaler.screenMatchMode;
                scaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
                scaler.scaleFactor = sourceScaler.scaleFactor;
                scaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
                scaler.physicalUnit = sourceScaler.physicalUnit;
                scaler.fallbackScreenDPI = sourceScaler.fallbackScreenDPI;
                scaler.defaultSpriteDPI = sourceScaler.defaultSpriteDPI;
            }

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject;
        }

        private static Text SetupTitle(Transform panel)
        {
            Transform titleTransform = panel.Find("OptionsText");
            Text title = titleTransform != null ? titleTransform.GetComponent<Text>() : null;
            if (title == null)
            {
                return null;
            }

            RectTransform rect = title.rectTransform;
            float left = rect.anchoredPosition.x - rect.sizeDelta.x * rect.pivot.x;
            rect.pivot = new Vector2(0f, rect.pivot.y);
            rect.sizeDelta = new Vector2(600f, rect.sizeDelta.y);
            rect.anchoredPosition = new Vector2(left, rect.anchoredPosition.y);
            title.alignment = TextAnchor.MiddleLeft;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            return title;
        }

        private static void SetupCloseButton(Transform panel, Action onClose)
        {
            Transform closeTransform = panel.Find("CloseButton");
            Button close = closeTransform != null ? closeTransform.GetComponent<Button>() : null;
            if (close == null)
            {
                return;
            }

            close.gameObject.SetActive(true);
            close.onClick.AddListener(() => onClose());
        }
    }
}
