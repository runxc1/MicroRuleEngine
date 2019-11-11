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
            Rule rule = Rule.Create("NotAProperty", MreOperator.Equal, 1);

            Mre engine = new Mre();
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

            Mre engine = new Mre();
            var c1123 = engine.CompileRule<Order>(rule);
            bool passes = c1123(order);
            Assert.IsTrue(false);       // should not get here.
        }

        [TestMethod]
        [ExpectedException(typeof(RulesException))]
        public void NotADataRow()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = DataRule.Create<int>("Customer", MreOperator.Equal, "123");

            Mre engine = new Mre();
            var c1123 = engine.CompileRule<Order>(rule);
            bool passes = c1123(order);
            Assert.IsTrue(false);       // should not get here.
        }

        [TestMethod]
        [ExpectedException(typeof(RulesException))]
        public void NotACollection()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.Create("Customer[1]", MreOperator.Equal, "123");

            Mre engine = new Mre();
            var c1123 = engine.CompileRule<Order>(rule);
            bool passes = c1123(order);
            Assert.IsTrue(false);       // should not get here.
        }
    }
}