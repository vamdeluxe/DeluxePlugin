using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.ThrustControl
{
    public class ThrustControl : MVRScript
    {
        UI ui;
        JSONStorableFloat thrustValue;
        JSONStorableString apName;
        JSONStorableFloat maxSpeed;

        AnimationPattern ap;

        const float SLIDER_MAX = 10;

        public override void Init()
        {
            try
            {
                ui = new UI(this, 0.001f);
                ui.canvas.transform.SetParent(containingAtom.mainController.transform, false);
                ui.canvas.transform.localPosition = new Vector3(0, -0.15f, 0);

                thrustValue = new JSONStorableFloat("thrust", 0, 0, SLIDER_MAX, true, true);
                RegisterFloat(thrustValue);

                UIDynamicSlider thrustSlider = ui.CreateSlider("Thrust Control", 300, 120);
                thrustSlider.valueFormat = "n0";
                thrustValue.slider = thrustSlider.slider;

                thrustSlider.slider.onValueChanged.AddListener((float value) =>
                {
                    if (ap != null)
                    {
                        float speed = Remap(thrustValue.val, 0, SLIDER_MAX, 0, maxSpeed.val);
                        ap.SetFloatParamValue("speed", speed);
                    }
                });

                apName = new JSONStorableString("apName", "");
                RegisterString(apName);

                CreateButton("Select Animation Pattern").button.onClick.AddListener(()=>
                {
                    SuperController.singleton.SelectModeAtom((atom) =>
                    {
                        if (atom.GetStorableByID("AnimationPattern") == null)
                        {
                            SuperController.LogError("You must select an animation pattern.");
                            return;
                        }

                        apName.SetVal(atom.name);
                        ap = atom.GetStorableByID("AnimationPattern") as AnimationPattern;
                    });
                });

                maxSpeed = new JSONStorableFloat("maxSpeed", 4, 0, 10, false, true);
                RegisterFloat(maxSpeed);
                CreateSlider(maxSpeed, true);

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Start()
        {
            if (apName.val != apName.defaultVal)
            {
                Atom atom = GetAtomById(apName.val);
                if (atom == null)
                {
                    SuperController.LogError("atom named " + apName + " not found");
                    return;
                }

                AnimationPattern foundAP = atom.GetStorableByID("AnimationPattern") as AnimationPattern;
                if (foundAP == null)
                {
                    SuperController.LogError("atom " + apName + " is not an animation pattern");
                    return;
                }

                ap = foundAP;
            }
        }

        void Update()
        {
            ui.Update();
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

            public UIDynamicTextField CreateTextfield(string text, float width = 300, float height = 300)
            {
                Transform textField = GameObject.Instantiate<Transform>(plugin.manager.configurableTextFieldPrefab);
                ConfigureTransform(textField, width, height);

                UIDynamicTextField textfieldDynamic = textField.GetComponent<UIDynamicTextField>();
                textfieldDynamic.text = text;
                return textfieldDynamic;
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

        private float Remap(float x, float x1, float x2, float y1, float y2)
        {
            var m = (y2 - y1) / (x2 - x1);
            var c = y1 - m * x1;

            return m * x + c;
        }
    }
}
