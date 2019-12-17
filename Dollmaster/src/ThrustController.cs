﻿using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin.Dollmaster
{
    public class ThrustController : BaseModule
    {
        JSONStorableBool thrustEnabled;
        public AnimationPattern ap;
        public UIDynamicSlider slider;
        Slider originalSlider;

        UIDynamicButton selectButton;
        UIDynamicButton createButton;

        JSONStorableStringChooser thrustAtomChooser;

        float lastAPCheckTime = 0;

        public void UISetupState(Atom apAtom)
        {
            if (apAtom==null)
            {
                selectButton.gameObject.SetActive(true);
                createButton.gameObject.SetActive(true);
                slider.gameObject.SetActive(false);
            }
            else
            {
                ap = apAtom.GetStorableByID("AnimationPattern") as AnimationPattern;
                if (ap == null)
                {
                    return;
                }

                ap.InitUI();
                AttachCustomSlider();
                selectButton.gameObject.SetActive(false);
                createButton.gameObject.SetActive(false);
                slider.gameObject.SetActive(true);
            }
        }

        public ThrustController(DollmasterPlugin dm) : base(dm)
        {
            thrustEnabled = new JSONStorableBool("thrustEnabled", true);
            dm.RegisterBool(thrustEnabled);
            UIDynamicToggle moduleEnableToggle = dm.CreateToggle(thrustEnabled);
            moduleEnableToggle.label = "Enable Thrusting";
            moduleEnableToggle.backgroundColor = Color.green;

            thrustAtomChooser = new JSONStorableStringChooser("thrustTarget", GetAnimationPatternNames(), "", "Thrust Control", (string name)=>
            {
                RestoreOriginalSlider();

                ap = null;

                UISetupState(SuperController.singleton.GetAtomByUid(name));

            });
            thrustAtomChooser.storeType = JSONStorableParam.StoreType.Full;
            dm.RegisterStringChooser(thrustAtomChooser);
            UIDynamicPopup popup = dm.CreatePopup(thrustAtomChooser);
            popup.popup.onOpenPopupHandlers += () =>
            {
                thrustAtomChooser.choices = GetAnimationPatternNames();
            };

            slider = dm.ui.CreateSlider("Thrust Speed", 300, 120);
            slider.transform.Translate(0, 0.15f, 0, Space.Self);

            slider.slider.onValueChanged.AddListener((float v) =>
            {
                if (ap == null)
                {
                    return;
                }

                JSONStorableFloat speedStore = ap.GetFloatJSONParam("speed");
                if (speedStore.slider != slider.slider)
                {
                    AttachCustomSlider();
                }
            });

            Image img = slider.GetComponentInChildren<Image>();
            img.color = new Color(0.4f, 0.2f, 0.245f, 1.0f);
            slider.labelText.color = new Color(1, 1, 1);

            slider.gameObject.SetActive(false);

            createButton = dm.ui.CreateButton("Generate Animation Pattern or...", 400, 120);
            createButton.transform.Translate(0, 0.15f, 0, Space.Self);
            createButton.buttonColor = new Color(0.4f, 0.2f, 0.245f, 1.0f);
            createButton.textColor = new Color(1, 1, 1);

            createButton.button.onClick.AddListener(GenerateThrustAtoms);

            selectButton = dm.ui.CreateButton("Select Animation Pattern To Control", 400, 120);
            selectButton.transform.Translate(0.52f, 0.15f, 0, Space.Self);
            selectButton.buttonColor = new Color(0.4f, 0.2f, 0.245f, 1.0f);
            selectButton.textColor = new Color(1, 1, 1);

            selectButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.SelectModeAtom((atom) =>
                {
                    if (atom == null)
                    {
                        return;
                    }

                    if (atom.GetStorableByID("AnimationPattern")==null)
                    {
                        SuperController.LogError("Select an Animation Pattern");
                        return;
                    }

                    AnimationPattern ap = atom.GetStorableByID("AnimationPattern") as AnimationPattern;
                    ap.SyncStepNames();
                    ap.autoSyncStepNamesJSON.SetVal(false);

                    thrustAtomChooser.SetVal(atom.uid);
                });
            });

            dm.CreateSpacer();

            GenerateThrustAtoms();
        }

        List<string> GetAnimationPatternNames()
        {
            List<string> list = SuperController.singleton.GetAllAnimationPatterns().Select((ap) =>
            {
                return ap.containingAtom.uid;
            }).ToList();

            list.Insert(0, "");

            return list;
        }

        void RestoreOriginalSlider()
        {
            if (ap != null && originalSlider != null)
            {
                ap.GetFloatJSONParam("speed").slider = originalSlider;
            }
        }

        void AttachCustomSlider()
        {
            if (ap != null)
            {
                JSONStorableFloat speedStore = ap.GetFloatJSONParam("speed");
                speedStore.min = 0;
                speedStore.max = 6;

                originalSlider = speedStore.slider;
                speedStore.slider = slider.slider;
            }
        }

        public override void Update()
        {
            base.Update();

            if (thrustEnabled.val == false)
            {
                return;
            }

            if (dm.arousal != null && ap!=null)
            {
                float alpha = ap.GetCurrentTimeCounter() / ap.GetTotalTime();
                if (alpha > 0.95f)
                {
                    dm.TriggerExpression();
                }
            }

            if(dm.climaxController.isClimaxing || dm.climaxController.isResting)
            {
                slider.slider.value = slider.slider.maxValue * (1.0f - dm.climaxController.climaxAlpha);
            }

            if(thrustAtomChooser.val != thrustAtomChooser.defaultVal && Time.time - lastAPCheckTime > 1.0f)
            {
                UISetupState(SuperController.singleton.GetAtomByUid(thrustAtomChooser.val));
                lastAPCheckTime = Time.time;
            }

        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            RestoreOriginalSlider();
            Clear();
        }

        public float sliderValue
        {
            get
            {
                return slider.slider.value;
            }
        }

        public void Clear()
        {
            if (ap != null)
            {
                SuperController.singleton.RemoveAtom(ap.containingAtom);
            }
        }

        public void GenerateThrustAtoms()
        {
            dm.StartCoroutine(CreateAtom("AnimationPattern", "Thrust AP", (apAtom) =>
            {
                AnimationPattern ap = apAtom.GetStorableByID("AnimationPattern") as AnimationPattern;
                this.ap = ap;

                ap.autoSyncStepNamesJSON.SetVal(false);

                thrustAtomChooser.SetVal(apAtom.name);

                if (ap.steps.Length >= 2)
                {
                    return;
                }

                ConfigureAPSteps(atom);

                UISetupState(apAtom);

                ap.SyncStepNames();
                ap.containingAtom.hidden = true;
                ap.hideCurveUnlessSelected = true;
                ap.HideAllSteps();

            }, true));
        }

        public void ConfigureAPSteps(Atom receiverAtom, Atom partner=null)
        {
            ap.DestroyAllSteps();

            Atom apAtom = ap.containingAtom;

            FreeControllerV3 hipControl= receiverAtom.GetStorableByID("hipControl") as FreeControllerV3;

            //apAtom.containingAtom.mainController.transform.position = new Vector3();
            //apAtom.mainController.transform.rotation = new Quaternion();
            //apAtom.mainController.transform.SetPositionAndRotation(new Vector3(), new Quaternion());

            apAtom.mainController.transform.SetPositionAndRotation(hipControl.transform.position, hipControl.transform.rotation);
            apAtom.SelectAtomParent(receiverAtom);

            MoveProducer mp = apAtom.GetStorableByID("AnimatedObject") as MoveProducer;
            mp.SetReceiverByName(receiverAtom.name + ":hipControl");

            Vector3 stepATarget = hipControl.transform.position;
            Quaternion stepARotation = hipControl.transform.rotation;

            Vector3 stepBTarget = (receiverAtom.GetStorableByID("hip") as DAZBone).transform.position;
            Quaternion stepBRotation = hipControl.transform.rotation;

            if (partner != null)
            {
                DAZCharacterSelector partnerCharacter = partner.GetStorableByID("geometry") as DAZCharacterSelector;
                if(partnerCharacter.gender == DAZCharacterSelector.Gender.Male)
                {
                    FreeControllerV3 penisTipControl = partner.GetStorableByID("penisTipControl") as FreeControllerV3;
                    FreeControllerV3 penisBaseControl = partner.GetStorableByID("penisBaseControl") as FreeControllerV3;
                    stepATarget = penisBaseControl.transform.position;
                    stepBTarget = penisTipControl.transform.position;
                }
                else
                {
                    FreeControllerV3 penisBaseControl = receiverAtom.GetStorableByID("penisBaseControl") as FreeControllerV3;
                    stepATarget = penisBaseControl.transform.position;
                    stepBTarget = (partner.GetStorableByID("hip") as DAZBone).transform.position;
                }
            }
            else
            {
                FreeControllerV3 chestControl = receiverAtom.GetStorableByID("chestControl") as FreeControllerV3;
                stepBTarget = Vector3.Lerp(stepATarget, chestControl.transform.position, 0.3f);
            }

            AnimationStep stepA = ap.CreateStepAtPosition(0);
            stepA.containingAtom.ClearParentAtom();
            stepA.containingAtom.mainController.transform.position = stepATarget;
            stepA.containingAtom.mainController.transform.rotation = stepARotation;

            AnimationStep stepB = ap.CreateStepAtPosition(1);
            stepB.containingAtom.ClearParentAtom();
            stepB.containingAtom.mainController.transform.position = stepBTarget;
            stepB.containingAtom.mainController.transform.rotation = stepBRotation;

            //apAtom.mainController.transform.Translate(0, 0, -0.2f, Space.Self);

            stepA.containingAtom.mainController.currentPositionState = FreeControllerV3.PositionState.ParentLink;
            stepA.containingAtom.mainController.currentRotationState = FreeControllerV3.RotationState.ParentLink;
            Rigidbody rba = SuperController.singleton.RigidbodyNameToRigidbody(apAtom.name + ":control");
            stepA.containingAtom.mainController.SelectLinkToRigidbody(rba, FreeControllerV3.SelectLinkState.PositionAndRotation);

            stepB.containingAtom.mainController.currentPositionState = FreeControllerV3.PositionState.ParentLink;
            stepB.containingAtom.mainController.currentRotationState = FreeControllerV3.RotationState.ParentLink;
            Rigidbody rbb = SuperController.singleton.RigidbodyNameToRigidbody(apAtom.name + ":control");
            stepB.containingAtom.mainController.SelectLinkToRigidbody(rbb, FreeControllerV3.SelectLinkState.PositionAndRotation);
        }
    }
}
