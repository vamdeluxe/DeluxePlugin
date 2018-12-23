using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin {
	public class BodySwap : MVRScript {

        #region Morph Region Groups
        string[] defaultOff =
        {
            "Pose Controls",
            "Actor",
            "Ears",
            "Eyes",
            "Face",
            "Mouth",
            "Head",
            "Nose"
        };

        string[] headRegion =
        {
            "Ears",
            "Eyes",
            "Face",
            "Mouth",
            "Head",
            "Nose",
            "Teeth"
        };

        string[] bodyRegion =
        {
            "Actor",
            "Arms",
            "Body",
            "Chest",
            "Hip",
            "Hands",
            "Legs",
            "Neck",
            "Feet",
            "Waist",
            "Genitalia",
            "Clothing"
        };

        string[] officialRegion =
        {
            "Actor",
            "Arms",
            "Body",
            "Chest",
            "Hip",
            "Hands",
            "Legs",
            "Feet",
            "Waist",
            "Genitalia",
            "Head",
            "Eyes",
            "Ears",
            "Face",
            "Head",
            "Nose",
            "Mouth",
            "Neck",
            "Upper_Body"
        };
        #endregion

        #region State
        //  Used for morph reset
        Dictionary<string, float> initialMorphValues = new Dictionary<string, float>();

        JSONStorableStringChooser selectionStyle = null;

        //  Region name to Toggle checkboxes dictionary
        Dictionary<string, UIDynamicToggle> toggles = new Dictionary<string, UIDynamicToggle>();

        #endregion


        public override void Init() {
			try {



                //  The atom in the scene we're going to copy morphs from
                Atom sceneAtom = null;

                //  The JSON we're copying morphs from, if we are copying from a file
                JSONClass filePersonJSON = null;

                //  In a save file, mapped set of all people to their IDs
                Dictionary<string, JSONClass> idToPersonFromFile = null;


                UpdateInitialMorphs();

                #region Select from Scene or File
                UIDynamicButton selectFileButton = null;

                UIDynamicTextField selectedFileTextField = null;

                JSONStorableStringChooser personChoice = null;

                selectionStyle = new JSONStorableStringChooser("selectionStyle", new List<string>() { "Scene", "File" }, "Scene", "Select Via", (string choice) =>
                {
                    if (choice == "File")
                    {
                        selectFileButton = CreateButton("Choose from File");
                        selectFileButton.textColor = Color.black;
                        selectFileButton.buttonColor = new Color(0.5f, 0.96f, 0.5f);
                        selectFileButton.button.onClick.AddListener(() =>
                        {
                            SuperController.singleton.GetScenePathDialog((filePath) =>
                            {
                                if (String.IsNullOrEmpty(filePath))
                                {
                                    return;
                                }

                                if (filePath.ToLower().Contains(".vac"))
                                {
                                    SuperController.LogError("loading VAC files not supported");
                                    return;
                                }

                                List<JSONClass> people = LoadPeopleFromFile(filePath);

                                if (people.Count <= 0)
                                {
                                    return;
                                }

                                idToPersonFromFile = new Dictionary<string, JSONClass>();
                                people.ForEach((person) =>
                                {
                                    string id = person["id"];
                                    idToPersonFromFile[id] = person;
                                });

                                filePersonJSON = idToPersonFromFile.Values.ToArray()[0];

                                List<string> names = people.Select((person) => {
                                    string id = person["id"];
                                    return id;
                                }).ToList();

                                if (personChoice != null)
                                {
                                    personChoice.choices = names;
                                    personChoice.displayChoices = names;

                                    if (names.Count > 0)
                                    {
                                        personChoice.SetVal(names[0]);
                                    }
                                }

                                if (selectedFileTextField != null)
                                {
                                    Destroy(selectedFileTextField.gameObject);
                                }
                                selectedFileTextField = CreateTextField(new JSONStorableString("file path", filePath));
                            });
                        });
                    }
                    else
                    {
                        if (selectFileButton != null)
                        {
                            RemoveButton(selectFileButton);
                        }

                        if (selectedFileTextField != null)
                        {
                            Destroy(selectedFileTextField.gameObject);
                        }
                    }
                });

                CreateScrollablePopup(selectionStyle);
                #endregion

                #region Choose Atom
                personChoice = new JSONStorableStringChooser("copyFrom", GetPeopleNamesFromScene(), null, "Copy From", delegate (string otherName)
                {
                    if (SelectingFromScene())
                    {
                        sceneAtom = GetAtomById(otherName);
                    }
                    else
                    {
                        if (idToPersonFromFile == null)
                        {
                            return;
                        }

                        if (idToPersonFromFile.ContainsKey(otherName) == false)
                        {
                            return;
                        }

                        filePersonJSON = idToPersonFromFile[otherName];
                        //Debug.Log(filePersonJSON);
                    }
                });
                personChoice.storeType = JSONStorableParam.StoreType.Full;
                UIDynamicPopup scenePersonChooser = CreateScrollablePopup(personChoice, false);
                scenePersonChooser.popupPanelHeight = 250f;
                RegisterStringChooser(personChoice);

                scenePersonChooser.popup.onOpenPopupHandlers += () =>
                 {
                     // refresh from scene if we are looking for people in the current scene
                     if(SelectingFromScene())
                     {
                         personChoice.choices = GetPeopleNamesFromScene();
                     }
                 };
                #endregion

                #region Region Group Buttons
                UIDynamicButton selectAll = CreateButton("Select All", true);
                selectAll.button.onClick.AddListener(delegate (){
                    toggles.Values.ToList().ForEach((toggle) =>
                    {
                        toggle.toggle.isOn = true;
                    });
                });
                selectAll.buttonColor = Color.white;

                UIDynamicButton selectNone = CreateButton("Select None", true);
                selectNone.button.onClick.AddListener(delegate () {
                    toggles.Values.ToList().ForEach((toggle) =>
                    {
                        toggle.toggle.isOn = false;
                    });
                });
                selectNone.buttonColor = Color.white;

                UIDynamicButton selectHead = CreateButton("Select Head", true);
                selectHead.button.onClick.AddListener(delegate () {
                    foreach (KeyValuePair<string, UIDynamicToggle> entry in toggles)
                    {
                        entry.Value.toggle.isOn = headRegion.Any((str)=>entry.Key.Contains(str));
                    }
                });
                selectHead.buttonColor = Color.white;

                UIDynamicButton selectBody = CreateButton("Select Body", true);
                selectBody.button.onClick.AddListener(delegate () {
                    foreach(KeyValuePair<string,UIDynamicToggle> entry in toggles)
                    {
                        entry.Value.toggle.isOn = bodyRegion.Any((str) => entry.Key.Contains(str));
                    }
                });
                selectBody.buttonColor = Color.white;

                UIDynamicButton selectPose = CreateButton("Select Pose Controls", true);
                selectPose.button.onClick.AddListener(delegate () {
                    foreach (KeyValuePair<string, UIDynamicToggle> entry in toggles)
                    {
                        entry.Value.toggle.isOn = entry.Key.Contains("Pose Controls");
                    }
                });
                selectPose.buttonColor = Color.white;

                UIDynamicButton selectOfficial = CreateButton("Select Official Controls", true);
                selectOfficial.button.onClick.AddListener(delegate () {
                    foreach (KeyValuePair<string, UIDynamicToggle> entry in toggles)
                    {
                        entry.Value.toggle.isOn = officialRegion.Any((str) => entry.Key == str);
                    }
                });
                selectOfficial.buttonColor = Color.white;

                UIDynamicButton selectInvert = CreateButton("Invert Selection", true);
                selectInvert.button.onClick.AddListener(delegate () {
                    foreach (KeyValuePair<string, UIDynamicToggle> entry in toggles)
                    {
                        entry.Value.toggle.isOn = !entry.Value.toggle.isOn;
                    }
                });
                selectInvert.buttonColor = Color.white;

                CreateSpacer(true);

                //JSONStorableString stringFilter = new JSONStorableString("filter", "", (string value)=>
                //{
                //    if (string.IsNullOrEmpty(value))
                //    {
                //        return;
                //    }

                //    foreach (KeyValuePair<string, UIDynamicToggle> entry in toggles)
                //    {
                //        entry.Value.toggle.isOn = entry.Key.Contains(value);
                //    }
                //});

                //stringFilter.inputField.enabled = true;

                //UIDynamicTextField filterField = CreateTextField(stringFilter);
                //filterField.enabled = true;
                //filterField.UItext.enabled = true;


                #endregion

                #region Region Toggle Buttons
                JSONStorable geometry = containingAtom.GetStorableByID("geometry");
                DAZCharacterSelector character = geometry as DAZCharacterSelector;
                GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;

                HashSet<string> regions = new HashSet<string>();
                morphControl.GetMorphDisplayNames().ForEach((name) =>
                {
                    DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                    regions.Add(morph.region);
                });

                foreach (string region in regions)
                {
                    bool isOnByDefault = defaultOff.Any((str) => region.Contains(str)) == false;
                    toggles[region] = CreateToggle(new JSONStorableBool(region, isOnByDefault), true);
                }
                #endregion

                #region Additive Toggle
                JSONStorableBool additiveToggle = new JSONStorableBool("Additive", false);
                CreateToggle(additiveToggle).labelText.text = "Additively Copy Morphs";
                #endregion

                #region Copy Button
                UIDynamicButton copyButton = CreateButton("Copy Morphs", false);
                copyButton.buttonColor = new Color(0, 0, 0.5f);
                copyButton.textColor = new Color(1, 1, 1);
                copyButton.button.onClick.AddListener(delegate ()
                {
                    //  copy morphs directly from an existing atom in the current scene
                    if (SelectingFromScene())
                    {
                        if (sceneAtom == null)
                        {
                            Debug.Log("other atom is null");
                            SuperController.LogMessage("Select an atom first");
                            return;
                        }

                        JSONStorable otherGeometry = sceneAtom.GetStorableByID("geometry");
                        DAZCharacterSelector otherCharacter = otherGeometry as DAZCharacterSelector;
                        GenerateDAZMorphsControlUI otherMorphControl = otherCharacter.morphsControlUI;

                        if (additiveToggle.val == false)
                        {
                            ZeroSelectedMorphs(morphControl);
                        }

                        morphControl.GetMorphDisplayNames().ForEach((name) =>
                        {
                            DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                            if (toggles.ContainsKey(morph.region) == false)
                            {
                                return;
                            }

                            if (toggles[morph.region].toggle.isOn)
                            {
                                morph.morphValue = otherMorphControl.GetMorphByDisplayName(name).morphValue;
                            }
                        });

                        //  also copy morphs that weren't included with this atom, such as imported morphs
                        otherMorphControl.GetMorphDisplayNames().ForEach((name) =>
                        {
                            DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                            if (toggles.ContainsKey(morph.region) == false)
                            {
                                return;
                            }

                            if (toggles[morph.region].toggle.isOn)
                            {
                                morph.morphValue = otherMorphControl.GetMorphByDisplayName(name).morphValue;
                            }
                        });
                    }

                    //  copy morphs from a JSON person atom
                    else
                    if(filePersonJSON != null)
                    {
                        JSONClass storableGeometry = null;

                        //  find geometry storable...
                        JSONArray storables = filePersonJSON["storables"].AsArray;
                        for(int i = 0; i < storables.Count; i++)
                        {
                            JSONClass storable = storables[i].AsObject;
                            string id = storable["id"];
                            if (id == "geometry")
                            {
                                storableGeometry = storable;
                                break;
                            }
                        }

                        if (storableGeometry == null)
                        {
                            return;
                        }

                        if (additiveToggle.val == false)
                        {
                            ZeroSelectedMorphs(morphControl);
                        }

                        //  build a morph value dictionary...
                        Dictionary<string, float> otherMorphNameToValue = new Dictionary<string, float>();
                        if (storableGeometry != null)
                        {
                            JSONArray morphs = storableGeometry["morphs"].AsArray;
                            for(int i = 0; i < morphs.Count; i++)
                            {
                                JSONClass morphStorable = morphs[i].AsObject;
                                string name = morphStorable["name"];
                                float value = morphStorable["value"].AsFloat;
                                otherMorphNameToValue[name] = value;
                            }
                        }

                        morphControl.GetMorphDisplayNames().ForEach((name) =>
                        {
                            DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                            if (morph == null)
                            {
                                Debug.Log("morph " + name + " does not exist in this context");
                                return;
                            }

                            if (toggles.ContainsKey(morph.region) == false)
                            {
                                return;
                            }

                            if (otherMorphNameToValue.ContainsKey(name) == false)
                            {
                                return;
                            }

                            if (toggles[morph.region].toggle.isOn)
                            {
                                morph.morphValue = otherMorphNameToValue[name];
                            }
                        });

                        otherMorphNameToValue.Keys.ToList().ForEach((name) =>
                        {
                            DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                            if (morph == null)
                            {
                                Debug.Log("morph " + name + " does not exist in this context");
                                return;
                            }
                            if (toggles.ContainsKey(morph.region) == false)
                            {
                                return;
                            }

                            if (toggles[morph.region].toggle.isOn)
                            {
                                morph.morphValue = otherMorphNameToValue[name];
                            }
                        });

                    }

                });

                CreateSpacer();
                #endregion

                #region Reset Button
                UIDynamicButton resetButton = CreateButton("Reset", false);
                resetButton.buttonColor = new Color(0.96f, 0.45f, 0.45f);
                resetButton.button.onClick.AddListener(delegate ()
                {
                    ResetMorphs();
                });
                CreateSpacer();
                #endregion

                #region Zero Selected Button
                UIDynamicButton zeroButton = CreateButton("Zero Selected", false);
                zeroButton.buttonColor = new Color(0, 0, 0.5f);
                zeroButton.textColor = new Color(1, 1, 1);
                zeroButton.button.onClick.AddListener(delegate ()
                {
                    ZeroSelectedMorphs(morphControl);
                });
                #endregion

                #region Clear Animatable Button
                UIDynamicButton animatableButton = CreateButton("Clear Animatable", false);
                animatableButton.buttonColor = new Color(0, 0, 0.5f);
                animatableButton.textColor = new Color(1, 1, 1);
                animatableButton.button.onClick.AddListener(delegate ()
                {
                    morphControl.GetMorphDisplayNames().ForEach((name) =>
                    {
                        DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                        morph.animatable = false;
                    });
                });

                CreateSpacer();
                #endregion


            }
            catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

        void Start()
        {
            UpdateInitialMorphs();
        }

        #region Implementation
        private void UpdateInitialMorphs()
        {
            JSONStorable geometry = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;
            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                initialMorphValues[name] = morphControl.GetMorphByDisplayName(name).morphValue;
            });
        }

        private void ResetMorphs()
        {
            JSONStorable geometry = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;
            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                if (initialMorphValues.ContainsKey(name) == false)
                {
                    Debug.Log("morph was not initially found: " + name + ", not resetting this morph");
                    return;
                }
                morph.morphValue = initialMorphValues[name];
            });
        }

        private void ZeroSelectedMorphs(GenerateDAZMorphsControlUI morphControl)
        {
            morphControl.GetMorphDisplayNames().ForEach((name) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(name);
                if (toggles.ContainsKey(morph.region) == false)
                {
                    return;
                }

                if (toggles[morph.region].toggle.isOn)
                {
                    morph.morphValue = 0.0f;
                }
            });
        }

        private List<JSONClass> LoadPeopleFromFile(string saveName)
        {
            List<JSONClass> foundPeople = new List<JSONClass>();

            string aJSON = SuperController.singleton.ReadFileIntoString(saveName);
            JSONNode jSONNode = JSON.Parse(aJSON);
            JSONArray asArray = jSONNode["atoms"].AsArray;

            for (int i = 0; i < asArray.Count; i++)
            {
                JSONClass asObject = asArray[i].AsObject;
                string id = asObject["id"];
                string type = asObject["type"];
                if (type == "Person")
                {
                    foundPeople.Add(asObject);
                }
            }

            SuperController.singleton.PauseSimulation(5, "Loading " + saveName);
            return foundPeople;
        }

        private List<string> GetPeopleNamesFromScene()
        {
            return SuperController.singleton.GetAtoms()
                    .Where(atom => atom.GetStorableByID("geometry") != null && atom != containingAtom)
                    .Select(atom => atom.name).ToList();
        }

        private bool SelectingFromScene()
        {
            if (selectionStyle == null)
            {
                return true;
            }

            return selectionStyle.val == "Scene";
        }
        #endregion

    }
}