using System.Collections.Generic;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Graphics;
using OutpostZero.Shell;
using UnityEngine;

namespace OutpostZero.Tests.EditMode
{
    public class SuccessionFlowTests
    {
        [Test]
        public void AMemorialWallSoftensEveryLoss()
        {
            Assert.AreEqual(25f, SuccessionLedger.Loss(false, false));
            Assert.AreEqual(40f, SuccessionLedger.Loss(true, false));
            Assert.AreEqual(15f, SuccessionLedger.Loss(false, true));
            Assert.AreEqual(30f, SuccessionLedger.Loss(true, true));
            var camp = new List<ColonistDay>
            {
                new ColonistDay { id = "jonas", bond = "Close to Mara", alive = true, morale = 80f },
                new ColonistDay { id = "priya", bond = "", alive = true, morale = 70f }
            };
            SuccessionLedger.Grieve(camp, "Mara Quill", true);
            Assert.AreEqual(50f, camp[0].morale);
            Assert.AreEqual(55f, camp[1].morale);
        }

        [Test]
        public void TheMemorialIsALivingModuleWithAModelAndNames()
        {
            Assert.AreEqual(BuildMenu.Tab.Living, BuildMenu.TabOf(ModuleKind.Memorial));
            Assert.AreEqual("camp.memorial", BuildMenu.LabelKey(ModuleKind.Memorial));
            Assert.AreEqual(6, GridBuilder.Cost(ModuleKind.Memorial));
            Assert.AreEqual("Memorial wall", Loc.T("yard.kind.memorial", "en"));
            Assert.AreEqual("Muro memorial", Loc.T("yard.kind.memorial", "es"));
            Assert.AreEqual(ModuleKind.Campfire + 1, ModuleKind.Memorial);
            string asset = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets/Resources/ModuleLooks.asset"));
            StringAssert.Contains("kind: Memorial", asset);
        }

        [Test]
        public void DeathDrainsTheColourAsTheCameraClosesIn()
        {
            Assert.AreEqual(0f, DeathVeil.Saturation(0f));
            Assert.AreEqual(0f, DeathVeil.Saturation(-1f));
            Assert.AreEqual(DeathVeil.Drained * 0.5f, DeathVeil.Saturation(0.5f), 0.001f);
            Assert.AreEqual(DeathVeil.Drained, DeathVeil.Saturation(3f));
            Assert.Less(DeathVeil.Drained, -50f);
        }

        [Test]
        public void TheSuccessionScreenNamesTheSuggestedHeir()
        {
            Assert.AreEqual("Suggested", Loc.T("menu.heir", "en"));
            Assert.AreEqual("Sugerido", Loc.T("menu.heir", "es"));
        }
    }
}
