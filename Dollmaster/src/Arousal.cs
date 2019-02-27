using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.Dollmaster
{
    public class Arousal : BaseModule
    {
        JSONStorableBool arousalEnabled;
        JSONStorableFloat arousalValue;
        JSONStorableFloat arousalRate;
        JSONStorableFloat arousalDecay;
        JSONStorableFloat arousalDelay;
        JSONStorableFloat timeToDecay;

        public const float SLIDER_MAX = 100;

        float arousalTarget = 0;
        float lastArousalTime = 0;
        float colorCycle = 0;

        Image sliderBGImage;

        public Arousal(DollmasterPlugin dm) : base(dm)
        {
            this.dm = dm;

            arousalEnabled = new JSONStorableBool("arousalEnabled", true);
            dm.RegisterBool(arousalEnabled);
            UIDynamicToggle moduleEnableToggle = dm.CreateToggle(arousalEnabled);
            moduleEnableToggle.label = "Enable Arousal";
            moduleEnableToggle.backgroundColor = Color.green;

            arousalValue = new JSONStorableFloat("arousal", 0, 0, SLIDER_MAX, false, false);
            dm.RegisterFloat(arousalValue);

            UIDynamicSlider arousalSlider = dm.ui.CreateSlider("❤", 300, 120);
            //arousalSlider.valueFormat = "n0";
            arousalValue.slider = arousalSlider.slider;
            sliderBGImage = arousalSlider.GetComponentInChildren<Image>();
            sliderBGImage.color = new Color(0.2f, 0.2f, 0.2f, 1.0f);
            arousalSlider.labelText.color = new Color(1, 1, 1);

            arousalRate = new JSONStorableFloat("arousalRate", 1, 0.01f, 10, false, true);
            dm.RegisterFloat(arousalRate);
            dm.CreateSlider(arousalRate);

            arousalDecay = new JSONStorableFloat("arousalDecay", 0.3f, 0.01f, 10, false, true);
            dm.RegisterFloat(arousalDecay);
            dm.CreateSlider(arousalDecay);

            arousalDelay = new JSONStorableFloat("arousalDelay", 0.25f, 0.01f, 5, false, true);
            dm.RegisterFloat(arousalDelay);
            dm.CreateSlider(arousalDelay);

            timeToDecay = new JSONStorableFloat("timeToDecay", 2, 0, 60, false, true);
            dm.RegisterFloat(timeToDecay);
            dm.CreateSlider(timeToDecay);

            dm.CreateSpacer();
        }

        public override void Update()
        {
            if (arousalEnabled.val == false)
            {
                return;
            }

            if ((Time.time - lastArousalTime) > timeToDecay.val)
            {
                Decay();
            }

            arousalValue.val += (arousalTarget - arousalValue.val) * Time.deltaTime * 2.0f;

            colorCycle += Time.deltaTime * arousalValue.val * 0.1f;

            float intensity = Mathf.Abs(Mathf.Sin(colorCycle));
            Color color = sliderBGImage.color;
            color.r = intensity * arousalValue.val / 100.0f;
            color.r = Mathf.Clamp01(color.r) * 0.7f + 0.3f;
            sliderBGImage.color = color;
        }

        void Decay()
        {
            arousalTarget -= arousalDecay.val;
            arousalTarget = Mathf.Clamp(arousalTarget, 0, SLIDER_MAX);
        }

        public void Trigger()
        {
            if((Time.time - lastArousalTime) > arousalDelay.val)
            {
                arousalTarget += arousalRate.val;
                arousalTarget = Mathf.Clamp(arousalTarget, 0, SLIDER_MAX);
                lastArousalTime = Time.time;
            }
        }

        public void MaxOut()
        {
            dm.climaxController.isClimaxing = false;
            dm.climaxController.isResting = false;
            arousalValue.val = arousalTarget = arousalValue.max;
        }

        public float value
        {
            get
            {
                return arousalValue.val;
            }
        }
    }
}
