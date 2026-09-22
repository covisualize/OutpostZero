using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class InventoryScreenTests
    {
        [Test]
        public void FilterCyclesThroughEveryCategoryAndBack()
        {
            int filter = PackFilter.All;
            int steps = 0;
            do
            {
                filter = PackFilter.Next(filter);
                steps++;
            } while (filter != PackFilter.All && steps < 50);
            Assert.AreEqual(PackFilter.Count, steps);
            Assert.AreEqual(PackFilter.All, PackFilter.Next(-4));
        }

        [Test]
        public void FilterShowsOnlyItsCategory()
        {
            foreach (ItemCategory category in System.Enum.GetValues(typeof(ItemCategory)))
            {
                Assert.IsTrue(PackFilter.Shows(PackFilter.All, category));
                int tab = (int)category + 1;
                foreach (ItemCategory other in System.Enum.GetValues(typeof(ItemCategory)))
                    Assert.AreEqual(other == category, PackFilter.Shows(tab, other), tab + " " + other);
            }
        }

        [Test]
        public void EveryFilterTabHasAnEnglishAndSpanishLabel()
        {
            for (int tab = 0; tab < PackFilter.Count; tab++)
            {
                string key = PackFilter.Key(tab);
                Assert.AreNotEqual(key, Loc.T(key, "en"), key);
                Assert.AreNotEqual(key, Loc.T(key, "es"), key);
            }
            foreach (var key in new[] { "pack.gear", "pack.tier", "pack.filter", "pack.stow" })
            {
                Assert.AreNotEqual(key, Loc.T(key, "en"), key);
                Assert.AreNotEqual(key, Loc.T(key, "es"), key);
            }
        }

        [Test]
        public void PutMergesIntoAnExistingStackOrAddsOne()
        {
            var held = new[] { new ContainerHold.Stack { Id = "cloth", Count = 2 } };
            var merged = ContainerHold.Put(held, "cloth", 3);
            Assert.AreEqual(1, merged.Length);
            Assert.AreEqual(5, merged[0].Count);
            Assert.AreEqual(2, held[0].Count, "the old array is left alone");
            var grown = ContainerHold.Put(merged, "tape", 1);
            Assert.AreEqual(2, grown.Length);
            Assert.AreEqual("tape", grown[1].Id);
            Assert.AreSame(grown, ContainerHold.Put(grown, "tape", 0));
            Assert.AreEqual(1, ContainerHold.Put(null, "water", 1).Length);
        }

        [Test]
        public void StowThenTakeRoundTripsThroughTheSavedHold()
        {
            var held = ContainerHold.Put(new ContainerHold.Stack[0], "water", 2);
            held = ContainerHold.Put(held, "bandage", 4);
            var restored = ContainerHold.Decode(ContainerHold.Encode(held));
            Assert.AreEqual(ContainerHold.Signature(held), ContainerHold.Signature(restored));
            restored = ContainerHold.Take(restored, "bandage", int.MaxValue, out int moved);
            Assert.AreEqual(4, moved);
            Assert.AreEqual(1, restored.Length);
        }
    }
}
