using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Qualia.Modules
{
    public class BlinkModule : IQualiaModule
    {
        DAZMorph blinkMorph;
        DAZMorph leftSquintMorph;
        DAZMorph rightSquintMorph;

        bool blink = false;
        float lastBlinkTime = Time.time;
        public float NextBlinkTime = Time.time;
        public float NextBlinkDurationMultiplier = 1.0f;

        Keyframe[] blinkKeyframes = new Keyframe[] {
            new Keyframe(0, 0),
            new Keyframe(0.1f, 1),
            new Keyframe(1.5f, 1),
            new Keyframe(1.6f, 0),
        };
        AnimationCurve blinkCurve;

        public void Init(SharedBehaviors plugin)
        {
            JSONStorable geometry = plugin.containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;
            blinkMorph = morphControl.GetMorphByDisplayName("Eyes Closed");

            blinkCurve = new AnimationCurve(blinkKeyframes);

            leftSquintMorph = morphControl.GetMorphByDisplayName("Eyes Squint Left");
            rightSquintMorph = morphControl.GetMorphByDisplayName("Eyes Squint Right");
        }

        public void Destroy(SharedBehaviors plugin)
        {
        }

        public void Update(SharedBehaviors plugin)
        {
            // This causes squinting to increase frequency of blinks.
            float squintMaxValue = Mathf.Max(leftSquintMorph.morphValue, rightSquintMorph.morphValue);

            if (squintMaxValue > 0.5f && UnityEngine.Random.Range(0, 100) > 70)
            {
                NextBlinkTime -= 0.01f;
                NextBlinkDurationMultiplier = 0.85f;
            }

            if (Time.time >= NextBlinkTime)
            {
                lastBlinkTime = Time.time;

                float blinkFrequency = UnityEngine.Random.Range(2.0f, 5.0f);
                NextBlinkTime = Time.time + blinkFrequency;

                // Small chance for a double blink
                if (UnityEngine.Random.Range(0, 100) < 5)
                {
                    NextBlinkTime *= 0.2f;
                }

                float maxBlinkValue = 1.0f - squintMaxValue * 0.5f;
                float blinkSpeed = UnityEngine.Random.Range(0.3f, 0.5f);
                float totalBlinkTime = UnityEngine.Random.Range(0.17f, 0.22f) * NextBlinkDurationMultiplier;
                blinkCurve.keys = new Keyframe[] {
                    new Keyframe(0, 0, 0.1f, 0.3f),
                    new Keyframe(totalBlinkTime * blinkSpeed, maxBlinkValue, 0.2f, 0.8f),
                    new Keyframe(totalBlinkTime * (1.0f - blinkSpeed), maxBlinkValue, 0.2f, 0.8f),
                    new Keyframe(totalBlinkTime, 0, 0.2f, 0.5f),
                }; ;

                NextBlinkDurationMultiplier = 1.0f;
            }

            float timeInBlink = (Time.time - lastBlinkTime);
            float contributionFromSquint = squintMaxValue * 0.25f;
            float blinkValue = blinkCurve.Evaluate(timeInBlink) + contributionFromSquint;
            blinkMorph.morphValue = blinkValue;
        }
    }
}
