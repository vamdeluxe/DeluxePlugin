using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using MeshVR;

namespace DeluxePlugin.MorphAnimation
{
    public class MorphAnimation : MVRScript
    {
        List<string> acceptList = new List<string>()
        {
            "Expression",
            "Pose",
        };

        List<DAZMorph> animatableMorphs = new List<DAZMorph>();

        HashSet<string> groups = new HashSet<string>();
        bool isRecording = false;
        float lastRecordTime = 0;
        float lastPlaybackCounter = 0;
        float lastTimelineCounter = 0;

        MotionAnimationMaster mam;

        class MorphChange
        {
            public DAZMorph morph;
            public float value = 0;

            public JSONClass ToJSON(float time)
            {
                JSONClass obj = new JSONClass();
                obj["time"] = time.ToString();
                obj["value"] = value.ToString();
                return obj;
            }
        }

        List<MorphChange> changesOnFrame = new List<MorphChange>();

        JSONClass morphAnimationData = new JSONClass();

        UIDynamicButton recordButton;

        // A linked list of animation steps.
        class MorphAnimationStep
        {
            public DAZMorph morph;
            public MorphAnimationStep next;
            public float time;
            public float value;
        }

        List<MorphAnimationStep> playbackHeads = new List<MorphAnimationStep>();
        List<MorphAnimationStep> playbackSteps = new List<MorphAnimationStep>();

