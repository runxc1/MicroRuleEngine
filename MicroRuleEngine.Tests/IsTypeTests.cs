using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroRuleEngine.Tests
{
    [TestClass]
    public class IsTypeTests
    {
        [TestMethod]
        public void IsInt_OK()
        {
            var target = new IsTypeClass  { NumAsString = "1234", OtherField = "Hello, World" };

            Rule rule = Rule.IsInteger("NumAsString");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void IsInt_Bad_Float()
        {
            var target = new IsTypeClass { NumAsString = "1234.567", OtherField = "Hello, World" };

            Rule rule = Rule.IsInteger("NumAsString");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void IsInt_Bad_Word()
        {
            var target = new IsTypeClass { NumAsString = "1234.567", OtherField = "Hello, World" };

            Rule rule = Rule.IsInteger("OtherField");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsFalse(passes);
        }
        [TestMethod]
        public void IsFloat_OK_Int()
        {
            var target = new IsTypeClass { NumAsString = "1234", OtherField = "Hello, World" };

            Rule rule = Rule.IsFloat("NumAsString");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void IsFloat_OK_Float()
        {
            var target = new IsTypeClass { NumAsString = "1234.567", OtherField = "Hello, World" };

            Rule rule = Rule.IsFloat("NumAsString");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void IsFloat_Bad_Word()
        {
            var target = new IsTypeClass { NumAsString = "1234.567", OtherField = "Hello, World" };

            Rule rule = Rule.IsFloat("OtherField");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void IsDouble_OK_Int()
        {
            var target = new IsTypeClass { NumAsString = "1234", OtherField = "Hello, World" };

            Rule rule = Rule.IsDouble("NumAsString");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void IsDouble_OK_Double()
        {
            var target = new IsTypeClass { NumAsString = "1234.56789012345", OtherField = "Hello, World" };

            Rule rule = Rule.IsDouble("NumAsString");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void IsDouble_Bad_Word()
        {
            var target = new IsTypeClass { NumAsString = "1234.567", OtherField = "Hello, World" };

            Rule rule = Rule.IsDouble("OtherField");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void IsDecimal_OK_Int()
        {
            var target = new IsTypeClass { NumAsString = "1234", OtherField = "Hello, World" };

            Rule rule = Rule.IsDecimal("NumAsString");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void IsDecimal_OK_Double()
        {
            var target = new IsTypeClass { NumAsString = "1234.56789012345", OtherField = "Hello, World" };

            Rule rule = Rule.IsDecimal("NumAsString");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void IsDecimal_Bad_Word()
        {
            var target = new IsTypeClass { NumAsString = "1234.567", OtherField = "Hello, World" };

            Rule rule = Rule.IsDecimal("OtherField");
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<IsTypeClass>(rule);
            bool passes = compiledRule(target);
            Assert.IsFalse(passes);
        }


    }

    internal class IsTypeClass
    {
        public string NumAsString { get; set; }
        public string OtherField { get; set; }
    }
}