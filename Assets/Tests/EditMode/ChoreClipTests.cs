using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Core;

namespace OutpostZero.Tests.EditMode
{
    public class ChoreClipTests
    {
        private static string Read(params string[] parts)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(parts))).Replace("\r\n", "\n");
        }

        [Test]
        public void YardWorkPlaysTheWorkTakeAndAVisitTalks()
        {
            foreach (string action in new[] { "Cook", "Build", "Clear", "Medic", CraftQueue.Task })
                Assert.AreEqual(CharacterRig.ActivityWork, YardPose.Activity(action), action);
            Assert.AreEqual(CharacterRig.ActivityTalk, YardPose.Activity("Visit"));
            foreach (string action in new[] { "Rest", "Guard", "Scavenge", "", null })
                Assert.AreEqual(CharacterRig.ActivityNone, YardPose.Activity(action), action ?? "null");
            Assert.AreEqual("Work", CharacterRig.ActivityState(CharacterRig.ActivityWork));
            Assert.AreEqual("Talk", CharacterRig.ActivityState(CharacterRig.ActivityTalk));
            Assert.AreEqual("", CharacterRig.ActivityState(CharacterRig.ActivityNone));
            Assert.AreEqual("", CharacterRig.ActivityState(7));
        }

        [Test]
        public void ARiggedBodyDropsTheProceduralTiltOnlyForItsChore()
        {
            Assert.AreEqual(0f, YardPose.Lean("Build", 0.4f, true), "the Work clip bends the spine itself");
            Assert.AreEqual(YardPose.Lean("Build", 0.4f), YardPose.Lean("Build", 0.4f, false), "capsules still swing");
            Assert.AreEqual(0f, YardPose.Lean("Visit", 0f, true));
            Assert.AreEqual(YardPose.RestLean, YardPose.Lean("Rest", 0f, true), "rest has no clip, so it keeps its slump");
            Assert.AreEqual(YardPose.GuardLean, YardPose.Lean("Guard", 0f, true));
        }

        [Test]
        public void TheMerchantTalksWhileTheStallIsOpen()
        {
            Assert.AreEqual(CharacterRig.ActivityTalk, CharacterRig.MerchantActivity(true));
            Assert.AreEqual(CharacterRig.ActivityWork, CharacterRig.MerchantActivity(false));
            StringAssert.Contains("Add<StallKeeper>(go);", Read("Assets", "Scripts", "Core", "GameSystemsInstaller.cs"));
        }

        [Test]
        public void TheGraphHasAChoreStateForEachTakeAndTheModelsCarryThem()
        {
            string builder = Read("Assets", "Scripts", "Editor", "SurvivorAnimatorBuilder.cs");
            StringAssert.Contains("EnsureParameter(controller, CharacterRig.Activity, AnimatorControllerParameterType.Int);", builder);
            StringAssert.Contains("FindOrAdd(machine, CharacterRig.ActivityStates[i]", builder);
            StringAssert.Contains("AnimatorConditionMode.Equals, code, CharacterRig.Activity", builder);
            StringAssert.Contains("case \"Work\":", builder, "chores loop");
            StringAssert.Contains("case \"Talk\":", builder);
            StringAssert.Contains("walker.Chore(YardPose.Activity(action));", Read("Assets", "Scripts", "Colony", "CampPopulation.cs"));

            foreach (string model in new[] { CampMateBody.ModelId, StallKeeper.ModelId })
            {
                string fbx = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Models", "Characters", model + ".fbx")));
                foreach (string take in CharacterRig.ActivityStates)
                    StringAssert.Contains(take, fbx, model + " has no " + take + " take");
            }
        }
    }
}
