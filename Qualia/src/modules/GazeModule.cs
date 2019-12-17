using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Qualia.Modules
{
    public class GazeModule : IQualiaModule
    {
        FreeControllerV3 eyeControl;
        FreeControllerV3 headControl;

        List<Transform> targets = new List<Transform>();
        Vector3 eyeTarget;
        Transform focusTargetTransform;
        Vector3 headTarget;
        Vector3 headHybridUp;
        float nextTargetSwitchTime = Time.time;
        float headInterpolatedRotationSpeed = 0.0f;
        float headRotationSpeed = 0;

        float nextSaccadeTime = Time.time;
        Vector3 saccadeOffset = new Vector3();

        DAZBone neckBone;
        DAZBone hipBone;

        public void Init(SharedBehaviors plugin)
        {
            var containingAtom = plugin.containingAtom;

            eyeControl = containingAtom.GetStorableByID("eyeTargetControl") as FreeControllerV3;
            headControl = containingAtom.GetStorableByID("headControl") as FreeControllerV3;

            var playerCamera = CameraTarget.centerTarget.transform;

            var playerTarget = new GameObject().transform;
            playerTarget.SetParent(playerCamera, false);
            playerTarget.localPosition = new Vector3(0, -0.05f, 0);
            targets.Add(playerTarget);

            var belowPlayerTarget = new GameObject().transform;
            belowPlayerTarget.SetParent(playerCamera, false);
            belowPlayerTarget.localPosition = new Vector3(0, -0.3f, 0);
            targets.Add(belowPlayerTarget);

            var abovePlayerTarget = new GameObject().transform;
            abovePlayerTarget.SetParent(playerCamera, false);
            abovePlayerTarget.localPosition = new Vector3(0, 0.05f, 0);
            targets.Add(abovePlayerTarget);

            var straightAheadTarget = new GameObject().transform;
            straightAheadTarget.SetParent(containingAtom.GetStorableByID("head").transform, false);
            straightAheadTarget.localPosition = new Vector3(0, -0.1f, 10);
            targets.Add(straightAheadTarget);

            var leftOfPersonTarget = new GameObject().transform;
            leftOfPersonTarget.SetParent(containingAtom.GetStorableByID("head").transform, false);
            leftOfPersonTarget.localPosition = new Vector3(-10, 0, 0);
            targets.Add(leftOfPersonTarget);

            var rightOfPersonTarget = new GameObject().transform;
            rightOfPersonTarget.SetParent(containingAtom.GetStorableByID("head").transform, false);
            rightOfPersonTarget.localPosition = new Vector3(10, 0, 0);
            targets.Add(rightOfPersonTarget);

            focusTargetTransform = playerTarget;
            eyeTarget = playerTarget.position;

            headTarget = playerTarget.position;

            neckBone = containingAtom.GetStorableByID("neck") as DAZBone;
            hipBone = containingAtom.GetStorableByID("hip") as DAZBone;
        }

        public void Update(SharedBehaviors plugin)
        {
            float behindAmount = 1f;
            Vector3 eyeDelta = eyeTarget - headControl.transform.position;
            Vector3 target = CameraTarget.centerTarget.transform.position + eyeDelta.normalized * behindAmount;

            if (Time.time >= nextSaccadeTime)
            {
                saccadeOffset = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(0.05f, 0.2f);
                nextSaccadeTime = Time.time + UnityEngine.Random.Range(0.5f, 1.5f);
            }

            eyeControl.transform.position = eyeTarget + saccadeOffset;

            // Hide the icon so it doesn't get in your face in VR.
            eyeControl.hidden = true;

            // If the target position deviates too much, reduce target switch time.
            float eyeToTargetDelta = (focusTargetTransform.position - eyeTarget).magnitude;
            if (eyeToTargetDelta > 0.2f && UnityEngine.Random.Range(0, 100) > 60)
            {
                nextTargetSwitchTime -= 0.1f;
            }

            if (Time.time > nextTargetSwitchTime)
            {
                nextTargetSwitchTime = Time.time + UnityEngine.Random.Range(3.0f, 5.0f);

                int selectedIndex = UnityEngine.Random.Range(0, targets.Count);

                // Also if we switched targets due to target delta, select player as target.
                if (eyeToTargetDelta > 0.2f && UnityEngine.Random.Range(0, 100) > 20)
                {
                    selectedIndex = 0;
                    nextTargetSwitchTime += UnityEngine.Random.Range(2.0f, 5.0f);
                }

                focusTargetTransform = targets[selectedIndex];

                // Have a change to look off to the side.
                // Never do this for player target though.
                if (UnityEngine.Random.Range(0, 100) > 70 && selectedIndex > 0)
                {
                    Vector3 tp = focusTargetTransform.localPosition;
                    tp = new Vector3(UnityEngine.Random.Range(-.4f, .4f), tp.y, tp.z);
                    focusTargetTransform.localPosition = tp;
                }

                eyeTarget = focusTargetTransform.position;

                // Cause a near-immediate blink when switching targets.
                // Have this blink be slightly longer than usual.
                plugin.BlinkModule.NextBlinkTime -= 2.0f;
                plugin.BlinkModule.NextBlinkDurationMultiplier = 1.5f;

                // Rapid eye movement when switching targets.
                // Only do this when not looking at player.
                if (selectedIndex != 0)
                {
                    nextSaccadeTime -= 1.0f;
                }

                // Some chance of matching head target to eye target.
                if(UnityEngine.Random.Range(0,100) > 40)
                {
                    headTarget = eyeTarget;

                    // Some random chance to dart head nearby target instead of head-on.
                    Vector3 squashed = new Vector3(1, 0.01f, 1);
                    Vector3 headTargetOffset = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(0.01f, 0.05f);
                    headTargetOffset = new Vector3(headTargetOffset.x * squashed.x, headTargetOffset.y * squashed.y, headTargetOffset.z * squashed.z);
                    headTarget += headTargetOffset;

                    // Always point head down a bit.
                    headTarget += new Vector3(0, -.15f, 0);

                    // Some random chance of rotating slowly to target.
                    if (UnityEngine.Random.Range(0, 100) > 60)
                    {
                        headInterpolatedRotationSpeed *= 0.3f;
                    }

                    headInterpolatedRotationSpeed = UnityEngine.Random.Range(0.8f, 1.0f);

                    // Combine hip, neck, and camera up to achieve a more natural up vector when turning.
                    // Only update up vectors when switching targets.
                    Vector3 hipToNeck = Vector3.Normalize(neckBone.transform.position - hipBone.transform.position);
                    Vector3 cameraUp = CameraTarget.centerTarget.transform.up;
                    headHybridUp = Vector3.Lerp(cameraUp, hipToNeck, 0.5f);
                }
            }

            // Lerp head to target.
            Vector3 headDelta = headTarget - headControl.transform.position;
            Quaternion targetHeadRotation = Quaternion.LookRotation(headDelta, headHybridUp);
            float headRotationDelta = Quaternion.Angle(headControl.transform.rotation, targetHeadRotation);

            // Linear rotate towards (using delta so it's kind of a lerp...).
            float rotationLerpSpeed = ((headRotationDelta * 4.0f + 10) + Mathf.PerlinNoise(Time.time * 2.0f, 300) * 0.5f) * headInterpolatedRotationSpeed;
            float rotationLerp = Time.deltaTime * rotationLerpSpeed;

            // Interpolate the interpolation...
            headRotationSpeed += rotationLerp * 0.4f;
            headRotationSpeed *= Time.deltaTime * 60;

            headControl.transform.rotation = Quaternion.RotateTowards(headControl.transform.rotation, targetHeadRotation, headRotationSpeed);

        }

        public void Destroy(SharedBehaviors plugin)
        {
            foreach (var transform in targets)
            {
                GameObject.Destroy(transform.gameObject);
            }
        }
    }
}
