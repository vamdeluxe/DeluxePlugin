using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Qualia.Modules
{
    public class BodyLanguageModule : IQualiaModule
    {
        public JSONStorableFloat confidence = new JSONStorableFloat("Shy (-1) to Confident (1)", 0, -1, 1, true);
        AnimationCurve confidenceHeadTiltCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(-1, 40),
            new Keyframe(1, -8),
        });

        public JSONStorableFloat playfulness = new JSONStorableFloat("Stoic (0) to Playful (1)", 0, 0, 1, true);

        AnimationCurve playfulnessHeadTiltCurve = new AnimationCurve(new Keyframe[]{
                new Keyframe(0, 0, 10, 20),
                new Keyframe(1, 40, 30, 40),
        });

        FreeControllerV3 headControl;

        public void Init(SharedBehaviors plugin)
        {
            headControl = plugin.containingAtom.GetStorableByID("headControl") as FreeControllerV3;

            plugin.RegisterFloat(confidence);
            plugin.CreateSlider(confidence);

            plugin.RegisterFloat(playfulness);
            plugin.CreateSlider(playfulness);
        }

        public void Update(SharedBehaviors plugin)
        {
            Vector3 headEuler = headControl.transform.eulerAngles;
            headEuler.x = confidenceHeadTiltCurve.Evaluate(confidence.val);
            //headEuler.z = 0;
            //headEuler.z = playfulnessHeadTiltCurve.Evaluate(playfulness.val);
            //headControl.transform.eulerAngles = headEuler;
        }

        public void Destroy(SharedBehaviors plugin)
        {
        }
    }
}
