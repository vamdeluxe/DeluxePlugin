using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin
{
    public class ChanceToTrigger : MVRScript
    {
        JSONStorableAction redirectTrigger;
        JSONStorableFloat chance;
        JSONStorableFloat delayBetweenTrigger;

        float lastTriggerTime = 0;

        protected void SyncAtomChocies()
        {
            List<string> atomChoices = new List<string>();
            atomChoices.Add("None");
            foreach (string atomUID in SuperController.singleton.GetAtomUIDs())
            {
                atomChoices.Add(atomUID);
            }
            atomJSON.choices = atomChoices;
        }

        // receiver Atom
        protected Atom receivingAtom;
        protected void SyncAtom(string atomUID)
        {
            List<string> receiverChoices = new List<string>();
            receiverChoices.Add("None");
            if (atomUID != null)
            {
                receivingAtom = SuperController.singleton.GetAtomByUid(atomUID);
                if (receivingAtom != null)
                {
                    foreach (string receiverChoice in receivingAtom.GetStorableIDs())
                    {
                        receiverChoices.Add(receiverChoice);
                        //SuperController.LogMessage("Found receiver " + receiverChoice);
                    }
                }
            }
            else
            {
                receivingAtom = null;
            }
            receiverJSON.choices = receiverChoices;
            receiverJSON.val = "None";
        }
        protected JSONStorableStringChooser atomJSON;

        protected string _missingReceiverStoreId = "";
        protected void CheckMissingReceiver()
        {
            if (_missingReceiverStoreId != "" && receivingAtom != null)
            {
                JSONStorable missingReceiver = receivingAtom.GetStorableByID(_missingReceiverStoreId);
                if (missingReceiver != null)
                {
                    //Debug.Log("Found late-loading receiver " + _missingReceiverStoreId);
                    string saveTargetName = _receiverTargetName;
                    SyncReceiver(_missingReceiverStoreId);
                    _missingReceiverStoreId = "";
                    insideRestore = true;
                    receiverTargetJSON.val = saveTargetName;
                    insideRestore = false;
                }
            }
        }

        // receiver JSONStorable
        protected JSONStorable receiver;
        protected void SyncReceiver(string receiverID)
        {
            List<string> receiverTargetChoices = new List<string>();
            receiverTargetChoices.Add("None");
            if (receivingAtom != null && receiverID != null)
            {
                receiver = receivingAtom.GetStorableByID(receiverID);
                if (receiver != null)
                {
                    foreach (string actionParams in receiver.GetActionNames())
                    {
                        receiverTargetChoices.Add(actionParams);
                    }
                }
                else if (receiverID != "None")
                {
                    // some storables can be late loaded, like skin, clothing, hair, etc so must keep track of missing receiver
                    //Debug.Log("Missing receiver " + receiverID);
                    _missingReceiverStoreId = receiverID;
                }
            }
            else
            {
                receiver = null;
            }
            receiverTargetJSON.choices = receiverTargetChoices;
            receiverTargetJSON.val = "None";
        }
        protected JSONStorableStringChooser receiverJSON;

        // receiver target parameter
        protected string _receiverTargetName;
        protected void SyncReceiverTarget(string receiverTargetName)
        {
            _receiverTargetName = receiverTargetName;
            if (receiver != null && receiverTargetName != null)
            {
            }
        }
        protected JSONStorableStringChooser receiverTargetJSON;


        public override void Init()
        {
            try
            {
                // make atom selector
                atomJSON = new JSONStorableStringChooser("atom", SuperController.singleton.GetAtomUIDs(), null, "Atom", SyncAtom);
                RegisterStringChooser(atomJSON);
                SyncAtomChocies();
                UIDynamicPopup dp = CreateScrollablePopup(atomJSON);
                dp.popupPanelHeight = 1100f;
                // want to always resync the atom choices on opening popup since atoms can be added/removed
                dp.popup.onOpenPopupHandlers += SyncAtomChocies;

                // make receiver selector
                receiverJSON = new JSONStorableStringChooser("receiver", null, null, "Receiver", SyncReceiver);
                RegisterStringChooser(receiverJSON);
                dp = CreateScrollablePopup(receiverJSON);
                dp.popupPanelHeight = 960f;

                // make receiver target selector
                receiverTargetJSON = new JSONStorableStringChooser("receiverTarget", null, null, "Target", SyncReceiverTarget);
                RegisterStringChooser(receiverTargetJSON);
                dp = CreateScrollablePopup(receiverTargetJSON);
                dp.popupPanelHeight = 820f;

                // set atom to current atom to initialize
                atomJSON.val = containingAtom.uid;

                redirectTrigger = new JSONStorableAction("Redirect Action", ()=>
                {

                    float durationSinceLastTrigger = Time.time - lastTriggerTime;
                    if(durationSinceLastTrigger < delayBetweenTrigger.val)
                    {
                        return;
                    }

                    if (UnityEngine.Random.Range(0,100)< chance.val)
                    {
                        receiver.CallAction(_receiverTargetName);
                        lastTriggerTime = Time.time;
                    }
                });
                RegisterAction(redirectTrigger);

                chance = new JSONStorableFloat("chance", 50, 0, 100, true, true);
                RegisterFloat(chance);
                CreateSlider(chance, true);

                delayBetweenTrigger = new JSONStorableFloat("delay", 0, 0, 10, false, true);
                RegisterFloat(delayBetweenTrigger);
                CreateSlider(delayBetweenTrigger, true);


            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        protected void Update()
        {
            try
            {
                // check for receivers that might have been missing on load due to asynchronous load of some assets like skin, clothing, hair
                CheckMissingReceiver();
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }


    }
}