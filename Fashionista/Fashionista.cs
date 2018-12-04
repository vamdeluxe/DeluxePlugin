using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Web;

namespace DeluxePlugin {
	public class Fashionista : MVRScript {

        JSONStorableBool allowDecals;

		public override void Init() {
			try {
                DAZCharacterSelector character = GetCharacter();
                if (character == null)
                {
                    SuperController.LogError("This plugin can only be used on Person atoms.");
                    return;
                }

                CreateButton("Undress", true).button.onClick.AddListener(() =>
                {
                    Undress(character);
                });

                List<UIDynamicButton> buttons = new List<UIDynamicButton>();

                CreateButton("Load Clothing").button.onClick.AddListener(() =>
                {
                    SuperController.singleton.GetScenePathDialog((path) =>
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        buttons.ForEach((button) =>
                        {
                            RemoveButton(button);
                        });

                        buttons.Clear();

                        //  IMPORTANT
                        //  need to set "Save Dir" here because GetJSON relies on this to normalize paths for Texture URLs...
                        SuperController.singleton.currentSaveDir = path;

                        SuperController.singleton.currentLoadDir = path;
                        SuperController.singleton.SetLoadDirFromFilePath(path);

                        JSONNode json = JSON.Parse(SuperController.singleton.ReadFileIntoString(path));
                        List<JSONNode> people = GetPeopleNodes(json);

                        if (people.Count == 0)
                        {
                            SuperController.LogError("The scene must have at least one person.");
                            return;
                        }

                        if (people.Count == 1)
                        {
                            JSONNode person = people[0];
                            StartCoroutine( LoadClothingFromPerson(person, character) );
                        }
                        else {
                            people.ForEach((person) =>
                            {
                                UIDynamicButton button = CreateButton("from " + person["id"]);
                                button.button.onClick.AddListener(() =>
                                {
                                    StartCoroutine( LoadClothingFromPerson(person, character) );
                                });
                                buttons.Add(button);
                            });
                        }




                    });
                });

                //  Decal texture support isn't quite working
                //allowDecals = new JSONStorableBool("allow decals", false);
                //CreateToggle(allowDecals);

            }
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

        //  partial matching list of storable ids we want to ignore
        //  because it's impossible to tell which storable belongs to clothing and which do not...
        List<string> ignoreList = new List<string>()
        {
            "trigger",
            "plugin",
            "hair",
            "audio",
            "expressions",
            "rescale",
            "control",
            "animation",
            "irises",
            "texture"
        };

