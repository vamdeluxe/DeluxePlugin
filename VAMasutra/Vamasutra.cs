using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleJSON;
using UnityEngine.UI;

namespace DeluxePlugin
{
    public class Vamasutra : MVRScript
    {
        private const string VAMAMSUTRA_UI_NAME = "VAMasutra Selector";
        private const string POSITIONS_DROPDOWN_NAME = "Positions Dropdown";
        private const string UI_ASSET_PREFAB = "assets/vamasutra.prefab";
        private const string DROPDOWN_NAME = "Positions Dropdown";

        private string PATH_TO_UI_ASSET = Application.dataPath + "/../Dollmaster/vamasutra.assetbundle";

        private List<UIDynamic> dynamicUI = new List<UIDynamic>();
        private string loadedSave = "";
        private string PATH_WHEN_LOADED = "";

        public void Start()
        {
            try
            {
                #region MVRUI Setup
                PATH_WHEN_LOADED = SuperController.singleton.currentLoadDir;

                CreateButton("Load").button.onClick.AddListener(() =>
                {
                    SuperController.singleton.GetScenePathDialog((string saveName) =>
                    {
                        if (String.IsNullOrEmpty(saveName))
                        {
                            return;
                        }
                        LoadPosesAndMakeMenu(saveName);
                        loadedSave = saveName;
                    });
                });

                UIDynamicButton buttonDynamic = CreateButton("Reset", true);
                buttonDynamic.button.onClick.AddListener(() =>
                {
                    if (string.IsNullOrEmpty(loadedSave))
                    {
                        return;
                    }
                    LoadPosesAndMakeMenu(loadedSave);
                });
                #endregion

                StartCoroutine(WaitThenLoadUI());
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }


        #region Pose Loading
        void LoadPoseFromJSON(Atom atom, JSONClass asObject)
        {
            atom.PreRestore();
            atom.RestoreTransform(asObject);
            atom.Restore(asObject, true, false, false, null);
            atom.LateRestore(asObject, true, false, false);
            atom.PostRestore();
        }

        List<JSONClass> LoadPosesFromFile(string saveName)
        {
            List<JSONClass> foundPeople = new List<JSONClass>();

            string aJSON = SuperController.singleton.ReadFileIntoString(saveName);
            JSONNode jSONNode = JSON.Parse(aJSON);
            JSONArray asArray = jSONNode["atoms"].AsArray;

            List<string> listOfPeople = SuperController.singleton.GetAtoms()
            .Where(atom => atom.GetStorableByID("geometry") != null && atom != containingAtom)
            .Select(atom => atom.name)
            .ToList();

            int personIndex = 0;
            for (int i = 0; i < asArray.Count; i++)
            {
                JSONClass asObject = asArray[i].AsObject;
                string id = asObject["id"];
                string type = asObject["type"];
                if (type == "Person")
                {
                    Atom atom = GetAtomById(id);

                    if (atom != null)
                    {
                        LoadPoseFromJSON(atom, asObject);
                    }
                    else
                    {
                        if (personIndex < listOfPeople.Count)
                        {
                            string personByIndex = listOfPeople[personIndex];
                            atom = GetAtomById(personByIndex);
                            if (atom != null)
                            {
                                LoadPoseFromJSON(atom, asObject);
                            }
                        }
                    }

                    personIndex++;

                    foundPeople.Add(asObject);
                }
            }

            SuperController.singleton.PauseSimulation(5, "Loading " + saveName);
            return foundPeople;
        }

        void LoadPosesAndMakeMenu(string saveName)
        {
            dynamicUI.ForEach((dropdown) =>
            {
                Destroy(dropdown.gameObject);
            });

            dynamicUI.Clear();


            List<string> listOfPeople = SuperController.singleton.GetAtoms()
            .Where(atom => atom.GetStorableByID("geometry") != null && atom != containingAtom)
            .Select(atom => atom.name).ToList();

            LoadPosesFromFile(saveName).ForEach((jsonObject) =>
            {
                string id = jsonObject["id"];
                Atom atom = GetAtomById(id);
                JSONStorableStringChooser receiverChoiceJSON = new JSONStorableStringChooser("assign_" + id, listOfPeople, atom == null ? null : atom.name, "Assign " + id + " to", delegate (string receiverName)
                {
                    Atom selectedAtom = GetAtomById(receiverName);
                    LoadPoseFromJSON(selectedAtom, jsonObject);
                });

                UIDynamicPopup dp = CreateScrollablePopup(receiverChoiceJSON, false);
                dynamicUI.Add(dp);
            });
        }

        //  Because we can't use System.IO.Path ........

        private static readonly char DirectorySeparatorChar = '\\';
        private const string DirectorySeparatorCharAsString = "\\";
        private static readonly char AltDirectorySeparatorChar = '/';
        private static readonly char VolumeSeparatorChar = ':';

        private static void CheckInvalidPathChars(string path, bool checkAdditional = false)
        {
            if (path == null)
                throw new ArgumentNullException("path");
        }

        private static String GetFileName(String path)
        {
            if (path != null)
            {
                CheckInvalidPathChars(path);

                int length = path.Length;
                for (int i = length; --i >= 0;)
                {
                    char ch = path[i];
                    if (ch == DirectorySeparatorChar || ch == AltDirectorySeparatorChar || ch == VolumeSeparatorChar)
                        return path.Substring(i + 1, length - i - 1);

                }
            }
            return path;
        }

        private static String GetFileNameWithoutExtension(String path)
        {
            path = GetFileName(path);
            if (path != null)
            {
                int i;
                if ((i = path.LastIndexOf('.')) == -1)
                    return path; // No path extension found
                else
                    return path.Substring(0, i);
            }
            return null;
        }

        private string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }
        #endregion

