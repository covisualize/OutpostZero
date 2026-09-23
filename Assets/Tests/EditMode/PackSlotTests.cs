using System.IO;
using NUnit.Framework;
using OutpostZero.Combat;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class PackSlotTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void SwappingSlotsKeepsTheWeaponInHand()
        {
            Assert.AreEqual(0, WeaponWheel.AfterSwap(1, 1, 0), "the held rifle moves up with its slot");
            Assert.AreEqual(1, WeaponWheel.AfterSwap(0, 1, 0));
            Assert.AreEqual(3, WeaponWheel.AfterSwap(3, 1, 0), "a swap elsewhere leaves the hand alone");
        }

        [Test]
        public void ThePackListsEachSlotWithEquipAndMoveUp()
        {
            string ui = Read("Assets/Scripts/UI/OutpostInterface.cs");
            StringAssert.Contains("slotLabel.text = gear.SlotLine(slot);", ui);
            StringAssert.Contains("Button(Loc.T(\"pack.equip\"), () => gear.SelectWeapon(slot))", ui);
            StringAssert.Contains("Button(Loc.T(\"pack.up\"), () => gear.SwapSlots(slot, slot - 1))", ui);
            string body = Read("Assets/Scripts/Player/PlayerController.cs");
            StringAssert.Contains("activeWeaponIndex = WeaponWheel.AfterSwap(activeWeaponIndex, a, b);", body);
            foreach (string key in new[] { "pack.equip", "pack.up" })
            {
                Assert.AreNotEqual(key, Loc.T(key, "en"));
                Assert.AreNotEqual(Loc.T(key, "en"), Loc.T(key, "es"), key + " has Spanish");
            }
        }
    }
}
