using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin
{
    public class DollmasterUI : MVRScript
    {
        JSONStorableFloat intensityStorable;
        Image image;
        Transform meterTransform;

        public override void Init()
        {
            try
            {
                List<string> peopleIds = SuperController.singleton.GetAtoms()
                .Where(atom => atom.GetStorableByID("geometry") != null && (atom != containingAtom))
                .Select(atom => atom.name).ToList();

                JSONStorableStringChooser receiverChoiceJSON = new JSONStorableStringChooser("target", peopleIds, peopleIds.Count > 0 ? peopleIds[0] : null, "Thrust Target", delegate (string receiverName)
                {
                    Atom receiver = GetAtomById(receiverName);
                    SetupDollControl(receiver);
                });
                receiverChoiceJSON.storeType = JSONStorableParam.StoreType.Full;
                UIDynamicPopup dp = CreateScrollablePopup(receiverChoiceJSON, false);
                RegisterStringChooser(receiverChoiceJSON);

                if (peopleIds.Count > 0)
                {
                    SetupDollControl(GetAtomById(peopleIds[0]));
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        public void SetupDollControl(Atom atom)
        {
            meterTransform = containingAtom.GetComponentsInChildren<Transform>().ToList().First((s) => s.name == "Orgasm Meter");

            JSONStorable ometerStorable = atom.GetStorableByID( atom.GetStorableIDs().ToList().First((s)=>s.Contains("OMeter")) );
            intensityStorable = ometerStorable.GetFloatJSONParam("<3");
        }

        void Update()
        {
            try
            {
                if (intensityStorable != null && meterTransform !=null)
                {

                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

    }
}
