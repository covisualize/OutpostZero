using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class CraftQueueTests
    {
        [Test]
        public void OnlyRecipesThatMakeAThingCanBeQueued()
        {
            Assert.IsTrue(CraftQueue.Orderable("ammo_9mm"));
            Assert.IsTrue(CraftQueue.Orderable("bandage"));
            Assert.IsTrue(CraftQueue.Orderable("cooked_meal"));
            foreach (var id in new[] { "suppressor", "rail", "optic", "extended_mag", "repair_kit", "barricade_kit", "radio_spare", "", null, "made_up" })
                Assert.IsFalse(CraftQueue.Orderable(id), id ?? "null");
        }

        [Test]
        public void TheQueuePacksCapsAndDropsStaleIds()
        {
            var orders = CraftQueue.Unpack("ammo_9mm,optic,bandage,nothing");
            CollectionAssert.AreEqual(new[] { "ammo_9mm", "bandage" }, orders);
            Assert.AreEqual("ammo_9mm,bandage", CraftQueue.Pack(orders));
            Assert.AreEqual("", CraftQueue.Pack(null));
            Assert.AreEqual(0, CraftQueue.Unpack(null).Count);
            var full = CraftQueue.Unpack("bandage,bandage,bandage,bandage,bandage,bandage,bandage,bandage");
            Assert.AreEqual(CraftQueue.Cap, full.Count);
            Assert.IsFalse(CraftQueue.CanAdd(full, "bandage"));
            Assert.IsTrue(CraftQueue.CanAdd(orders, "bandage"));
            Assert.AreEqual("9mm, Bandage", CraftQueue.Line(orders, id => id == "ammo_9mm" ? "9mm" : "Bandage"));
        }

        [Test]
        public void SkilledHandsFinishMoreOrdersAndNobodyWorksWithoutABench()
        {
            Assert.AreEqual(0, CraftQueue.Hands(80f, 6, false));
            Assert.AreEqual(0, CraftQueue.Hands(5f, 6, true));
            Assert.AreEqual(1, CraftQueue.Hands(60f, 1, true));
            Assert.AreEqual(2, CraftQueue.Hands(60f, 4, true));
        }

        [Test]
        public void AnEngineerTakesTheBenchWhenOrdersWait()
        {
            var tinker = new Survivor { id = "a", alive = true, morale = 70f, engineering = 5, trait = "Engineer", combat = 1, scavenge = 1 };
            var camp = new TaskPick.Camp { FoodPerHead = 4, Scrap = 40, Orders = 3, Bench = true };
            Assert.AreEqual(CraftQueue.Task, TaskPick.Choose(tinker, camp));
            camp.Orders = 0;
            Assert.AreNotEqual(CraftQueue.Task, TaskPick.Choose(tinker, camp));
            camp.Orders = 3;
            camp.Bench = false;
            Assert.AreNotEqual(CraftQueue.Task, TaskPick.Choose(tinker, camp));
        }

        [Test]
        public void OrdersSurviveASaveAndHaveNames()
        {
            var data = new SaveGameData { craftOrders = "ammo_9mm,bandage" };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var loaded, out var error), error);
            Assert.AreEqual("ammo_9mm,bandage", loaded.craftOrders);
            Assert.AreEqual("Craft", Loc.Task(CraftQueue.Task, "en"));
            Assert.AreEqual("Fabricar", Loc.Task(CraftQueue.Task, "es"));
            Assert.AreEqual("El banco es mío.", Loc.T("bark.craft", "es"));
        }
    }
}
