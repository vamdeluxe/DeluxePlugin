﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.Dollmaster
{
    public class ExpressionController : BaseModule
    {
        JSONStorableBool morphsEnabled;
        JSONStorableBool voiceEnabled;
        JSONStorableFloat morphPercent;

        GenerateDAZMorphsControlUI morphControl;

        float nextTriggerAvailableTime = 0;

        Dictionary<DAZMorph, float> morphTargets = new Dictionary<DAZMorph, float>();

        JSONNode currentMouthOpened;
        JSONNode currentMouthClosed;
        JSONNode currentEyes;
        JSONNode currentIdle;

        DAZMorph mouthOpenMorph;
        DAZMorph tongueRaiseMorph;

        DAZBone upperJaw;
        DAZBone lowerJaw;

        JSONStorableFloat interpolationSpeed;
        float mouthOpenness = 0;

        NamedAudioClip lastPlayedClip;

        List<DAZMorph> animatedMorphs;

        public ExpressionController(DollmasterPlugin dm) : base(dm)
        {
            morphsEnabled = new JSONStorableBool("morphsEnabled", true, (bool enabled)=>
            {
                if (enabled == false)
                {
                    ZeroTargets();
                }
            });
            dm.RegisterBool(morphsEnabled);
            UIDynamicToggle morphsEnabledToggle = dm.CreateToggle(morphsEnabled);
            morphsEnabledToggle.label = "Enable Facial Morphs";
            morphsEnabledToggle.backgroundColor = Color.green;

            voiceEnabled = new JSONStorableBool("voiceEnabled", true);
            dm.RegisterBool(voiceEnabled);
            UIDynamicToggle voiceEnabledToggle = dm.CreateToggle(voiceEnabled);
            voiceEnabledToggle.label = "Enable Voice";
            voiceEnabledToggle.backgroundColor = Color.green;

            morphPercent = new JSONStorableFloat("morph percent", 0.7f, 0, 1, true);
            dm.RegisterFloat(morphPercent);
            UIDynamicSlider morphPercentSlider = dm.CreateSlider(morphPercent);

            JSONStorable geometry = atom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            morphControl = character.morphsControlUI;

            mouthOpenMorph = morphControl.GetMorphByDisplayName("Mouth Open Wide");
            tongueRaiseMorph = morphControl.GetMorphByDisplayName("Tongue Raise-Lower");

            interpolationSpeed = new JSONStorableFloat("expression speed", 8f, 0.1f, 20.0f, true);
            dm.RegisterFloat(interpolationSpeed);
            dm.CreateSlider(interpolationSpeed);

            upperJaw = atom.GetStorableByID("upperJaw") as DAZBone;
            lowerJaw = atom.GetStorableByID("lowerJaw") as DAZBone;

            //  Hack to fix bad tongue morph values.
            //morphControl.GetMorphByDisplayName("Tongue In-Out").startValue = 1.0f;

            dm.CreateSpacer();

            // Prevent mouth from getting too big.
            dm.headAudioSource.SetFloatParamValue("volumeTriggerMultiplier", 0);
        }

        public override void Update()
        {
            base.Update();

            bool shouldBlink = currentIdle == null && currentEyes == null;
            atom.GetStorableByID("EyelidControl").SetBoolParamValue("blinkEnabled", shouldBlink);

            if (dm.expressionController.morphsEnabled.val)
            {
                ZeroTargets();
            }

            if (dm.arousal.value == 0)
            {
                //ClearExpressions();
            }

            float mouthOpennessTarget = dm.headAudioSource.clipLoudness * 100.0f;

            float mouthOpenDelta = (mouthOpennessTarget - mouthOpenness);
            float mouthOpenRate = mouthOpenDelta > 0 ? 40 : 2f;
            mouthOpenness += mouthOpenDelta * Time.deltaTime * mouthOpenRate;
            SetExpression(currentMouthClosed, 1.0f - mouthOpenness);
            SetExpression(currentMouthOpened, mouthOpenness * 0.4f);
            SetExpression(currentEyes);

            tongueRaiseMorph.SetValue(mouthOpenness * 0.04f);

            if (dm.kissController.isKissing)
            {
                SetExpression(currentIdle);
            }
            if (dm.climaxController.isClimaxing)
            {
                SetExpression(currentIdle);

                float mouthOpenTarget = Mathf.Clamp(dm.headAudioSource.smoothedClipLoudness * 1.0f, 0, 1.0f);

                if (dm.expressionController.morphsEnabled.val)
                {
                    mouthOpenMorph.SetValue(mouthOpenTarget);
                }
                dm.headAudioSource.volumeTriggerQuickness = 20.0f;
            }
            else
            if (dm.arousal.value <= 0.1f && dm.thrustController.sliderValue < 10)
            {
                ClearExpressions();
                TriggerSelectIdle();
                SetExpression(currentIdle);
            }

            if (dm.breathController.isBreathingWithMouth)
            {
                float mouthOpenTarget = Mathf.Clamp(0.4f + dm.headAudioSource.smoothedClipLoudness * 3.0f, 0, 1.0f);
                if (dm.expressionController.morphsEnabled.val)
                {
                    mouthOpenMorph.SetValue(mouthOpenTarget);
                }
            }

            //Debug.Log(lowerJaw.transform.localEulerAngles.x + " " + upperJaw.transform.localEulerAngles.x);



            if (morphsEnabled.val == false)
            {
                ClearExpressions();
            }

            if (dm.expressionController.morphsEnabled.val)
            {
                TweenMorphTargets();
            }
        }

        public void Trigger()
        {
            if(Time.time >= nextTriggerAvailableTime)
            {
                StartExpression();
            }
        }

        public void StartExpression()
        {
            if (dm == null)
            {
                return;
            }

            if(dm.personality == null)
            {
                return;
            }

            if (dm.expressions == null)
            {
                return;
            }

            NamedAudioClip clip = dm.personality.GetTriggeredAudioClip(dm.arousal);
            if (clip == null || clip.sourceClip == null || clip == lastPlayedClip)
            {
                return;
            }

            //Debug.Log("playing " + clip.displayName);

            currentIdle = null;

            if (voiceEnabled.val)
            {
                dm.PlayAudio(clip);
                lastPlayedClip = clip;
            }

            nextTriggerAvailableTime = Time.time + clip.sourceClip.length + UnityEngine.Random.Range(0.5f, 3.0f);

            if (UnityEngine.Random.Range(0, 100) > 10)
            {
                currentMouthOpened = dm.expressions.mouthOpenGroup.SelectRandomExpression();
            }

            if (UnityEngine.Random.Range(0, 100) > 40)
            {
                currentMouthClosed = dm.expressions.mouthClosedGroup.SelectRandomExpression();
            }

            if (UnityEngine.Random.Range(0, 100) > 60)
            {
                currentEyes = dm.expressions.eyesGroup.SelectRandomExpression();
            }
        }

        public void TriggerSelectKiss()
        {
            currentIdle = dm.expressions.kissGroup.SelectRandomExpression();
            nextTriggerAvailableTime = Time.time + 5.0f;
        }

        void TriggerSelectIdle()
        {
            if(Time.time < nextTriggerAvailableTime)
            {
                return;
            }

            if (UnityEngine.Random.Range(0, 100) > 20)
            {
                currentIdle = dm.expressions.idleGroup.SelectRandomExpression();
                nextTriggerAvailableTime = Time.time + 5.0f;
            }
            else
            {
                currentIdle = null;
            }

        }

        void ClearExpressions()
        {
            currentMouthClosed = null;
            currentMouthOpened = null;
            currentEyes = null;
            currentIdle = null;
        }

        void ZeroTargets()
        {
            morphTargets.Keys.ToList().ForEach((morph) =>
            {
                morphTargets[morph] = morph.startValue;
            });
        }

        public static void ZeroPoseMorphs(Atom atom)
        {
            JSONStorable geometry = atom.GetStorableByID("geometry");
            if (geometry == null)
            {
                return;
            }

            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            var morphControl = character.morphsControlUI;

            morphControl.GetMorphDisplayNames().ToList().ForEach(name =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                if (morph == null)
                {
                    return;
                }
                if (morph.region == null)
                {
                    return;
                }
                if (morph.isPoseControl || morph.region.Contains("Expression"))
                {
                    morph.SetValue(morph.startValue);
                }
            });
        }

        void SetExpression(JSONNode node, float alpha = 1.0f)
        {
            if (node == null)
            {
                return;
            }

            alpha = Mathf.Clamp01(alpha);

            JSONArray morphs = node["morphs"].AsArray;
            for (int i = 0; i < morphs.Count; i++)
            {
                JSONClass morphNode = morphs[i].AsObject;
                string name = morphNode["name"].Value;
                float value = morphNode["value"].AsFloat * alpha;
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                if (morph == null)
                {
                    continue;
                }

                if (morph.isPoseControl || morph.region.Contains("Expressions"))
                {
                    morphTargets[morph] = value * morphPercent.val;
                    animatedMorphs = morphTargets.Keys.ToList();
                }
            }
        }

        //  This function is very performance sensitive.
        void TweenMorphTargets()
        {
            if (animatedMorphs == null)
            {
                return;
            }

            animatedMorphs.ForEach((morph) =>
            {
                float target = morphTargets[morph];

                target = Mathf.Clamp(target, -target, target);

                float current = morph.morphValue;

                if (Mathf.Abs(target - current)<0.01f)
                {
                    return;
                }

                float next = current + (target - current) * interpolationSpeed.val * Time.deltaTime;

                if ( Mathf.Abs(next - current)<0.01f )
                {
                    morph.SetValue(target);
                    return;
                }
                morph.SetValue(next);
            });
        }

        public void TriggerClimax()
        {
            if (dm.personality == null)
            {
                return;
            }

            if (dm.expressions == null)
            {
                return;
            }

            NamedAudioClip clip = dm.personality.GetRandomClimaxClip();
            if (clip == null || clip.sourceClip == null)
            {
                return;
            }

            dm.PlayAudio(clip);
            lastPlayedClip = clip;

            dm.climaxController.SetClimaxDuration(clip.sourceClip.length);

            currentIdle = dm.expressions.climaxGroup.SelectRandomExpression();
            currentMouthOpened = dm.expressions.mouthOpenGroup.SelectRandomExpression();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            ZeroTargets();
        }
    }
}
