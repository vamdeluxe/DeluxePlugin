using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.EmotionEngine
{
    public class ExpressionBank : MVRScript
    {
        string PATH_WHEN_LOADED = "";
        SuperController sc;

        StorableStringList storeList;
        JSONStorableStringChooser expressionChooser;

        GenerateDAZMorphsControlUI morphControl;
        AudioSourceControl headAudioSource;
        Dictionary<string, ExpressionAnimation> animationLookup = new Dictionary<string, ExpressionAnimation>();

        List<ExpressionAnimationUI> uiList = new List<ExpressionAnimationUI>();

        ExpressionAnimation currentAnimation = null;
        ExpressionAnimation transitionAnimation = null;

        string lastLoadPath = "";

        public override void Init()
        {
            try
            {
                lastLoadPath = PATH_WHEN_LOADED = SuperController.singleton.currentLoadDir;
                sc = SuperController.singleton;

                DAZCharacterSelector dcs = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
                morphControl = dcs.morphsControlUI;

                //  Fix tongue morph...
                morphControl.GetMorphByDisplayName("Tongue In-Out").startValue = 0.0f;

                headAudioSource = containingAtom.GetStorableByID("HeadAudioSource") as AudioSourceControl;

                JSONStorableAction playRandomAction = new JSONStorableAction("Play Random Expression", () =>
                {
                    if (currentAnimation != null)
                    {
                        currentAnimation.Stop();
                    }

                    if (animationLookup.Count == 0)
                    {
                        return;
                    }

                    ExpressionAnimation selectedAnimation = currentAnimation;
                    do
                    {
                        int randomIndex = UnityEngine.Random.Range(0, animationLookup.Count);
                        string key = animationLookup.Keys.ToList()[randomIndex];
                        selectedAnimation = animationLookup[key];
                        if (animationLookup.Count <= 1)
                        {
                            break;
                        }
                    }
                    while (currentAnimation == selectedAnimation);

                    PlayExpression(selectedAnimation);
                });
                RegisterAction(playRandomAction);

                JSONStorableAction playRandomWhenReadyAction = new JSONStorableAction("Play Random Expression When Ready", () =>
                {
                    if (currentAnimation != null)
                    {
                        return;
                    }

                    if (animationLookup.Count == 0)
                    {
                        return;
                    }

                    ExpressionAnimation selectedAnimation = currentAnimation;
                    do
                    {
                        int randomIndex = UnityEngine.Random.Range(0, animationLookup.Count);
                        string key = animationLookup.Keys.ToList()[randomIndex];
                        selectedAnimation = animationLookup[key];
                        if (animationLookup.Count <= 1)
                        {
                            break;
                        }
                    }
                    while (currentAnimation == selectedAnimation);

                    PlayExpression(selectedAnimation);
                });
                RegisterAction(playRandomWhenReadyAction);

                CreateButton("Load Expression").button.onClick.AddListener(() =>
                {
                    sc.fileBrowserUI.defaultPath = PATH_WHEN_LOADED;
                    sc.fileBrowserUI.SetTextEntry(false);
                    sc.fileBrowserUI.Show((path) =>
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        JSONStorableString store = storeList.GetNext();
                        if (store == null)
                        {
                            return;
                        }
                        CreateUIFromExpressionPath(path, store);

                        lastLoadPath = path;
                    });
                });


                CreateButton("Load Expression (Folder)").button.onClick.AddListener(() =>
                {
                    sc.GetDirectoryPathDialog((path) =>
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        sc.GetFilesAtPath(path).ToList().ForEach((filePath) =>
                        {
                            if (filePath.ToLower().Contains(".json") == false)
                            {
                                return;
                            }

                            JSONStorableString store = storeList.GetNext();
                            if (store == null)
                            {
                                return;
                            }
                            Debug.Log("loading " + filePath);
                            CreateUIFromExpressionPath(filePath, store);
                        });

                        lastLoadPath = path;

                    }, lastLoadPath);
                });

                CreateButton("Secret Button", true);

                CreateButton("Clear All", true).button.onClick.AddListener(()=>
                {

                    uiList.GetRange(0, uiList.Count).ForEach((ui) =>
                    {
                        ui.Remove();
                    });
                });
                CreateSpacer(false).height = 50;
                CreateSpacer(true).height = 50;

                //  do this last so it shows up at the bottom...
                storeList = new StorableStringList(this, "expression_");


            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Start()
        {

            //  this allows other atoms to target
            expressionChooser = new JSONStorableStringChooser("expression", new List<string>(), "", "expression", (string choice) =>
            {
                ExpressionAnimation animation = animationLookup[choice];
                PlayExpression(animation);
            });
            RegisterStringChooser(expressionChooser);

            //  load storables as animations
            storeList.storables.ForEach((storable) =>
            {
                if (storable.val != storable.defaultVal)
                {
                    CreateUIFromExpressionPath(storable.val, storable);
                }
            });
        }

        ExpressionAnimationUI CreateUIFromExpressionPath(string path, JSONStorableString store)
        {
            string fileName = PathExt.GetFileNameWithoutExtension(path);

            store.SetVal(sc.NormalizePath(path));

            string jsonString = sc.ReadFileIntoString(path);

            ExpressionAnimation animation = new ExpressionAnimation(morphControl, jsonString);

            JSONStorableAction action = new JSONStorableAction("Play " + fileName, ()=>
            {
                PlayExpression(animation);
            });
            RegisterAction(action);

            ExpressionAnimationUI ui = null;
            ui = new ExpressionAnimationUI(this, fileName, animation, () =>
            {
                store.SetVal("");
                uiList.Remove(ui);
                animationLookup.Remove(fileName);
                DeregisterAction(action);
            });




            animationLookup[fileName] = animation;
            expressionChooser.choices = animationLookup.Keys.ToList();
            uiList.Add(ui);
            return ui;
        }

        public void PlayExpression(ExpressionAnimation animation)
        {
            ExpressionAnimation lastAnimation = currentAnimation;

            currentAnimation = animation;
            currentAnimation.Start();
            if (animation.nac != null)
            {
                headAudioSource.PlayNow(animation.nac);
            }

            if (lastAnimation != null && currentAnimation != null && lastAnimation != currentAnimation)
            {
                transitionAnimation = new ExpressionAnimation(morphControl, lastAnimation, currentAnimation);
                transitionAnimation.StartFadeOut();
            }
        }

        public void Update()
        {
            if(currentAnimation != null && currentAnimation.isPlaying)
            {
                bool complete = currentAnimation.Update();

                if (complete)
                {
                    currentAnimation.StartFadeOut();
                }
            }

            if (currentAnimation != null && currentAnimation.isFading)
            {
                bool fadeComplete = currentAnimation.UpdateFadeOut();
                if (fadeComplete)
                {
                    currentAnimation = null;
                }
            }

            if (transitionAnimation != null && transitionAnimation.isFading)
            {
                transitionAnimation.UpdateFadeOut();
            }
        }


    }

    class StorableActionList
    {
        const int maxCount = 100;
        public List<JSONStorableAction> storables = new List<JSONStorableAction>();
        public StorableActionList(MVRScript script, string prefix)
        {
            for (int i = 0; i < maxCount; i++)
            {
                JSONStorableAction store = new JSONStorableAction(prefix + i, null);
                storables.Add(store);
                script.RegisterAction(store);
            }
        }

        public JSONStorableAction GetNext()
        {
            int nextIndex = FindNextOpen();
            if (nextIndex < 0)
            {
                Debug.LogWarning("Ran out of space in storable string list");
                return null;
            }
            JSONStorableAction next = storables[nextIndex];
            return next;
        }

        int FindNextOpen()
        {
            for (int i = 0; i < storables.Count; i++)
            {
                if (storables[i].actionCallback == null)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    class StorableStringList
    {
        const int maxCount = 100;
        public List<JSONStorableString> storables = new List<JSONStorableString>();
        public StorableStringList(MVRScript script, string prefix)
        {
            for (int i = 0; i < maxCount; i++)
            {
                JSONStorableString store = new JSONStorableString(prefix + i, "");
                storables.Add(store);
                script.RegisterString(store);
            }
        }

        public JSONStorableString GetNext()
        {
            int nextIndex = FindNextOpen();
            if (nextIndex < 0)
            {
                Debug.LogWarning("Ran out of space in storable string list");
                return null;
            }
            JSONStorableString next = storables[nextIndex];
            return next;
        }

        int FindNextOpen()
        {
            for(int i=0; i< storables.Count; i++)
            {
                if(storables[i].val == storables[i].defaultVal)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    class ExpressionAnimationUI
    {
        ExpressionAnimation animation;
        UIDynamicButton testButton;
        UIDynamicButton removeButton;
        Action onRemove;
        ExpressionBank script;

        public ExpressionAnimationUI(ExpressionBank script, string name, ExpressionAnimation animation, Action onRemove=null)
        {
            this.script = script;
            this.animation = animation;

            testButton = script.CreateButton("Play " + name);
            testButton.button.onClick.AddListener(() =>
            {
                script.PlayExpression(animation);
            });

            this.onRemove = onRemove;

            removeButton = script.CreateButton("Remove " + name, true);
            removeButton.button.onClick.AddListener(Remove);
        }

        public void Remove()
        {
            script.RemoveButton(removeButton);
            script.RemoveButton(testButton);
            if (onRemove!=null)
            {
                onRemove();
            }
        }

    }

    class PathExt
    {

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

        public static String GetFileName(String path)
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

        public static String GetFileNameWithoutExtension(String path)
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

        public string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }
    }
}
