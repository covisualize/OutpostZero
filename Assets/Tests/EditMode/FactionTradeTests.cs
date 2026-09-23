using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Items;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class FactionTradeTests
    {
        [Test]
        public void TrustOpensEachFactionsPremiumStock()
        {
            foreach (var id in CaravanBook.Ids)
            {
                string premium = CaravanBook.Premium(id);
                Assert.IsNotNull(ItemCatalog.Find(premium), premium);
                CollectionAssert.DoesNotContain(CaravanBook.Stock(id, CaravanBook.Trusted - 1), premium, id);
                CollectionAssert.Contains(CaravanBook.Stock(id, CaravanBook.Trusted), premium, id);
                Assert.AreEqual(CaravanBook.Stock(id).Length + 1, CaravanBook.Stock(id, 100).Length, id);
                foreach (var item in CaravanBook.Stock(id, 100)) Assert.IsNotNull(ItemCatalog.Find(item), id + " sells " + item);
            }
        }

        [Test]
        public void LeadershipHagglesUpToTenPercent()
        {
            Assert.AreEqual(14, CaravanBook.Price("medkit", 0, 0));
            Assert.AreEqual(13, CaravanBook.Price("medkit", 0, 5));
            Assert.AreEqual(CaravanBook.Price("medkit", 0, true), CaravanBook.Price("medkit", 0, 5));
            Assert.AreEqual(13, CaravanBook.Price("medkit", 0, 10));
            Assert.AreEqual(CaravanBook.Price("antibiotics", 0, 10), CaravanBook.Price("antibiotics", 0, 40));
            Assert.Less(CaravanBook.Price("antibiotics", 0, 10), CaravanBook.Price("antibiotics", 0, 0));
            Assert.Less(CaravanBook.Price("medkit", 100, 0), CaravanBook.Price("medkit", -100, 0));
        }

        [Test]
        public void BuyBackPaysHalfAndRisesWithStanding()
        {
            Assert.AreEqual(2, CaravanBook.Offer("bandage", 0));
            Assert.AreEqual(7, CaravanBook.Offer("medkit", 0));
            Assert.Greater(CaravanBook.Offer("medkit", 100), CaravanBook.Offer("medkit", -100));
            foreach (var id in new[] { "medkit", "bandage", "antibiotics", "pipe_bomb", "ammo_rifle" })
                Assert.Less(CaravanBook.Offer(id, 100), CaravanBook.Price(id, 100, 10), id + " can be flipped for profit");
        }

        [Test]
        public void CraftedGearHasABarterValue()
        {
            Assert.IsTrue(CaravanBook.Sellable("molotov"));
            Assert.IsTrue(CaravanBook.Sellable("canned_food"));
            Assert.IsFalse(CaravanBook.Sellable("scrap"));
            Assert.IsFalse(CaravanBook.Sellable("print_radio"));
            Assert.IsFalse(CaravanBook.Sellable(""));
            Assert.AreEqual(CraftBill.Value("molotov"), CaravanBook.BasePrice("molotov"));
            Assert.AreEqual(6, CaravanBook.BasePrice("street_bottle_unknown"));
            Assert.AreEqual("Sell", Loc.T("stall.sell_item", "en"));
            Assert.AreEqual("(de confianza)", Loc.T("stall.trusted", "es"));
        }
    }
}
