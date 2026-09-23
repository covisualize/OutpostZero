using System.IO;
using NUnit.Framework;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class ResultsRetryTests
    {
        [Test]
        public void RetryHeadsForTheNextOpenDistrictOnly()
        {
            Assert.IsTrue(ResultsRetry.Open(true, false, false, false));
            Assert.IsFalse(ResultsRetry.Open(false, false, false, false));
            Assert.IsFalse(ResultsRetry.Open(true, true, false, false));
            Assert.IsFalse(ResultsRetry.Open(true, false, false, true));
            Assert.IsTrue(ResultsRetry.Open(true, true, true, false));
            Assert.IsTrue(ResultsRetry.Open(true, true, true, true));
        }

        [Test]
        public void TheResultsScreenOffersRetryThroughCamp()
        {
            string ui = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "UI", "OutpostInterface.cs"));
            int results = ui.IndexOf("case GameState.ExpeditionResults:");
            int next = ui.IndexOf("case GameState.GameOver:", results);
            Assert.Greater(results, 0);
            string block = ui.Substring(results, next - results);
            StringAssert.Contains("ResultsRetry.Open(", block);
            StringAssert.Contains("GameManager.Instance.HeadOutAgain()", block);
            StringAssert.Contains("FlowStep.Expedition", block);

            string manager = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "Core", "GameManager.cs"));
            int head = manager.IndexOf("public void HeadOutAgain()");
            Assert.Greater(head, 0);
            string body = manager.Substring(head, manager.IndexOf("public void BeginExpedition()", head) - head);
            Assert.Less(body.IndexOf("EnterCamp()"), body.IndexOf("BeginExpedition()"));

            Assert.AreNotEqual("menu.retry", Loc.Raw("menu.retry", "en"));
            Assert.AreNotEqual("menu.retry", Loc.Raw("menu.retry", "es"));
        }
    }
}
