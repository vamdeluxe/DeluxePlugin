using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin
{
    class BulkAudioLoader : MVRScript
    {
        List<NamedAudioClip> clips = new List<NamedAudioClip>();
        JSONStorableString loadLog;

        public override void Init()
        {
            try
            {
                loadLog = new JSONStorableString("log", "");
                CreateTextField(loadLog, true).height = 400;

                JSONStorableString pathStorable = new JSONStorableUrl("audioPath", "", (string path) =>
                {
                    /*
                    Debug.Log("savedPath: " + path);
                    Debug.Log("dataPath: " + Application.dataPath);
                    Debug.Log("savedir: " + SuperController.singleton.currentLoadDir);
                    Debug.Log("loaddir: " + SuperController.singleton.currentSaveDir);
                    Debug.Log("combined: " + Application.dataPath + "/../" + path);
                    */
                    LoadClips(Application.dataPath + "/../" + path);


                });
                RegisterString(pathStorable);


                CreateButton("Load Audio From Folder").button.onClick.AddListener(() =>
                {
                    SuperController.singleton.GetDirectoryPathDialog((string path) => {
                        if (String.IsNullOrEmpty(path))
                        {
                            return;
                        }
                        pathStorable.val = SuperController.singleton.NormalizePath(path);

                    }, SuperController.singleton.currentLoadDir);
                });

                if (string.IsNullOrEmpty(pathStorable.val) == false)
                {
                    Debug.Log(pathStorable.val);
                    LoadClips(Application.dataPath + "/../" + pathStorable.val);
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void OnDestroy()
        {
            ClearClips();
        }

        void ClearClips()
        {
            clips.ForEach((nac) =>
            {
                URLAudioClipManager.singleton.RemoveClip(nac);
            });
            clips.Clear();
        }

        void LoadClips(string folder)
        {
            ClearClips();
            try
            {
                //  try as-is, when user selects a folder instead of being in the folder
                SuperController.singleton.GetFilesAtPath(folder).ToList().ForEach((string fileName) =>
                {
                    clips.Add(LoadAudio(fileName));
                    loadLog.val += SuperController.singleton.NormalizePath(fileName) + "\n";
                });
            }
            catch (Exception e)
            {
                //  if user is in the folder, figure out what path they meant
                string folderName = "/" + folder.Substring(folder.LastIndexOf('/') + 1) + "/";
                folder = folder.Replace(folderName, "/");
                SuperController.singleton.GetFilesAtPath(folder).ToList().ForEach((string fileName) =>
                {
                    clips.Add(LoadAudio(fileName));
                    loadLog.val += SuperController.singleton.NormalizePath(fileName) + "\n";
                });
            }

        }

        NamedAudioClip LoadAudio(string absFilePath)
        {
            string localPath = SuperController.singleton.NormalizePath(absFilePath);
            NamedAudioClip existing = URLAudioClipManager.singleton.GetClip(localPath);
            if (existing != null)
            {
                return existing;
            }

            URLAudioClip clip = URLAudioClipManager.singleton.QueueClip(absFilePath, localPath);
            return URLAudioClipManager.singleton.GetClip(clip.uid);
        }
    }
}