        public override void Init()
        {
            try
            {
                JSONStorableBool dummy = new JSONStorableBool("dummy", false);
                RegisterBool(dummy);
                dummy.SetVal(true);

                mam = SuperController.singleton.motionAnimationMaster;

                recordButton = CreateButton("Record");
                recordButton.buttonColor = Color.green;
                recordButton.button.onClick.AddListener(() =>
                {
                    isRecording = !isRecording;

                    // Should now be recording
                    if (isRecording)
                    {
                        mam.ResetAnimation();
                        mam.autoRecordStop = true;
                        mam.showRecordPaths = false;
                        mam.showStartMarkers = false;
                        mam.StartRecord();
                        recordButton.buttonColor = Color.red;
                    }
                    else
                    {
                        mam.StopRecord();
                        mam.StopPlayback();
                        recordButton.buttonColor = Color.green;
                    }
                });

                DAZCharacterSelector dcs = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
                GenerateDAZMorphsControlUI morphUI = dcs.morphsControlUI;
                List<string> allMorphNames = morphUI.GetMorphDisplayNames();


                List<DAZMorph> acceptableMorphs = allMorphNames
                .Select((string name) =>
                {
                    return morphUI.GetMorphByDisplayName(name);
                })
                .ToList()
                .Where((morph) =>
                {
                    return acceptList.Exists((acceptString) =>
                    {
                        return morph.group.Contains(acceptString);
                    });
                })
                .ToList();

                animatableMorphs = acceptableMorphs;

                acceptableMorphs.ForEach((morph) =>
                {
                    groups.Add(morph.group);
                });

                CreateButton("Rewind").button.onClick.AddListener(() =>
                {
                    mam.SeekToBeginning();
                    GenerateAnimation();
                });

                CreateButton("Play").button.onClick.AddListener(() =>
                {
                    mam.StartPlayback();
                    mam.StopRecord();
                    recordButton.buttonColor = Color.green;
                    GenerateAnimation();
                });

                CreateButton("Stop").button.onClick.AddListener(() =>
                {
                    mam.StopRecord();
                    mam.StopPlayback();
                    recordButton.buttonColor = Color.green;
                    GenerateAnimation();
                });

                CreateButton("Clear All Poses").button.onClick.AddListener(() =>
                {
                    acceptableMorphs.ForEach((morph) =>
                    {
                        morph.Reset();
                    });
                });

                CreateSpacer();

                List<UIDynamicSlider> morphSliders = new List<UIDynamicSlider>();
                List<JSONStorableBool> onGroups = new List<JSONStorableBool>();

                groups.ToList().ForEach((groupName) =>
                {
                    JSONStorableBool groupOn = new JSONStorableBool(groupName, false, (bool isOn) =>
                    {
                        // Clear list of morphs.
                        morphSliders.ForEach((slider) =>
                        {
                            RemoveSlider(slider);
                        });
                        morphSliders = new List<UIDynamicSlider>();

                        List<string> onList = onGroups.Where((storableBool) => storableBool.val).ToList().Select((storableBool) => storableBool.name).ToList();

                        acceptableMorphs
                        .Where((morph) =>
                        {
                            return onList.Exists((name) =>
                            {
                                return morph.group == name;
                            });
                        })
                        .ToList()
                        .ForEach((morph) =>
                        {
                            bool highCost = IsHighCostMorph(morph);
                            JSONStorableFloat morphValue = new JSONStorableFloat(morph.displayName, morph.morphValue, (float value) =>
                            {
                                morph.SetValue(value);

                                if (isRecording)
                                {
                                    changesOnFrame.Add(new MorphChange()
                                    {
                                        morph = morph,
                                        value = value,
                                    });
                                }

                            }, morph.min, morph.max, false, true);
                            UIDynamicSlider slider = CreateSlider(morphValue, true);
                            slider.labelText.color = highCost ? Color.red : Color.black;
                            morphSliders.Add(slider);
                        });
                    });
                    UIDynamicToggle groupToggle = CreateToggle(groupOn);
                    onGroups.Add(groupOn);
                });
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        private bool IsHighCostMorph(DAZMorph morph)
        {
            bool flag = false;
            bool flag2 = false;
            DAZMorphFormula[] array = morph.formulas;
            foreach (DAZMorphFormula dAZMorphFormula in array)
            {
                switch (dAZMorphFormula.targetType)
                {
                    case DAZMorphFormulaTargetType.BoneCenterX:
                    case DAZMorphFormulaTargetType.BoneCenterY:
                    case DAZMorphFormulaTargetType.BoneCenterZ:
                        flag = true;
                        break;
                    case DAZMorphFormulaTargetType.OrientationX:
                    case DAZMorphFormulaTargetType.OrientationY:
                    case DAZMorphFormulaTargetType.OrientationZ:
                        flag2 = true;
                        break;
                }
            }

            return flag || flag2;
        }

        void Update()
        {
            bool shouldRecordStep = (mam.playbackCounter - lastRecordTime) > mam.recordInterval;
            if (isRecording && shouldRecordStep)
            {
                lastRecordTime = mam.playbackCounter;

                if (changesOnFrame.Count > 0)
                {
                    float time = mam.playbackCounter;

                    changesOnFrame.ForEach((change) =>
                    {
                        DAZMorph morph = change.morph;
                        string name = morph.displayName;
                        if (morphAnimationData[name] == null)
                        {
                            morphAnimationData[name] = new JSONArray();
                        }

                        JSONArray morphChanges = morphAnimationData[name].AsArray;
                        morphChanges.Add(change.ToJSON(time));
                    });

                    changesOnFrame.Clear();
                }
            }

            if(isRecording && mam.playbackCounter >= mam.totalTime)
            {
                isRecording = false;
                recordButton.buttonColor = Color.green;
            }

            bool recordTimeChanged = mam.playbackCounter != lastPlaybackCounter;
            if (recordTimeChanged)
            {
                lastPlaybackCounter = mam.playbackCounter;
            }

            if (morphAnimationData != null)
            {
                float time = mam.playbackCounter;

                playbackSteps = playbackHeads.Select(v => v).ToList();
                SeekToTime(time);
            }
        }

        public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
        {
            JSONClass jSON = base.GetJSON(includePhysical, includeAppearance);
            jSON["morphAnimations"] = morphAnimationData;
            return jSON;
        }

        public override void RestoreFromJSON(JSONClass jc, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
        {
            base.RestoreFromJSON(jc, restorePhysical, restoreAppearance, presetAtoms);

            if (jc["morphAnimations"] == null)
            {
                return;
            }

            morphAnimationData = jc["morphAnimations"].AsObject;
            GenerateAnimation();
        }

        void GenerateAnimation()
        {
            playbackHeads = ConstructPlayback(morphAnimationData);

            // Make a clone of the list.
            playbackSteps = playbackHeads.Select(v => v).ToList();
            SeekToTime(mam.GetCurrentTimeCounter());
        }

        void SeekToTime(float time)
        {
            playbackSteps.ForEach(clipStep =>
            {
                MorphAnimationStep step = clipStep;
                while (time >= step.time && step.next != null)
                {
                    step = step.next;
                }

                step.morph.SetValue(step.value);
                step.morph.SyncJSON();
            });
        }

        List<MorphAnimationStep> ConstructPlayback(JSONClass animationData)
        {
            DAZCharacterSelector dcs = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphUI = dcs.morphsControlUI;
            return animationData.Keys.ToList().Select((morphName) =>
            {
                DAZMorph morph = morphUI.GetMorphByDisplayName(morphName);
                JSONArray changes = animationData[morphName].AsArray;

                List<MorphAnimationStep> steps = changes.Childs.ToList().Select(change =>
                {
                    JSONClass changeObject = change.AsObject;
                    float time = float.Parse(changeObject["time"]);
                    float value = float.Parse(changeObject["value"]);
                    MorphAnimationStep step = new MorphAnimationStep()
                    {
                        morph = morph,
                        time = time,
                        value = value,
                    };
                    return step;
                }).ToList();

                for(int i=0; i < steps.Count-1; i++)
                {
                    steps[i].next = steps[i + 1];
                }

                MorphAnimationStep head = steps[0];
                return head;
            }).ToList();
        }
    }
}
