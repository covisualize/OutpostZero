using System.IO;
using NUnit.Framework;
using OutpostZero.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OutpostZero.Tests.EditMode
{
    public class InputProviderTests
    {
        private FakeInputProvider fake;

        [SetUp]
        public void Plug()
        {
            fake = new FakeInputProvider { HasPad = true };
            ExpeditionInput.Provider = fake;
        }

        [TearDown]
        public void Unplug()
        {
            ExpeditionInput.Provider = null;
            ControlBindings.ResetDefaults();
            PadBindings.ResetDefaults();
        }

        [Test]
        public void KeysMoveAndThePadStickWinsOverThem()
        {
            fake.Hold(FakeInputProvider.KeyId(Key.W));
            fake.Hold(FakeInputProvider.KeyId(Key.D));
            Vector2 keys = ExpeditionInput.Move;
            Assert.AreEqual(0.7071f, keys.x, 0.001f);
            Assert.AreEqual(0.7071f, keys.y, 0.001f);
            fake.LeftStick = new Vector2(-0.5f, 0f);
            Assert.AreEqual(new Vector2(-0.5f, 0f), ExpeditionInput.Move);
            fake.HasPad = false;
            Assert.AreEqual(Vector2.zero, ExpeditionInput.AimStick);
            Assert.AreEqual(keys, ExpeditionInput.Move);
        }

        [Test]
        public void MouseAndPadTriggerBothFire()
        {
            Assert.IsFalse(ExpeditionInput.FirePressed);
            fake.Press(FakeInputProvider.MouseId(0));
            Assert.IsTrue(ExpeditionInput.FirePressed);
            Assert.IsTrue(ExpeditionInput.FireHeld);
            fake.EndFrame();
            Assert.IsFalse(ExpeditionInput.FirePressed);
            Assert.IsTrue(ExpeditionInput.FireHeld);
            fake.Release(FakeInputProvider.MouseId(0));
            fake.Press(FakeInputProvider.PadId("RightTrigger"));
            Assert.IsTrue(ExpeditionInput.FirePressed);
            Assert.IsTrue(ExpeditionInput.BuildPlacePressed);
        }

        [Test]
        public void ARebindMovesTheActionToTheNewKey()
        {
            fake.Press(FakeInputProvider.KeyId(Key.R));
            Assert.IsTrue(ExpeditionInput.ReloadPressed);
            Assert.IsTrue(ControlBindings.TryRebind(ControlBindings.Action.Reload, Key.T));
            Assert.IsFalse(ExpeditionInput.ReloadPressed);
            fake.Press(FakeInputProvider.KeyId(Key.T));
            Assert.IsTrue(ExpeditionInput.ReloadPressed);
        }

        [Test]
        public void APadRebindMovesTheActionToTheNewButton()
        {
            fake.Press(FakeInputProvider.PadId("South"));
            Assert.IsTrue(ExpeditionInput.InteractPressed);
            string[] saved = PadBindings.Pack().Split(',');
            saved[(int)PadBindings.Action.Interact] = "DpadLeft";
            saved[(int)PadBindings.Action.PrevWeapon] = "South";
            PadBindings.Unpack(string.Join(",", saved));
            Assert.AreEqual("DpadLeft", PadBindings.Label(PadBindings.Action.Interact));
            Assert.IsFalse(ExpeditionInput.InteractPressed);
            fake.Press(FakeInputProvider.PadId("DpadLeft"));
            Assert.IsTrue(ExpeditionInput.InteractPressed);
        }

        [Test]
        public void TheThrowFiresOnRelease()
        {
            fake.Press(FakeInputProvider.KeyId(Key.G));
            Assert.IsTrue(ExpeditionInput.ThrowHeld);
            Assert.IsFalse(ExpeditionInput.ThrowReleased);
            fake.EndFrame();
            fake.Release(FakeInputProvider.KeyId(Key.G));
            Assert.IsFalse(ExpeditionInput.ThrowHeld);
            Assert.IsTrue(ExpeditionInput.ThrowReleased);
        }

        [Test]
        public void ThePointerAndScrollComeFromTheProvider()
        {
            fake.Pointer = new Vector2(320f, 200f);
            fake.Scroll = 120f;
            Assert.AreEqual(new Vector2(320f, 200f), ExpeditionInput.Pointer);
            Assert.AreEqual(120f, ExpeditionInput.Scroll);
        }

        [Test]
        public void GameplayCodeReadsDevicesOnlyThroughTheProvider()
        {
            string root = Directory.GetCurrentDirectory();
            string input = File.ReadAllText(Path.Combine(root, "Assets/Scripts/Player/ExpeditionInput.cs"));
            StringAssert.DoesNotContain(".current", input);
            string grid = File.ReadAllText(Path.Combine(root, "Assets/Scripts/Colony/GridBuilder.cs"));
            StringAssert.DoesNotContain("Mouse.current", grid);
            StringAssert.DoesNotContain("Keyboard.current", grid);
        }
    }
}
