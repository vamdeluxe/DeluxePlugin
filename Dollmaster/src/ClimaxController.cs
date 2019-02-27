using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class ClimaxController : BaseModule
    {
        JSONStorableBool climaxEnabled;
        JSONStorableBool climaxHeadRestEnabled;
        JSONStorableBool climaxConvulsionEnabled;

        public bool isClimaxing = false;
        public bool isResting = false;
        float lastClimaxTime = 0;

        float climaxDuration = 8.0f;

        JSONStorableFloat restDuration;
        UIDynamicButton climaxButton;

        FreeControllerV3 headControl;
        float headHoldRotationTarget;
        float headHoldRotationRestore;

        Dictionary<FreeControllerV3, float> initialRotationSpring = new Dictionary<FreeControllerV3, float>();
        Dictionary<FreeControllerV3, float> initialPositionSpring = new Dictionary<FreeControllerV3, float>();

        float climaxRotationSpeed = 10.0f;
        float climaxRotationAmount = 600.0f;

        float climaxPositionSpeed = 5.0f;
        float climaxPositionAmount = 100.0f;

        public ClimaxController(DollmasterPlugin dm) : base(dm)
        {
            climaxEnabled = new JSONStorableBool("climaxEnabled", true);
            dm.RegisterBool(climaxEnabled);
            UIDynamicToggle moduleEnableToggle = dm.CreateToggle(climaxEnabled);
            moduleEnableToggle.label = "Enable Climax";
            moduleEnableToggle.backgroundColor = Color.green;

            climaxHeadRestEnabled = new JSONStorableBool("climaxHeadRestEnabled", true);
            dm.RegisterBool(climaxHeadRestEnabled);
            dm.CreateToggle(climaxHeadRestEnabled);

            climaxConvulsionEnabled = new JSONStorableBool("climaxConvulsionEnabled", true);
            dm.RegisterBool(climaxConvulsionEnabled);
            dm.CreateToggle(climaxConvulsionEnabled);

            restDuration = new JSONStorableFloat("restDuration", 5.0f, 0.1f, 60.0f, false);
            dm.RegisterFloat(restDuration);
            dm.CreateSlider(restDuration);

            climaxButton = dm.ui.CreateButton("Climax!", 300, 120);
            climaxButton.button.gameObject.SetActive(false);
            climaxButton.buttonText.fontSize = 48;
            UI.ColorButton(climaxButton, new Color(1, 1, 1), new Color(0.8f, 0.0f, 0.0f));

            climaxButton.button.onClick.AddListener(() =>
            {
                dm.arousal.MaxOut();
            });

            headControl = atom.GetStorableByID("headControl") as FreeControllerV3;
            headHoldRotationTarget = headControl.RBHoldRotationSpring;

            atom.freeControllers.ToList().ForEach((controller) =>
            {
                initialRotationSpring[controller] = controller.RBHoldRotationSpring;
                initialPositionSpring[controller] = controller.RBHoldPositionSpring;
            });

            dm.CreateSpacer();
        }

        public override void Update()
        {
            base.Update();

            if (climaxEnabled.val == false)
            {
                return;
            }

            if(isClimaxing == false && dm.arousal.value >= 95 && UnityEngine.Random.Range(0,100)>10)
            {
                isClimaxing = true;
                lastClimaxTime = Time.time;
                dm.TriggerClimax();
                OnClimaxStart();
            }

            float elapsedSinceClimax = Time.time - lastClimaxTime;
            if(isClimaxing && isResting==false && elapsedSinceClimax > climaxDuration)
            {
                isResting = true;
                isClimaxing = false;
            }

            if (isClimaxing == false && isResting && elapsedSinceClimax > (climaxDuration + restDuration.val))
            {
                isResting = false;
                OnClimaxEnd();
            }

            bool isClimaxButtonAvailable = isClimaxing == false && isResting == false && dm.arousal.value >= 80;
            climaxButton.button.gameObject.SetActive(isClimaxButtonAvailable);

            //Debug.Log(isClimaxing + " " + isResting);

            if (climaxHeadRestEnabled.val)
            {
                if (Mathf.Abs(headControl.RBHoldRotationSpring - headHoldRotationTarget) > 1)
                {
                    headControl.RBHoldRotationSpring += (headHoldRotationTarget - headControl.RBHoldRotationSpring) * Time.deltaTime * 2.0f;
                }
            }

            if (climaxConvulsionEnabled.val && isClimaxing)
            {
                int index = 0;
                initialRotationSpring.Keys.ToList().ForEach((controller) =>
                {
                    float alpha = climaxAlpha;

                    float initialRotationValue = initialRotationSpring[controller];

                    float initialPositionValue = initialPositionSpring[controller];

                    float rn = Mathf.PerlinNoise(Time.time * climaxRotationSpeed, index);

                    controller.RBHoldRotationSpring = initialRotationValue + rn * climaxPositionAmount * alpha;

                    float pn = Mathf.PerlinNoise(Time.time * climaxPositionSpeed, index + 1000);
                    controller.RBHoldPositionSpring = initialPositionValue + pn * climaxPositionAmount * alpha;

                    index += 100;
                });
            }
        }

        void OnClimaxStart()
        {
            headHoldRotationRestore = headControl.RBHoldRotationSpring;
            headHoldRotationTarget = 0;
        }

        void OnClimaxEnd()
        {
            headHoldRotationTarget = headHoldRotationRestore;
        }

        public void SetClimaxDuration(float duration)
        {
            climaxDuration = duration;
        }

        public float climaxAlpha
        {
            get
            {
                float elapsedSinceClimax = Time.time - lastClimaxTime;
                float alpha = Mathf.Clamp01(elapsedSinceClimax / (climaxDuration + restDuration.val * 0.25f));
                return alpha;
            }
        }
    }
}
