using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroRuleEngine.Tests.Models;

namespace MicroRuleEngine.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class NewApi
    {
        [TestMethod]
        public void ChildProperties2()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.Create("Customer.Country.CountryCode", MreOperator.Equal, "AUS");
    
            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Customer.Country.CountryCode = "USA";
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void IntProperties()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.Create("OrderId", MreOperator.Equal, 1);

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.OrderId = 5;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }


        [TestMethod]
        public void DateProperties()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.Create("OrderDate", MreOperator.LessThan, "1800-01-01");

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.OrderDate = new DateTime(1814, 9, 13);
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void DecimalProperties()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.Create("Total", MreOperator.GreaterThan, 12.00m);

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Total = 9.99m;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void NullableProperties()
        {
            Order order = ExampleUsage.GetOrder();
            order.Total = null;
            Rule rule = Rule.Create("Total", MreOperator.Equal, null);

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Total = 9.99m;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void NullAsWord()
        {
            Order order = ExampleUsage.GetOrder();
            order.Total = null;
            Rule rule = Rule.Create("Total", MreOperator.Equal, "null");

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Total = 9.99m;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void EnumProperties()
        {
            Order order = ExampleUsage.GetOrder();
            order.Total = null;
            Rule rule = Rule.Create("Status", MreOperator.Equal, Status.Open);

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

        }
        [TestMethod]
        public void EnumAsWord()
        {
            Order order = ExampleUsage.GetOrder();
            order.Total = null;
            Rule rule = Rule.Create("Status", MreOperator.Equal, "Open");

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

        }


        [TestMethod]
        public void ArrayTest()
        {
            var  array = new ArrayInside();

            Rule rule = Rule.Create("Dbl[1]", MreOperator.Equal, 22.222);

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<ArrayInside>(rule);
            bool passes = compiledRule(array);
            Assert.IsTrue(passes);

            array.Dbl[1] = .0001;
            passes = compiledRule(array);
            Assert.IsFalse(passes);
        }

        class ArrayInside
        {
            public double[] Dbl { get; }= {1.111, 22.222, 333.333};
        }

        [TestMethod]
        public void ListTest()
        {
            Order order = ExampleUsage.GetOrder();

            Rule rule = Rule.Create("Items[1].Cost", MreOperator.Equal, 5.25m);

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Items[1].Cost = 6.99m;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void SelfReferenialTest()
        {
            Order order = ExampleUsage.GetOrder();

            Rule rule = Rule.Create("Items[1].Cost", MreOperator.Equal, "*.Items[0].Cost");

            Mre engine = new Mre();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Items[1].Cost = 6.99m;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void ConditionalLogic2()
        {
            Order order = ExampleUsage.GetOrder();
            Rule rule = Rule.Create("Customer.LastName", MreOperator.Equal, "Doe")
                          & (Rule.Create("Customer.FirstName", MreOperator.Equal, "John") | Rule.Create("Customer.FirstName", MreOperator.Equal, "Jane"));

            Mre engine = new Mre();
            var fakeName = engine.CompileRule<Order>(rule);
            bool passes = fakeName(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "Philip";
            passes = fakeName(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void BooleanMethods2()
        {
            Order order = ExampleUsage.GetOrder();

            //The Order Object Contains a method named 'HasItem(string itemCode)' that returns true/false
            Rule rule = Rule.Method("HasItem", "Test");

            Mre engine = new Mre();
            var boolMethod = engine.CompileRule<Order>(rule);
            bool passes = boolMethod(order);
            Assert.IsTrue(passes);

            var item = order.Items.First(x => x.ItemCode == "Test");
            item.ItemCode = "Changed";
            passes = boolMethod(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void ChildPropertyBooleanMethods2()
        {
            Order order = ExampleUsage.GetOrder();
            //Regular method that exists on string.. As a note expression methods are not available
            Rule rule = Rule.MethodOnChild("Customer.FirstName", "EndsWith", "ohn");

            Mre engine = new Mre();
            var childPropCheck = engine.CompileRule<Order>(rule);
            bool passes = childPropCheck(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "jane";
            passes = childPropCheck(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void RegexIsMatch2()//Had to add a Regex evaluator to make it feel 'Complete'
        {
            Order order = ExampleUsage.GetOrder();
            // Regex = Capital letter, vowel, then two constanants  ("John" passes, "Jane" fails)
            Rule rule = Rule.Create("Customer.FirstName", MreOperator.IsMatch, @"^[A-Z][aeiou][bcdfghjklmnpqrstvwxyz]{2}$");
 
            Mre engine = new Mre();
            var regexCheck = engine.CompileRule<Order>(rule);
            bool passes = regexCheck(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "Jane";
            passes = regexCheck(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void AnyOperator()
        {
            Order order = ExampleUsage.GetOrder();
            //order.Items.Any(a => a.ItemCode == "test")
            Rule rule = Rule.Any("Items", Rule.Create("ItemCode", MreOperator.Equal, "Test"));

            Mre engine = new Mre();
            var boolMethod = engine.CompileRule<Order>(rule);
            bool passes = boolMethod(order);
            Assert.IsTrue(passes);

            var item = order.Items.First(x => x.ItemCode == "Test");
            item.ItemCode = "Changed";
            passes = boolMethod(order);
            Assert.IsFalse(passes);
        }


        [TestMethod]
        public void AllOperator()
        {
            Order order = ExampleUsage.GetOrder();
            //order.Items.All(a => a.Cost > 2.00m)
            Rule rule = Rule.All("Items", Rule.Create("Cost", MreOperator.GreaterThan, "2.00"));

            Mre engine = new Mre();
            var boolMethod = engine.CompileRule<Order>(rule);
            bool passes = boolMethod(order);
            Assert.IsTrue(passes);

            var item = order.Items.First(x => x.ItemCode == "Test");
            item.Cost = 1.99m;
            passes = boolMethod(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void Prebuild2()
        {
            Mre engine = new Mre();
            Rule rule1 = Rule.MethodOnChild("Customer.FirstName", "EndsWith", "ohn");
            Rule rule2 = Rule.Create("Customer.Country.CountryCode", MreOperator.Equal, "AUS");

            var endsWithOhn = engine.CompileRule<Order>(rule1);
            var inAus = engine.CompileRule<Order>(rule2);

            Order order = ExampleUsage.GetOrder();

            int reps = 1000;
            for (int i = 0; i < reps; ++i)
            {
                bool passes = endsWithOhn(order);
                Assert.IsTrue(passes);

                passes = inAus(order);
                Assert.IsTrue(passes);
            }
        }



    }
}
