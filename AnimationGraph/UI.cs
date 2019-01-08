using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace DeluxePlugin.AnimationGraph
{
    public class UI
    {
        public Canvas canvas;

        public MVRScript plugin;

        float UIScale = 1;

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

        public UIDynamicSlider CreateSlider(string name, float width = 300, float height = 80)
        {
            Transform slider = GameObject.Instantiate<Transform>(plugin.manager.configurableSliderPrefab);
            ConfigureTransform(slider, width, height);

            UIDynamicSlider sliderDynamic = slider.GetComponent<UIDynamicSlider>();
            sliderDynamic.quickButtonsEnabled = false;
            sliderDynamic.rangeAdjustEnabled = false;
            sliderDynamic.defaultButtonEnabled = false;
            sliderDynamic.label = name;

            return sliderDynamic;
        }

        public UIDynamicButton CreateButton(string name, float width = 100, float height = 80)
        {
            Transform button = GameObject.Instantiate<Transform>(plugin.manager.configurableButtonPrefab);
            ConfigureTransform(button, width, height);

            UIDynamicButton uiButton = button.GetComponent<UIDynamicButton>();
            uiButton.label = name;

            return uiButton;
        }

        public UIDynamicToggle CreateToggle(string name, float width = 100, float height = 80)
        {
            Transform toggle = GameObject.Instantiate<Transform>(plugin.manager.configurableTogglePrefab);
            ConfigureTransform(toggle, width, height);

            UIDynamicToggle uiToggle = toggle.GetComponent<UIDynamicToggle>();
            uiToggle.label = name;
            return uiToggle;
        }

        public UIDynamicPopup CreatePopup(string name, float width = 100, float height = 80)
        {
            Transform popup = GameObject.Instantiate<Transform>(plugin.manager.configurablePopupPrefab);
            ConfigureTransform(popup, width, height);

            UIDynamicPopup uiPopup = popup.GetComponent<UIDynamicPopup>();
            uiPopup.label = name;
            return uiPopup;
        }

        public RectTransform CreateHorizontalLayout(float width = 300, float height = 500)
        {
            GameObject go = new GameObject();
            RectTransform rt = go.AddComponent<RectTransform>();
            ContentSizeFitter csf = go.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = true;

            rt.SetParent(canvas.transform, false);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public RectTransform CreateScrollRect(float width = 300, float height = 500)
        {
            GameObject vGo = new GameObject();
            vGo.AddComponent<VerticalLayoutGroup>();
            RectTransform vRT = vGo.GetComponent<RectTransform>();
            vRT.SetParent(canvas.transform, false);
            return vRT;
        }

        private void ConfigureTransform(Transform t, float width, float height)
        {
            t.transform.position = Vector3.zero;
            t.SetParent(canvas.transform, false);

            RectTransform rt = t.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
        }

        public void Update()
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

            canvas.transform.localScale = Vector3.one * plugin.containingAtom.GetStorableByID("scale").GetFloatParamValue("scale") * UIScale;
        }

        public void OnDestroy()
        {
            SuperController.singleton.RemoveCanvas(canvas);
            GameObject.Destroy(canvas.gameObject);
        }
    }
}
