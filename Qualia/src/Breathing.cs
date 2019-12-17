using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Qualia
{
    public class Breathing : MVRScript
    {
        JSONStorableFloat speed = new JSONStorableFloat("Speed", 1.2f, 0, 10, false);

        DAZMorph breathingMorph;
        DAZMorph throatMorph;
        FreeControllerV3 chestControl;
        FreeControllerV3 headControl;

        public override void Init()
        {
            JSONStorable geometry = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            GenerateDAZMorphsControlUI  morphControl = character.morphsControlUI;
            breathingMorph = morphControl.GetMorphByDisplayName("Breath1");
            throatMorph = morphControl.GetMorphByDisplayName("deepthroat");

            chestControl = containingAtom.GetStorableByID("chestControl") as FreeControllerV3;
            headControl = containingAtom.GetStorableByID("headControl") as FreeControllerV3;

            RegisterFloat(speed);
            CreateSlider(speed);
        }

        void Update()
        {
            if (breathingMorph == null)
            {
                return;
            }

            float cycle = Mathf.Sin(Time.time * speed.val);
            cycle = cycle * cycle * 1.2f;

            // Expands chest when breathing.
            float chestMorphValue = cycle;
            breathingMorph.morphValue += (chestMorphValue - breathingMorph.morphValue) * Time.deltaTime * 100.0f;

            // Produces up/down movement for chest when breathing.
            float chestJointDriveTarget = -cycle * 4;
            chestControl.jointRotationDriveXTarget = chestJointDriveTarget;

            // Opens/closes throat when breathing.
            float throatMorphValue = -cycle * 0.2f;
            throatMorph.morphValue += (throatMorphValue - throatMorph.morphValue) * Time.deltaTime * 100.0f;

            // Relax/tense chest when breathing. Creates subtle idle motion in upper body due to physics.
            float chestMuscleNoise = Mathf.PerlinNoise(Time.time, 1000) * 600;
            float chestSpringValue = 1500 - cycle * 1000 + chestMuscleNoise;
            chestControl.RBHoldPositionSpring = chestSpringValue;

            float neckMuscleNoise = Mathf.PerlinNoise(Time.time, 4000);
            headControl.RBHoldPositionSpring = 1200 - cycle * 300 + neckMuscleNoise * 300;
            headControl.RBHoldRotationSpring = 200 - cycle * 50 + neckMuscleNoise * 50;

        }
    }
}
