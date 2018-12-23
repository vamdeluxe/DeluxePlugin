using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.FullService
{
    public class SceneBranch : MVRScript
    {
        Sequence currentSequence = null;
        Dictionary<string, Sequence> sequences = new Dictionary<string, Sequence>();
        SceneLoader sceneLoader = null;

        public static string LOAD_PATH;

        public override void Init()
        {
            try
            {
                LOAD_PATH = SuperController.singleton.currentLoadDir;

                Sequence.onBranchSelected += OnBranchSelected;

                JSONStorableString sequenceStorable = new JSONStorableString("sequencePath", "", (string sequencePath)=>
                {
                    #region path normalization testing
                    //Debug.Log(sequencePath);
                    //string scenePath = SuperController.singleton.NormalizeScenePath(sequencePath);
                    //string directoryPath = SuperController.singleton.NormalizeDirectoryPath(sequencePath);
                    //string loadPath = SuperController.singleton.NormalizeLoadPath(sequencePath);
                    //string savePath = SuperController.singleton.NormalizeSavePath(sequencePath);
                    //string mediaPath = SuperController.singleton.NormalizeMediaPath(sequencePath);
                    //string path = SuperController.singleton.NormalizePath(sequencePath);

                    //Debug.Log("path " + path);
                    //Debug.Log("scene path " + scenePath);
                    //Debug.Log("directory path " + directoryPath);
                    //Debug.Log("load path " + loadPath);
                    //Debug.Log("save path " + savePath);
                    //Debug.Log("media path " + mediaPath);
                    #endregion


                    Cleanup();

                    string loadPath = SuperController.singleton.NormalizeLoadPath(sequencePath);
                    string loadedString = SuperController.singleton.ReadFileIntoString(loadPath);
                    JSONClass setup = (JSONClass) JSON.Parse(loadedString);

                    string startingSequenceName = setup["start"];
                    sceneLoader = new SceneLoader(setup["reference"], GetContainingAtom());

                    JSONClass sequenceData = setup["sequences"].AsObject;
                    sequenceData.Keys.ToList().ForEach((string key) =>
                    {
                        JSONClass branchesObject = sequenceData[key].AsObject["branch"].AsObject as JSONClass;
                        Sequence sequence = sequences[key] = new Sequence(this, branchesObject);
                    });

                    currentSequence = sequences[startingSequenceName];

                    if (currentSequence != null)
                    {
                        currentSequence.GenerateButtons();
                    }
                });

                RegisterString(sequenceStorable);

                CreateButton("Load Sequence").button.onClick.AddListener(() =>
                {
                    SuperController.singleton.currentLoadDir = LOAD_PATH;
                    SuperController.singleton.GetScenePathDialog((filePath) =>
                    {
                        if (string.IsNullOrEmpty(filePath))
                        {
                            return;
                        }

                        string normalizedPath = SuperController.singleton.NormalizeSavePath(filePath);
                        sequenceStorable.SetVal(filePath);
                    });
                });


            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void OnBranchSelected(string branchName, Sequence sequence)
        {
            if (sequences.ContainsKey(branchName) == false)
            {
                return;
            }

            Sequence nextSequence = sequences[branchName];
            sequence.DestroyButtons();

            currentSequence = nextSequence;
            currentSequence.GenerateButtons();

            SuperController.singleton.currentLoadDir = LOAD_PATH;
            sceneLoader.LoadSequence(branchName);
        }

        void Cleanup()
        {
            if (sceneLoader != null)
            {
                sceneLoader.RemoveSceneOnlyAtoms();
            }

            sequences.Values.ToList().ForEach((sequence) =>
            {
                sequence.DestroyButtons();
            });

            currentSequence = null;
            sequences = new Dictionary<string, Sequence>();
        }

        void OnDestroy()
        {
            Cleanup();
        }
    }
}