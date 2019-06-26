using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.DollMaker
{
    public class UI
    {
        public Canvas canvas;

        MVRScript plugin;

        public bool lookAtCamera = true;
        private float UIScale = 1.0f;

        public UI(MVRScript plugin, float scale = 0.002f)
        {
            this.plugin = plugin;
            this.UIScale = scale;

            GameObject canvasObject = new GameObject();
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.pixelPerfect = false;
            SuperController.singleton.AddCanvas(canvas);

            CanvasScaler cs = canvasObject.AddComponent<CanvasScaler>();
            cs.scaleFactor = 40.0f;
            cs.dynamicPixelsPerUnit = 1.0f;

            GraphicRaycaster gr = canvasObject.AddComponent<GraphicRaycaster>();

            canvas.transform.localScale = new Vector3(scale, scale, scale);
        }

        public UIDynamicSlider CreateSlider(string name, float width = 300, float height = 80, bool minimal = true, Transform parent=null)
        {
            Transform slider = GameObject.Instantiate<Transform>(plugin.manager.configurableSliderPrefab);
            ConfigureTransform(slider, width, height);

            ParentTo(slider, parent);

            UIDynamicSlider sliderDynamic = slider.GetComponent<UIDynamicSlider>();
            if (minimal)
            {
                sliderDynamic.quickButtonsEnabled = false;
                sliderDynamic.rangeAdjustEnabled = false;
                sliderDynamic.defaultButtonEnabled = false;
            }
            sliderDynamic.label = name;

            return sliderDynamic;
        }

        public UIDynamicButton CreateButton(string name, float width = 100, float height = 80, Transform parent = null)
        {
            Transform button = GameObject.Instantiate<Transform>(plugin.manager.configurableButtonPrefab);
            ConfigureTransform(button, width, height);

            ParentTo(button, parent);

            UIDynamicButton uiButton = button.GetComponent<UIDynamicButton>();
            uiButton.label = name;

            //ContentSizeFitter csf = button.gameObject.AddComponent<ContentSizeFitter>();
            //csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            //csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return uiButton;
        }

        public UIDynamicToggle CreateToggle(string name, float width = 100, float height = 80, Transform parent = null)
        {
            Transform toggle = GameObject.Instantiate<Transform>(plugin.manager.configurableTogglePrefab);
            ConfigureTransform(toggle, width, height);

            ParentTo(toggle, parent);

            UIDynamicToggle uiToggle = toggle.GetComponent<UIDynamicToggle>();
            uiToggle.label = name;
            return uiToggle;
        }

        public UIDynamicPopup CreatePopup(string name, float width = 100, float height = 80, Transform parent = null)
        {
            Transform popup = GameObject.Instantiate<Transform>(plugin.manager.configurablePopupPrefab);
            ConfigureTransform(popup, width, height);

            ParentTo(popup, parent);

            UIDynamicPopup uiPopup = popup.GetComponent<UIDynamicPopup>();
            uiPopup.label = name;
            return uiPopup;
        }

        public InputField CreateTextInput(string defaultText, float width = 400, float height = 120, Transform parent = null)
        {
            int fontSize = 40;
            Color fontColor = new Color(1, 1, 1);

            GameObject container = new GameObject();
            container.name = "InputField";

            ParentTo(container.transform, parent);

            GameObject imageContainer = new GameObject();
            imageContainer.transform.SetParent(container.transform, false);
            Image image = imageContainer.AddComponent<Image>();
            RectTransform imageRT = image.GetComponent<RectTransform>();
            imageRT.anchoredPosition = new Vector2(0,0);
            imageRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            imageRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            image.color = new Color(0.5f, 0.5f, 0.5f);

            Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

            GameObject textContainer = new GameObject();
            textContainer.name = "Text";
            textContainer.transform.SetParent(container.transform, false);

            Text activeText = textContainer.AddComponent<Text>();
            activeText.color = fontColor;
            activeText.font = ArialFont;
            activeText.fontSize = fontSize;
            activeText.supportRichText = false;
            activeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            //activeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            activeText.text = "";
            activeText.alignment = TextAnchor.MiddleCenter;

            RectTransform rt = textContainer.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0,0);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);


            GameObject placeholderContainer = new GameObject();
            placeholderContainer.name = "Placeholder";
            placeholderContainer.transform.SetParent(container.transform, false);

            Text placeholderText = placeholderContainer.AddComponent<Text>();
            placeholderText.color = new Color(1, 1, 1);
            placeholderText.font = ArialFont;
            placeholderText.fontSize = fontSize;
            placeholderText.fontStyle = FontStyle.Italic;
            placeholderText.supportRichText = false;
            placeholderText.horizontalOverflow = HorizontalWrapMode.Overflow;
            placeholderText.text = defaultText;
            placeholderText.alignment = TextAnchor.MiddleCenter;

            InputField inputField = container.AddComponent<InputField>();
            inputField.targetGraphic = image;
            inputField.textComponent = activeText;
            inputField.placeholder = placeholderText;
            inputField.characterLimit = 100;

            Text t = container.AddComponent<Text>();

            //inputField.transform.localScale = Vector3.one * 0.01f;

            ConfigureTransform(container.transform, width, height);

            container.GetComponent<RectTransform>().pivot = new Vector2(0, 0);

            return inputField;
        }

        public ScrollRect CreateScrollRect(float width = 300, float height = 500, Transform parent = null)
        {
            GameObject vGo = new GameObject();
            Image srImage = vGo.gameObject.AddComponent<Image>();
            srImage.color = DollMaker.BG_COLOR;

            ScrollRect sr = vGo.AddComponent<ScrollRect>();
            RectTransform vRT = vGo.GetComponent<RectTransform>();
            ParentTo(vRT, parent);
            sr.gameObject.AddComponent<Mask>();
            sr.viewport = vRT;
            sr.elasticity = 0;

            ConfigureTransform(vRT, width, height);

            return sr;
        }

        public VerticalLayoutGroup CreateVerticalLayout(float width = 300, float height = 500, Transform parent = null)
        {
            GameObject go = new GameObject();
            RectTransform rt = go.AddComponent<RectTransform>();

            ParentTo(rt, parent);

            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = false;
            vlg.childForceExpandHeight = true;
            vlg.childForceExpandWidth = true;

            rt.anchoredPosition = new Vector2(0, 0);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            //ConfigureTransform(rt, width, height);
            return vlg;
        }

        public GridLayoutGroup CreateGridLayout(float width = 300, float height = 500, Transform parent = null)
        {
            GameObject go = new GameObject();
            RectTransform rt = go.AddComponent<RectTransform>();

            ParentTo(rt, parent);

            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            GridLayoutGroup gridLayoutGroup = go.AddComponent<GridLayoutGroup>();
            rt.anchoredPosition = new Vector2(0, 0);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            //ConfigureTransform(rt, width, height);
            return gridLayoutGroup;
        }

        public VerticalLayoutGroup CreateVerticalScrollArea(float width=1200, float height=800, Transform parent = null)
        {
            ScrollRect sr = CreateScrollRect(width, height);

            VerticalLayoutGroup vlg = CreateVerticalLayout(width, height);
            vlg.transform.SetParent(sr.transform, false);
            vlg.padding = new RectOffset(0, 0, 25, 25);

            RectTransform vlgrt = vlg.GetComponent<RectTransform>();
            vlgrt.pivot = new Vector2(0.5f, 1);
            vlgrt.anchoredPosition = new Vector2(0, 0);
            sr.content = vlgrt;

            ContentSizeFitter csf = vlg.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ParentTo(vlg.transform, parent);

            return vlg;
        }

        public UIDynamicSlider CreateMorphSlider(string name, float width = 300, float height = 80, Transform parent = null)
        {
            UIDynamicSlider slider = CreateSlider(name, width, height, true);

            slider.transform.Find("Panel").GetComponent<Image>().color = new Color(0, 0, 0);

            slider.slider.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);

            RectTransform sliderRT = slider.slider.GetComponent<RectTransform>();
            sliderRT.sizeDelta = new Vector2(1, 80);
            //sliderRT.anchoredPosition = new Vector2(0, -0.5f);
            sliderRT.pivot = new Vector2(0, 0.5f);

            Image fillRectImage = slider.slider.fillRect.GetComponent<Image>();
            fillRectImage.color = new Color(0.1f, 0.1f, 0.1f);

            slider.labelText.color = new Color(1, 1, 1);
            slider.GetComponentsInChildren<Text>().ToList().ForEach((Text text) =>
            {
                text.color = new Color(1, 1, 1);
                text.raycastTarget = false;
                //text.fontSize = 16;
                //RectTransform textRT = text.GetComponent<RectTransform>();
                //textRT.sizeDelta = new Vector2(120, 80);
                //textRT.pivot = new Vector2(-0.2f, 0.5f);
                //textRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            });

            slider.slider.transform.Find("Handle Slide Area").gameObject.SetActive(false);

            InputField inputField = slider.GetComponentInChildren<InputField>();
            inputField.enabled = false;

            EventTrigger trigger = slider.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry mouseEnter = new EventTrigger.Entry();
            mouseEnter.eventID = EventTriggerType.PointerEnter;
            mouseEnter.callback.AddListener((eventData) =>
            {
                fillRectImage.color = new Color(0.3f, 0.4f, 0.6f);
            });
            trigger.triggers.Add(mouseEnter);

            EventTrigger.Entry mouseExit = new EventTrigger.Entry();
            mouseExit.eventID = EventTriggerType.PointerExit;
            mouseExit.callback.AddListener((eventData) =>
            {
                fillRectImage.color = new Color(0.1f, 0.1f, 0.1f);
            });
            trigger.triggers.Add(mouseExit);
            ParentTo(slider.transform, parent);

            //sliderRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 350);
            return slider;
        }

        public void DebugRoot()
        {
            Debug.Log("----- Debug root -----");
            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType(typeof(GameObject)))
            {
                if (go.transform.parent == null)
                {
                    DebugDeeper(go.transform);
                }
            }
        }

        public void DebugDeeper(Transform trans)
        {
            int children = trans.childCount;
            Debug.Log(trans.name + " has " + children);

            for (int child = 0; child < children; child++)
            {
                DebugDeeper(trans.GetChild(child));
            }
        }

        private void ConfigureTransform(Transform t, float width, float height)
        {
            t.transform.position = Vector3.zero;
            RectTransform rt = t.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(width / 2, height / 2);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            rt.pivot = new Vector2(0, 0);
        }

        private void ParentTo(Transform t, Transform parent)
        {
            if (parent == null)
            {
                parent = canvas.transform;
            }
            t.SetParent(parent, false);
        }

        public void Update()
        {
            {
                if (XRSettings.enabled == false)
                {
                    Transform cameraT = SuperController.singleton.lookCamera.transform;
                    Vector3 endPos = cameraT.position + cameraT.forward * 10000000.0f;
                    canvas.transform.LookAt(endPos, cameraT.up);
                }
                else
                {
                    canvas.transform.localEulerAngles = new Vector3(0, 180, 0);
                }

                //canvas.transform.localScale = Vector3.one * plugin.containingAtom.GetStorableByID("scale").GetFloatParamValue("scale") * UIScale;
            }
        }

        public void OnDestroy()
        {
            SuperController.singleton.RemoveCanvas(canvas);
            if (canvas.gameObject != null)
            {
                GameObject.Destroy(canvas.gameObject);
            }
        }

        public static void ColorButton(UIDynamicButton button, Color textColor, Color buttonColor)
        {
            button.textColor = textColor;
            button.buttonColor = buttonColor;
        }
    }
}
