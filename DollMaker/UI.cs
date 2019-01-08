using System;
using UnityEngine;
using UnityEngine.UI;
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
            SuperController.singleton.AddCanvas(canvas);

            CanvasScaler cs = canvasObject.AddComponent<CanvasScaler>();
            cs.scaleFactor = 100.0f;
            cs.dynamicPixelsPerUnit = 1f;

            GraphicRaycaster gr = canvasObject.AddComponent<GraphicRaycaster>();

            canvas.transform.localScale = new Vector3(scale, scale, scale);
        }

        public UIDynamicSlider CreateSlider(string name, float width = 300, float height = 80, bool minimal = true)
        {
            Transform slider = GameObject.Instantiate<Transform>(plugin.manager.configurableSliderPrefab);
            ConfigureTransform(slider, width, height);
            ParentToCanvas(slider);

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

        public UIDynamicButton CreateButton(string name, float width = 100, float height = 80)
        {
            Transform button = GameObject.Instantiate<Transform>(plugin.manager.configurableButtonPrefab);
            ConfigureTransform(button, width, height);
            ParentToCanvas(button);

            UIDynamicButton uiButton = button.GetComponent<UIDynamicButton>();
            uiButton.label = name;
            return uiButton;
        }

        public UIDynamicToggle CreateToggle(string name, float width = 100, float height = 80)
        {
            Transform toggle = GameObject.Instantiate<Transform>(plugin.manager.configurableTogglePrefab);
            ConfigureTransform(toggle, width, height);
            ParentToCanvas(toggle);

            UIDynamicToggle uiToggle = toggle.GetComponent<UIDynamicToggle>();
            uiToggle.label = name;
            return uiToggle;
        }

        public UIDynamicPopup CreatePopup(string name, float width = 100, float height = 80)
        {
            Transform popup = GameObject.Instantiate<Transform>(plugin.manager.configurablePopupPrefab);
            ConfigureTransform(popup, width, height);
            ParentToCanvas(popup);

            UIDynamicPopup uiPopup = popup.GetComponent<UIDynamicPopup>();
            uiPopup.label = name;
            return uiPopup;
        }

        public InputField CreateTextInput(string defaultText, float width = 400, float height = 120)
        {
            int fontSize = 40;
            Color fontColor = new Color(1, 1, 1);

            GameObject container = new GameObject();
            container.name = "InputField";
            ParentToCanvas(container.transform);

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

            return inputField;
        }

        public ScrollRect CreateScrollRect(float width = 300, float height = 500)
        {
            GameObject vGo = new GameObject();
            Image srImage = vGo.gameObject.AddComponent<Image>();
            srImage.color = DollMaker.BG_COLOR;

            ScrollRect sr = vGo.AddComponent<ScrollRect>();
            RectTransform vRT = vGo.GetComponent<RectTransform>();
            ParentToCanvas(vRT);
            sr.gameObject.AddComponent<Mask>();
            sr.viewport = vRT;
            sr.elasticity = 0;

            ConfigureTransform(vRT, width, height);
            return sr;
        }

        public VerticalLayoutGroup CreateVerticalLayout(float width = 300, float height = 500)
        {
            GameObject go = new GameObject();
            RectTransform rt = go.AddComponent<RectTransform>();
            ParentToCanvas(rt);

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

        private void ConfigureTransform(Transform t, float width, float height)
        {
            t.transform.position = Vector3.zero;
            RectTransform rt = t.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(width / 2, height / 2);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        private void ParentToCanvas(Transform t)
        {
            t.SetParent(canvas.transform, false);
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
    }
}
