using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.Arousal
{
    public class Arousal: MVRScript
    {
        UI ui;
        JSONStorableFloat arousalValue;
        JSONStorableFloat arousalRate;
        JSONStorableFloat arousalDecay;
        JSONStorableFloat timeToDecay;

        const float SLIDER_MAX = 100;

        float lastArousalTime = 0;

        public override void Init()
        {
            try
            {
                ui = new UI(this, 0.001f);
                ui.canvas.transform.SetParent(containingAtom.mainController.transform, false);
                ui.canvas.transform.localPosition = new Vector3(0, -0.15f, 0);

                arousalValue = new JSONStorableFloat("arousal", 0, 0, SLIDER_MAX, false, false);
                RegisterFloat(arousalValue);

                UIDynamicSlider arousalSlider = ui.CreateSlider("❤", 300, 120);
                arousalSlider.valueFormat = "n0";
                arousalValue.slider = arousalSlider.slider;

                arousalRate = new JSONStorableFloat("arousalRate", 1, 0.01f, 10, false, true);
                RegisterFloat(arousalRate);
                CreateSlider(arousalRate);

                arousalDecay = new JSONStorableFloat("arousalDecay", 0.1f, 0.01f, 10, false, true);
                RegisterFloat(arousalDecay);
                CreateSlider(arousalDecay);

                timeToDecay = new JSONStorableFloat("timeToDecay", 4, 0, 60, false, true);
                RegisterFloat(timeToDecay);
                CreateSlider(timeToDecay);

                JSONStorableAction arousalAction = new JSONStorableAction("arouse", () =>
                {
                    arousalValue.val += arousalRate.val;
                    lastArousalTime = Time.time;
                });
                RegisterAction(arousalAction);
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Update()
        {
            ui.Update();

            if((Time.time - lastArousalTime) > timeToDecay.val)
            {
                Decay();
            }
        }

        void Decay()
        {
            arousalValue.val -= arousalDecay.val;
            arousalValue.val = Mathf.Clamp(arousalValue.val, 0, SLIDER_MAX);
        }

        void OnDestroy()
        {
            if (ui != null)
            {
                ui.OnDestroy();
            }
        }

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
}
