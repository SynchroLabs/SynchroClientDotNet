using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SynchroCore;

namespace SynchroCoreTest
{
    [TestClass]
    public class ControlWrapperTest
    {
        [TestMethod]
        public void TestGetColorByName()
        {
            var color = ControlWrapper.getColor("NavajoWhite");
            Assert.AreEqual(0xFF, color.a);
            Assert.AreEqual(0xFF, color.r);
            Assert.AreEqual(0xDE, color.g);
            Assert.AreEqual(0xAD, color.b);
        }

        [TestMethod]
        public void TestGetColorByRRGGBB()
        {
            var color = ControlWrapper.getColor("#FFDEAD");
            Assert.AreEqual(0xFF, color.a);
            Assert.AreEqual(0xFF, color.r);
            Assert.AreEqual(0xDE, color.g);
            Assert.AreEqual(0xAD, color.b);
        }

        [TestMethod]
        public void TestGetColorByAARRGGBB()
        {
            var color = ControlWrapper.getColor("#80FFDEAD");
            Assert.AreEqual(0x80, color.a);
            Assert.AreEqual(0xFF, color.r);
            Assert.AreEqual(0xDE, color.g);
            Assert.AreEqual(0xAD, color.b);
        }

        [TestMethod]
        public void TestStarWithStarOnly()
        {
            var stars = ControlWrapper.GetStarCount("*");
            Assert.AreEqual(1, stars);
        }

        [TestMethod]
        public void TestStarWithNumStar()
        {
            var stars = ControlWrapper.GetStarCount("69*");
            Assert.AreEqual(69, stars);
        }

        [TestMethod]
        public void TestStarWithNum()
        {
            var stars = ControlWrapper.GetStarCount("69");
            Assert.AreEqual(0, stars);
        }

        [TestMethod]
        public void TestStarWithEmpty()
        {
            var stars = ControlWrapper.GetStarCount("");
            Assert.AreEqual(0, stars);
        }

        [TestMethod]
        public void TestStarWithNull()
        {
            var stars = ControlWrapper.GetStarCount(null);
            Assert.AreEqual(0, stars);
        }   
    }
}
