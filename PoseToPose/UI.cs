using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace DeluxePlugin.PoseToPose
{
    public class UI
    {
        public Canvas canvas;

        MVRScript plugin;

        public bool lookAtCamera = true;

        public UI(MVRScript plugin, float scale = 0.002f)
        {
            this.plugin = plugin;

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

        private void ConfigureTransform(Transform t, float width, float height)
        {
            t.transform.position = Vector3.zero;
            t.SetParent(canvas.transform, false);

            RectTransform timeSliderRT = t.GetComponent<RectTransform>();
            timeSliderRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            timeSliderRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        public void Update()
        {
            if (lookAtCamera)
            {
                Transform cameraT = SuperController.singleton.lookCamera.transform;
                Vector3 endPos = cameraT.position + cameraT.forward * 10000000.0f;
                canvas.transform.LookAt(endPos, cameraT.up);
            }
            else
            {
                canvas.transform.localRotation = Quaternion.identity;
            }

            canvas.enabled = SuperController.singleton.editModeToggle.isOn;
        }

        public void OnDestroy()
        {
            SuperController.singleton.RemoveCanvas(canvas);
            GameObject.Destroy(canvas.gameObject);
        }
    }
}
