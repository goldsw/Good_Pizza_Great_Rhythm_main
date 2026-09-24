using NUnit.Framework;

namespace GPGR.Charting.Tests
{
    public class JudgementWindowsTests
    {
        [Test]
        public void Classify_UsesFirstMatchingWindow()
        {
            JudgementWindows windows = JudgementWindows.Default;

            Assert.AreEqual(Judgement.Perfect, windows.Classify(0));
            Assert.AreEqual(Judgement.Perfect, windows.Classify(30));
            Assert.AreEqual(Judgement.Perfect, windows.Classify(50));
            Assert.AreEqual(Judgement.Good, windows.Classify(51));
            Assert.AreEqual(Judgement.Good, windows.Classify(100));
            Assert.AreEqual(Judgement.Bad, windows.Classify(101));
            Assert.AreEqual(Judgement.Bad, windows.Classify(160));
            Assert.AreEqual(Judgement.Miss, windows.Classify(161));
        }
    }
}
