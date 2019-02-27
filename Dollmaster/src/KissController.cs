using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class KissController : BaseModule
    {
        JSONStorableBool kissEnabled;
        JSONStorableFloat kissDistance;

        float kissStartTime = 0;
        float durationBetweenKiss = 2.0f;
        float maxDotProductDelta = 0.35f;
        float kissDuration = 1.0f;

        FreeControllerV3 headController;
        DAZBone jawBone;
        float previousHoldSpring = 0;
        Vector3 kissStartPosition;
        Quaternion kissStartRotation;

        Transform kissTarget;
        DAZBone targetJaw;
        DAZBone targetHead;

        public bool isKissing = false;

        public KissController(DollmasterPlugin dm) : base(dm)
        {
            kissEnabled = new JSONStorableBool("kissEnabled", false);
            dm.RegisterBool(kissEnabled);
            UIDynamicToggle moduleEnableToggle = dm.CreateToggle(kissEnabled);
            moduleEnableToggle.label = "Enable Kiss (Experimental)";
            moduleEnableToggle.backgroundColor = Color.green;

            kissDistance = new JSONStorableFloat("kissDistance", 0.3f, 0.0f, 0.5f, true);
            dm.RegisterFloat(kissDistance);
            dm.CreateSlider(kissDistance);

            headController = atom.GetStorableByID("headControl") as FreeControllerV3;
            jawBone = atom.GetStorableByID("lowerJaw") as DAZBone;

            previousHoldSpring = headController.RBHoldPositionSpring;
        }

        public override void Update()
        {
            base.Update();

            if (kissEnabled.val == false)
            {
                return;
            }

            FreeControllerV3 nearestHead = FindNearestHeadToKiss();

            if(dm.climaxController.isClimaxing || dm.climaxController.isResting)
            {
                kissStartTime = Time.time;
                EndKiss();
                return;
            }

            if ((Time.time - kissStartTime) >= durationBetweenKiss && nearestHead!=null && isKissing==false)
            {
                kissStartTime = Time.time;
                targetJaw = nearestHead.containingAtom.GetStorableByID("lowerJaw") as DAZBone;
                targetHead = nearestHead.containingAtom.GetStorableByID("head") as DAZBone;
                BeginKiss(targetJaw.transform);
            }

            if((Time.time- kissStartTime)>= kissDuration && isKissing)
            {
                EndKiss();
            }

            if (isKissing && kissTarget!=null)
            {
                headController.transform.rotation = Quaternion.LookRotation(targetJaw.transform.position - jawBone.transform.position, headController.transform.up);
                Vector3 delta = targetJaw.transform.position - headController.transform.position;
                if (delta.magnitude >= 0.13f)
                {
                    headController.transform.position += (targetJaw.transform.position - headController.transform.position) * 1f * Time.deltaTime;
                }
            }
        }

        FreeControllerV3 FindNearestHeadToKiss()
        {
            float minDistance = 1000000;
            FreeControllerV3 nearestHead = null;

            SuperController.singleton.GetAtoms()
            .Where((otherAtom) =>
            {
                return otherAtom.GetStorableByID("headControl") != null && otherAtom!=atom;
            }).ToList()
            .Select((atom) =>
            {
                return (atom.GetStorableByID("headControl") as FreeControllerV3);
            }).ToList()
            .ForEach((controller) =>
            {
                float distance = (controller.transform.position - headController.transform.position).magnitude;
                if (distance > kissDistance.val)
                {
                    return;
                }

                if (distance > minDistance)
                {
                    return;
                }



                float dotProduct = Vector3.Dot(headController.transform.forward, controller.transform.forward);
                float dotDelta = 1.0f + dotProduct;
                if(dotDelta > maxDotProductDelta)
                {
                    return;
                }


                minDistance = distance;
                nearestHead = controller;
            });

            return nearestHead;
        }

        public void BeginKiss(Transform target)
        {
            isKissing = true;
            kissTarget = target;
            previousHoldSpring = headController.RBHoldPositionSpring;
            headController.RBHoldPositionSpring = 8000;
            kissStartPosition = headController.transform.position;
            kissStartRotation = headController.transform.rotation;
            dm.expressionController.TriggerSelectKiss();

            dm.arousal.Trigger();
        }

        public void EndKiss()
        {
            headController.RBHoldPositionSpring = previousHoldSpring;
            kissTarget = null;
            isKissing = false;
        }
    }
}
