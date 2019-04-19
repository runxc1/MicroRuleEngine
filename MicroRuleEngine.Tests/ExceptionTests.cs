using MicroRuleEngine.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroRuleEngine.Tests
{
    [TestClass]
    public class ExceptionTests
    {
        [TestMethod]
        [ExpectedException(typeof(RulesException))]
        public void BadPropertyName()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.Create("NotAProperty", mreOperator.Equal, 1);

            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(false);       // should not get here.
        }

        [TestMethod]
        [ExpectedException(typeof(RulesException))]
        public void BadMethod()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.MethodOnChild("Customer.FirstName", "NotAMethod", "ohn");

            MRE engine = new MRE();
            var c1_123 = engine.CompileRule<Order>(rule);
            bool passes = c1_123(order);
            Assert.IsTrue(false);       // should not get here.
        }

        [TestMethod]
        [ExpectedException(typeof(RulesException))]
        public void NotADataRow()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = DataRule.Create<int>("Customer", mreOperator.Equal, "123");

            MRE engine = new MRE();
            var c1_123 = engine.CompileRule<Order>(rule);
            bool passes = c1_123(order);
            Assert.IsTrue(false);       // should not get here.
        }

        [TestMethod]
        [ExpectedException(typeof(RulesException))]
        public void NotACollection()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.Create("Customer[1]", mreOperator.Equal, "123");

            MRE engine = new MRE();
            var c1_123 = engine.CompileRule<Order>(rule);
            bool passes = c1_123(order);
            Assert.IsTrue(false);       // should not get here.
        }
    }
}