        IEnumerator LoadClothingFromPerson(JSONNode imported, DAZCharacterSelector character)
        {
            Undress(character);

            JSONArray importedStorables = imported["storables"].AsArray;


            //  attach clothes
            JSONArray clothesArray = GetClothesArrayFromPerson(imported);
            for (int i = 0; i < clothesArray.Count; i++)
            {
                bool enabled = clothesArray[i]["enabled"].AsBool;
                string clothingName = clothesArray[i]["name"];
                character.SetActiveClothingItem(clothingName, enabled, true);
            }

            //  if clothes are not loaded yet, they won't be applied with the correct material when the atom is restored...
            SuperController.singleton.PauseSimulation(10, "Load Clothes " + containingAtom.uid);
            yield return new WaitForSeconds(2.5f);


            //  collapse containing atom to JSON
            JSONClass currentPersonAsJSON = containingAtom.GetJSON();
            JSONArray currentStorables = new JSONArray();
            currentPersonAsJSON["storables"] = currentStorables;

            containingAtom.GetStorableIDs()
                .Select((id) => containingAtom.GetStorableByID(id))
                .Where((storable) =>
                {
                    return storable.name != "PluginManager";
                })
                .Select((storable) =>
                {
                    return storable.GetJSON();
                })
                .ToList()
                .ForEach((node) => currentStorables.Add(node));


            //  merge relevant storables
            for (int i=0; i < importedStorables.Count; i++)
            {
                string id = importedStorables[i]["id"];
                bool containsIgnoreString = ignoreList.Any((ignoreString) =>
                {
                    return id.ToLower().Contains(ignoreString);
                });

                if (containsIgnoreString)
                {
                    continue;
                }

                //  not efficient but
                //  search through all storables of current person with same id and copy data over
                bool found = false;
                for(int s=0; s < currentStorables.Count; s++)
                {
                    string currentId = currentStorables[s]["id"];
                    if (currentId == id)
                    {
                        currentStorables[s] = importedStorables[i];
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                //  if not found, add it
                currentStorables.Add(importedStorables[i]);
            }

            //  not using containingAtom.Restore because VAM will lag if person has custom textures...

            //containingAtom.Restore(currentPersonAsJSON as JSONClass, false, true, false);
            //containingAtom.LateRestore(currentPersonAsJSON as JSONClass, false, true, false);

            //  custom restore will restore everything but geometry and textures

            containingAtom.PreRestore();

            CustomRestore(currentStorables);

            containingAtom.PostRestore();
        }


        public DAZCharacterSelector GetCharacter()
        {
            JSONStorable geometryStorable = containingAtom.GetStorableByID("geometry");
            if (geometryStorable != null)
            {
                DAZCharacterSelector geometry = geometryStorable as DAZCharacterSelector;
                if (geometry != null)
                {
                    return geometry;
                }
            }

            return null;
        }

        public List<JSONNode> GetPeopleNodes(JSONNode jsc)
        {
            List<JSONNode> people = new List<JSONNode>();

            JSONArray atoms = jsc["atoms"].AsArray;
            for (int i = 0; i < atoms.Count; i++)
            {
                JSONNode atom = atoms[i];
                string type = atom["type"];
                if(type == "Person")
                {
                    people.Add(atom);
                }
            }

            return people;
        }

        public void Undress(DAZCharacterSelector character)
        {
            int index = 0;
            character.activeClothingItems.ToList().ForEach((wearing) =>
            {
                character.SetActiveClothingItem(index, false);
                index++;
            });
        }

        public List<JSONNode> JSONArrayToList(JSONArray array)
        {
            List<JSONNode> list = new List<JSONNode>();
            for(int i = 0; i < array.Count; i++)
            {
                list.Add(array[i]);
            }
            return list;
        }

        public JSONArray GetClothesArrayFromPerson(JSONNode person)
        {
            List<JSONNode> storables = JSONArrayToList(person["storables"].AsArray);

            JSONNode geometry = storables.Find((node) =>
            {
                string id = node["id"];
                return id == "geometry";
            });

            JSONArray clothes = geometry["clothing"].AsArray;
            return clothes;
        }

        private void CustomRestore(JSONArray currentStorables)
        {
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            IEnumerator enumerator = currentStorables.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    JSONClass jSONClass = (JSONClass)enumerator.Current;
                    string id = jSONClass["id"];
                    if (id == "geometry" || id == "textures")
                    {
                        JSONStorable storable = containingAtom.GetStorableByID(id);
                        if (!dictionary.ContainsKey(id))
                        {
                            dictionary.Add(id, value: true);
                        }
                        continue;
                    }
                    JSONStorable value = containingAtom.GetStorableByID(id);
                    if (value != null)
                    {
                        value.RestoreFromJSON(jSONClass, false, true, null);
                        if (!dictionary.ContainsKey(id))
                        {
                            dictionary.Add(id, value: true);
                        }
                    }
                }
            }
            finally
            {
                IDisposable disposable;
                if ((disposable = (enumerator as IDisposable)) != null)
                {
                    disposable.Dispose();
                }
            }
            JSONStorable[] array = containingAtom.GetStorableIDs().Select((id) => containingAtom.GetStorableByID(id)).ToArray();
            JSONStorable[] array2 = array;
            foreach (JSONStorable jSONStorable in array2)
            {
                if (!jSONStorable.exclude && !dictionary.ContainsKey(jSONStorable.storeId) && (!jSONStorable.onlyStoreIfActive || jSONStorable.gameObject.activeInHierarchy))
                {
                    JSONClass jc2 = new JSONClass();
                    jSONStorable.RestoreFromJSON(jc2, false, true);
                }
            }
        }



	}
}