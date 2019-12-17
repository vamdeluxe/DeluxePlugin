using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.Dollmaster
{
    public class BreathController : BaseModule
    {
        JSONStorableBool breathEnabled;

        FreeControllerV3 chestControl;

        const float maxBPM = 160f;
        const float minBPM = 20f;

        public float breathesPerMinute = minBPM;

        float cycle = 0.0f;

        NamedAudioClip breathingMouth;
        NamedAudioClip breathingNose;
        NamedAudioClip breathingPanting;

        bool breatheWithMouth = false;

        public BreathController(DollmasterPlugin dm) : base(dm)
        {
            chestControl = atom.GetStorableByID("chestControl") as FreeControllerV3;

            string SFX_PATH = DollmasterPlugin.ASSETS_PATH + "/SFX";
            breathingMouth = DollmasterPlugin.LoadAudio(SFX_PATH + "/breathing_mouth.wav");
            breathingNose = DollmasterPlugin.LoadAudio(SFX_PATH + "/breathing_nose.wav");
            breathingPanting = DollmasterPlugin.LoadAudio(SFX_PATH + "/breathing_panting.mp3");

            breathEnabled = new JSONStorableBool("breathEnabled", true);
            dm.RegisterBool(breathEnabled);
            UIDynamicToggle moduleEnableToggle = dm.CreateToggle(breathEnabled);
            moduleEnableToggle.label = "Enable Breathing";
            moduleEnableToggle.backgroundColor = Color.green;

            dm.CreateSpacer();
        }

        public override void Update()
        {
            base.Update();

            if (breathEnabled.val == false)
            {
                return;
            }

            if (dm.arousal.value > 1)
            {
                breathesPerMinute += 2.0f * Time.deltaTime;
            }
            else
            {
                breathesPerMinute -= 10.0f * Time.deltaTime;
            }

            breathesPerMinute = Mathf.Clamp(breathesPerMinute, minBPM, maxBPM);
            float amount = 6.0f;

            float oneBreathePerMinute = Time.deltaTime * Mathf.PI * 2.0f / 60.0f;
            cycle += oneBreathePerMinute * breathesPerMinute;

            chestControl.jointRotationDriveXTarget = Mathf.Sin(cycle * 0.5f) * amount;

            if (dm.kissController.isKissing)
            {
                return;
            }

            if (dm.headAudioSource.playingClip == null)
            {
                if (breathesPerMinute > 100 && dm.thrustController.slider.slider.value > 2 && dm.arousal.value > 20)
                {
                    NamedAudioClip pantingClip = dm.personality.GetRandomPantingClip();
                    if(pantingClip == null)
                    {
                        pantingClip = breathingPanting;
                    }
                    if (pantingClip == null)
                    {
                        return;
                    }
                    dm.headAudioSource.PlayIfClear(pantingClip);
                    breatheWithMouth = true;
                }
                else
                if(breathesPerMinute > 60)
                {
                    if (dm.thrustController.sliderValue <= 0 && breathingMouth != null)
                    {
                        dm.headAudioSource.PlayIfClear(breathingMouth);
                    }
                    breatheWithMouth = true;
                }
                else
                {
                    if ( dm.thrustController.sliderValue <= 0 && breathingNose != null)
                    {
                        dm.headAudioSource.PlayIfClear(breathingNose);
                    }
                    breatheWithMouth = false;
                }
            }

        }

        public bool isBreathingWithMouth
        {
            get
            {
                return breathesPerMinute > 60 || breatheWithMouth;
            }
        }
    }
}
