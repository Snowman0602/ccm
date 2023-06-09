﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CCMEngine;

namespace CCMTests
{
    [TestClass]
    public class PSParserTests
    {
        private class PSTestContext
        {
            public LookAheadLangParser TextParser { get; private set; }
            public PSParser Parser { get; private set; }
            public BlockAnalyzer Analyzer { get; private set; }

            public static PSTestContext NewTestContext(string code)
            {
                var context = new PSTestContext();

                context.TextParser = LookAheadLangParserFactory.CreatePowerShellParser(TestUtil.GetTextStream(code));
                context.Parser = new PSParser(context.TextParser);
                context.Analyzer = new BlockAnalyzer(context.TextParser, context.Parser);

                return context;
            }

            public bool NextIsFunction()
            {
                return this.Parser.NextIsFunction();
            }

            public string NextFunction()
            {
                try
                {
                    this.Parser.AdvanceToNextFunction();

                    return String.Empty;
                }
                catch(CCCParserSuccessException success)
                {
                    return success.Function;
                }
            }
        }

        [TestMethod]
        public void TestNextIsFunction_WithSignatureParameters()
        {
            string code = "function Get-NextFunction([string] $body) { Write-Host $body } ";

            var context = PSTestContext.NewTestContext(code);
            Assert.IsTrue(context.NextIsFunction());
        }

        [TestMethod]
        public void TestNextIsFunction_WithBodyParameters()
        {
            string code = "function Write-Body { param([string] $body) Write-Host $body } ";

            var context = PSTestContext.NewTestContext(code);
            Assert.IsTrue(context.NextIsFunction());
        }

        [TestMethod]
        public void TestAdvanceToFunction()
        {
            string code = "function Get-NextFunction([string] $body) { Write-Host $body } ";

            var context = PSTestContext.NewTestContext(code);
            Assert.AreEqual("Get-NextFunction", context.NextFunction());
        }

        [TestMethod]
        public void TestAdvanceToFunction_WithLineBreaks()
        {
            string code = @"
function Write-HostEx
{
    param
    (
        [string] $Output
    )

    Write-Host $Output
}";

            var context = PSTestContext.NewTestContext(code);
            Assert.AreEqual("Write-HostEx", context.NextFunction());
        }

        [TestMethod]
        public void TestAdvancesStreamToBodyBlock()
        {
            string code = "function Get-NextFunction([string] $body) { Write-Host $body } ";

            var context = PSTestContext.NewTestContext(code);

            Assert.AreEqual("Get-NextFunction", context.NextFunction());
            Assert.AreEqual("{", context.TextParser.PeekNextKeyword());
        }

        [TestMethod]
        public void TestComputesComplexityForFunction()
        {
            string code = @"
{
    param
    (
        [string] $Output
    )

    if ($Output -or $Global:Debug)
    {
        Write-Host $Output
    }
}";

            var context = PSTestContext.NewTestContext(code);

            Assert.AreEqual(2, context.Analyzer.ConsumeBlockCalculateAdditionalComplexity());
        }


    }
}
