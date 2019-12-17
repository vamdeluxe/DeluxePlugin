using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;


namespace DeluxePlugin.Dollmaster
{
    public class MontageController : BaseModule
    {
        public List<Montage> montages = new List<Montage>();

        PoseController poseController;

        public Montage currentMontage;

        public JSONStorableUrl montagePath;
        UIDynamicButton poseButton;

        Color accessButtonColor = new Color(0.05f, 0.35f, 0.08f);
        Color accessButtonColorDisabled = new Color(0.25f, 0.25f, 0.25f);
        Color accessTextColor = new Color(1, 1, 1);
        Color accessTextColorDisabled = new Color(0.5f, 0.5f, 0.5f);

        public MontageController(DollmasterPlugin dm, PoseController poseController) : base(dm)
        {
            montagePath = new JSONStorableUrl("montagePath", DollmasterPlugin.VAMASUTRA_PATH);
            dm.RegisterUrl(montagePath);
            UIDynamicButton selectMontageFolderButton = dm.CreateButton("Select Montage Folder", true);
            selectMontageFolderButton.button.onClick.AddListener(() =>
            {
                SuperController.singleton.directoryBrowserUI.defaultPath = SuperController.singleton.savesDir;
                SuperController.singleton.directoryBrowserUI.showDirs = true;
                SuperController.singleton.directoryBrowserUI.showFiles = false;
                SuperController.singleton.directoryBrowserUI.selectDirectory = true;
                SuperController.singleton.directoryBrowserUI.SetTextEntry(true);
                SuperController.singleton.directoryBrowserUI.Show((path) => {
                    montagePath.SetVal(path);
                    LoadMontagesFromPath(path);
                });
            });

            UIDynamicButton montageButton = ui.CreateButton("Random Montage", 200, 50);
            montageButton.button.onClick.AddListener(RandomMontage);
            montageButton.transform.Translate(0, 0.3f, 0, Space.Self);
            UI.ColorButton(montageButton, accessTextColor, accessButtonColor);

            poseButton = ui.CreateButton("Random Pose", 200, 50);
            poseButton.button.onClick.AddListener(RandomPose);
            poseButton.transform.Translate(0.3f, 0.3f, 0, Space.Self);
            UI.ColorButton(poseButton, accessTextColor, accessButtonColor);
        }

        public override void Update()
        {
            if (currentMontage!=null && currentMontage.poses.Count>1)
            {
                poseButton.button.enabled = true;
                poseButton.buttonColor = accessButtonColor;
                poseButton.textColor = accessTextColor;
            }
            else
            {
                poseButton.button.enabled = false;
                poseButton.buttonColor = accessButtonColorDisabled;
                poseButton.textColor = accessTextColorDisabled;
            }
        }

        public void LoadMontagesFromPath(string path)
        {
            montages = new List<Montage>();
            List<string> files = SuperController.singleton.GetFilesAtPath(path, "*.json").ToList();
            files.ForEach(fileName =>
            {
                int periodCount = fileName.Count(f => f == '.');
                if (periodCount == 1)
                {
                    montages.Add(new Montage(atom, fileName));
                }
            });
        }

        public void RandomMontage()
        {
            currentMontage = SelectRandomMontage();
            if (currentMontage != null)
            {
                //SuperController.LogMessage("selected montage " + currentMontage.name);
                //SuperController.LogMessage(currentMontage.poses.Count().ToString());
                currentMontage.Activate(dm);
            }
        }

        public Montage SelectRandomMontage()
        {
            if (montages.Count <= 0)
            {
                return null;
            }

            if (montages.Count == 1)
            {
                return montages[0];
            }

            if (currentMontage == null)
            {
                int randomIndex = UnityEngine.Random.Range(0, montages.Count);
                return montages[randomIndex];
            }

            int currentMontageIndex = montages.FindIndex((montage=>montage==currentMontage));

            int index = UnityEngine.Random.Range(0, montages.Count);
            while (index == currentMontageIndex)
            {
                index = UnityEngine.Random.Range(0, montages.Count);
            }

            return montages[index];
        }

        public void RandomPose()
        {
            if (currentMontage == null)
            {
                return;
            }

            Pose pose = currentMontage.SelectRandomPose();
            if (pose != null)
            {
                dm.poseController.AnimateToPose(pose);
            }
        }

        public void NextThruster()
        {
            AnimationPattern ap = dm.thrustController.ap;
            if (ap == null)
            {
                return;
            }

            Atom apAtom = ap.containingAtom;
            MoveProducer mp = apAtom.GetStorableByID("AnimatedObject") as MoveProducer;

            Atom otherPerson = DollmasterPlugin.GetSomeoneElse(mp.receiver.containingAtom);
            if (otherPerson == null)
            {
                return;
            }

            Atom otherOtherPerson = DollmasterPlugin.GetSomeoneElse(otherPerson);
            if (otherOtherPerson == null)
            {
                return;
            }

            dm.thrustController.ConfigureAPSteps(otherPerson, otherOtherPerson);

        }

        public static void BeginMontage(DollmasterPlugin dm, JSONNode montageJSON)
        {
            dm.poseController.StopCurrentAnimation();

            JSONArray atoms = montageJSON["atoms"].AsArray;

            JSONClass person1Pose = null;
            JSONClass person2Pose = null;
            for (int i = 0; i < atoms.Count; i++)
            {
                JSONClass atomObj = atoms[i].AsObject;
                string id = atomObj["id"].Value;
                if (id == "Person")
                {
                    person1Pose = atomObj;
                }
                if (id == "Person#2")
                {
                    person2Pose = atomObj;
                }
            }

            Atom atom = dm.containingAtom;

            if (person1Pose != null)
            {
                atom.PreRestore();
                atom.RestoreTransform(person1Pose);
                atom.Restore(person1Pose, restorePhysical: true, restoreAppearance: false, restoreCore: false);
                atom.LateRestore(person1Pose, restorePhysical: true, restoreAppearance: false, restoreCore: false);
                atom.PostRestore();
            }

            Atom otherPerson = DollmasterPlugin.GetSomeoneElse(atom);

            if (person2Pose != null && otherPerson != null)
            {
                otherPerson.PreRestore();
                otherPerson.RestoreTransform(person2Pose);
                otherPerson.Restore(person2Pose, restorePhysical: true, restoreAppearance: false, restoreCore: false);
                otherPerson.LateRestore(person2Pose, restorePhysical: true, restoreAppearance: false, restoreCore: false);
                otherPerson.PostRestore();
            }

            SuperController.singleton.PauseSimulation(5, "Loading Sutra");

            dm.thrustController.Clear();
            dm.thrustController.GenerateThrustAtoms();

            ExpressionController.ZeroPoseMorphs(atom);
            if (otherPerson != null)
            {
                ExpressionController.ZeroPoseMorphs(otherPerson);
            }
        }

    }
}