        #region UI Custom Asset

        /// <summary>
        /// This script loads way faster than the unity asset so we gotta wait...
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitThenLoadUI()
        {
            yield return new WaitForSeconds(2.0f);
            StartCoroutine(CreateAssetAtom(VAMAMSUTRA_UI_NAME, PATH_TO_UI_ASSET, UI_ASSET_PREFAB, DROPDOWN_NAME, SetupUI));
        }

        delegate void AtomCreatedEvent(Atom atom);

        IEnumerator CreateAssetAtom(string uniqueAtomName, string pathToAsset, string prefabName, string nameOfGameObjectToFind, AtomCreatedEvent OnAtomCreated)
        {
            yield return new WaitForSeconds(0.5f);

            Atom createdAtom = GetAtomById(uniqueAtomName);
            if (createdAtom == null)
            {
                SuperController.singleton.StartCoroutine(SuperController.singleton.AddAtomByType("CustomUnityAsset", uniqueAtomName, true));
                yield return new WaitWhile(() => GetAtomById(uniqueAtomName) == null);
                createdAtom = GetAtomById(uniqueAtomName);
            }

            JSONStorable storable = createdAtom.GetStorableByID("asset");
            storable.SetUrlParamValue("assetUrl", pathToAsset);
            storable.SetStringChooserParamValue("assetName", prefabName);


            yield return new WaitWhile(() => GameObject.Find(nameOfGameObjectToFind) == null);
            if (OnAtomCreated != null)
            {
                OnAtomCreated(createdAtom);
            }
        }


        Dropdown dropdown;
        string[] scenes;
        Atom vamasutraUIAtom;

        void SetupUI(Atom atom)
        {
            vamasutraUIAtom = atom;
            vamasutraUIAtom.GetStorableByID("scale").SetFloatParamValue("scale", 0.5f);

            GameObject dropdownGO = GameObject.Find(POSITIONS_DROPDOWN_NAME);
            if (dropdownGO == null)
            {
                Debug.Log("no VAMasutra HUD found");
                return;
            }

            if (dropdownGO != null)
            {
                dropdown = dropdownGO.GetComponent<Dropdown>();
                dropdown.onValueChanged.RemoveAllListeners();
                dropdown.onValueChanged.AddListener(OnSelected);
            }

            //string vamasutraPath = PATH_WHEN_LOADED + "/VAMasutra/";
            string vamasutraPath = Application.dataPath + "/../Dollmaster/VAMasutra/";

            scenes = SuperController.singleton.GetFilesAtPath(vamasutraPath).ToList().Where((str) => str.Contains(".json")).ToArray();

            List<string> sceneNames = new List<string>();
            List<Sprite> sprites = new List<Sprite>();
            foreach (string scenePath in scenes)
            {
                string filename = GetFileNameWithoutExtension(scenePath);
                string name = FirstLetterToUpper(filename);

                sceneNames.Add(name);
                string thumbnailPath = vamasutraPath + filename + ".jpg";
                try
                {

                    //string dataAsString = SuperController.singleton.ReadFileIntoString(thumbnailPath);

                    //  can't do this...
                    //File.ReadAllBytes(thumbnailPath);

                    //byte[] data = Encoding.ASCII.GetBytes(dataAsString);
                    //Texture2D texture = new Texture2D(512, 512, TextureFormat.ARGB32, false);
                    //texture.LoadImage(data);
                    //Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                    //sprites.Add(sprite);
                }
                catch (Exception e)
                {

                }

                sprites.Add(null);
            }



            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            for (int i = 0; i < sceneNames.Count; i++)
            {
                string name = sceneNames[i];
                Sprite sprite = sprites[i];

                Dropdown.OptionData data;
                if (sprite != null)
                {
                    data = new Dropdown.OptionData(name, sprite);
                }
                else
                {
                    data = new Dropdown.OptionData(name);
                }

                options.Add(data);
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(options);
        }

        private void OnSelected(int id)
        {
            LoadPosesAndMakeMenu(scenes[id]);
        }
        #endregion


        void OnDestroy()
        {
            if (vamasutraUIAtom != null)
            {
                SuperController.singleton.RemoveAtom(vamasutraUIAtom);
            }
        }

    }
}
