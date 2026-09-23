using System.IO;
using NUnit.Framework;
using OutpostZero.Graphics;
using UnityEngine;

namespace OutpostZero.Tests.EditMode
{
    public class ScreenGradeTests
    {
        [Test]
        public void HurtStartsAtHalfHealthAndPeaksAtZero()
        {
            Assert.AreEqual(0f, ScreenGrade.Hurt(1f));
            Assert.AreEqual(0f, ScreenGrade.Hurt(0.5f));
            Assert.AreEqual(0.5f, ScreenGrade.Hurt(0.25f), 0.001f);
            Assert.AreEqual(1f, ScreenGrade.Hurt(0f));
            Assert.AreEqual(1f, ScreenGrade.Hurt(-2f));
        }

        [Test]
        public void SaturationKeepsTheBaseAndDrainsWithHurtAndDeath()
        {
            Assert.AreEqual(-20f, ScreenGrade.Saturation(0f, 0f));
            Assert.AreEqual(-50f, ScreenGrade.Saturation(1f, 0f), 0.001f);
            Assert.AreEqual(-100f, ScreenGrade.Saturation(1f, 1f));
            Assert.Less(ScreenGrade.Saturation(0f, 0.5f), ScreenGrade.Saturation(0f, 0f));
        }

        [Test]
        public void HurtEdgeTurnsRedAndStaysInRange()
        {
            Assert.AreEqual(Color.black, ScreenGrade.VignetteColor(0f));
            var red = ScreenGrade.VignetteColor(1f);
            Assert.Greater(red.r, red.g * 5f);
            Assert.AreEqual(0.28f + ScreenGrade.HurtEdge, ScreenGrade.Vignette(0.28f, 1f), 0.001f);
            Assert.AreEqual(1f, ScreenGrade.Vignette(0.95f, 1f));
        }

        [Test]
        public void NightLiftsTowardBlueAndDayIsNeutral()
        {
            Assert.AreEqual(new Vector4(1f, 1f, 1f, 0f), ScreenGrade.Lift(0f));
            Assert.AreEqual(new Vector4(1f, 1f, 1f, 0f), ScreenGrade.Gamma(0f));
            Assert.AreEqual(new Vector4(1f, 1f, 1f, 0f), ScreenGrade.Gain(0f));
            var lift = ScreenGrade.Lift(1f);
            Assert.Greater(lift.z, lift.x);
            Assert.Less(ScreenGrade.Gamma(1f).w, 0f);
            Assert.AreEqual(ScreenGrade.Lift(1f), ScreenGrade.Lift(4f));
        }

        [Test]
        public void ShadowsLeanTealAndCampLeansWarm()
        {
            Assert.Greater(ScreenGrade.Shadows.z, ScreenGrade.Shadows.x);
            Assert.Greater(ScreenGrade.Highlights.x, ScreenGrade.Highlights.z);
            ScreenGrade.Warm(false, 1f, 0.96f, 0.9f, out var r0, out var g0, out var b0);
            Assert.AreEqual(0.9f, b0);
            ScreenGrade.Warm(true, 1f, 0.96f, 0.9f, out var r, out var g, out var b);
            Assert.LessOrEqual(r, 1f);
            Assert.Less(b, 0.9f);
            Assert.Greater(r - b, r0 - b0);
        }

        [Test]
        public void RigAddsTheFullStack()
        {
            var source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "Graphics", "PostFxRig.cs"));
            foreach (var part in new[] { "Tonemapping", "ColorAdjustments", "ShadowsMidtonesHighlights", "Bloom", "Vignette", "FilmGrain", "ChromaticAberration", "LiftGammaGain", "DepthOfField" })
                StringAssert.Contains("Add<" + part + ">", source, part);
            Assert.IsFalse(ScreenGrade.AberrationOn(0));
            Assert.IsTrue(ScreenGrade.AberrationOn(1));
        }
    }
}
