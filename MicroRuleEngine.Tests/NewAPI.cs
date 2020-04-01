using System;
using System.Collections.Generic;
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
            Rule rule = Rule.Create("Customer.Country.CountryCode", mreOperator.Equal, "AUS");
    
            MRE engine = new MRE();
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
            Rule rule = Rule.Create("OrderId", mreOperator.Equal, 1);

            MRE engine = new MRE();
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
            Rule rule = Rule.Create("OrderDate", mreOperator.LessThan, "1800-01-01");

            MRE engine = new MRE();
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
            Rule rule = Rule.Create("Total", mreOperator.GreaterThan, 12.00m);

            MRE engine = new MRE();
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
            Rule rule = Rule.Create("Total", mreOperator.Equal, null);

            MRE engine = new MRE();
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
            Rule rule = Rule.Create("Total", mreOperator.Equal, "null");

            MRE engine = new MRE();
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
            Rule rule = Rule.Create("Status", mreOperator.Equal, Status.Open);

            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

        }
        [TestMethod]
        public void EnumAsWord()
        {
            Order order = ExampleUsage.GetOrder();
            order.Total = null;
            Rule rule = Rule.Create("Status", mreOperator.Equal, "Open");

            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

        }


        [TestMethod]
        public void ArrayTest()
        {
            var  array = new ArrayInside();

            Rule rule = Rule.Create("Dbl[1]", mreOperator.Equal, 22.222);

            MRE engine = new MRE();
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

            Rule rule = Rule.Create("Items[1].Cost", mreOperator.Equal, 5.25m);

            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Items[1].Cost = 6.99m;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        class DictTest<T>
        {
	        public Dictionary<T, int> Dict { get; set; }
        }

        [TestMethod]
        public void Dictionary_StringIndex()
        {
	        var objDict = new DictTest<string> { Dict = new Dictionary<string, int>()};
            objDict.Dict.Add("Key", 1234);

	        Rule rule = Rule.Create("Dict['Key']", mreOperator.Equal, 1234);

	        MRE engine = new MRE();
	        var compiledRule = engine.CompileRule<DictTest<string>>(rule);
	        bool passes = compiledRule(objDict);
	        Assert.IsTrue(passes);

	        objDict.Dict["Key"] = 2345;
            passes = compiledRule(objDict);
	        Assert.IsFalse(passes);
        }

        [TestMethod]
        public void Dictionary_IntIndex()
        {
	        var objDict = new DictTest<int> { Dict = new Dictionary<int, int>() };
	        objDict.Dict.Add(111, 1234);

	        Rule rule = Rule.Create("Dict[111]", mreOperator.Equal, 1234);

	        MRE engine = new MRE();
	        var compiledRule = engine.CompileRule<DictTest<int>>(rule);
	        bool passes = compiledRule(objDict);
	        Assert.IsTrue(passes);

	        objDict.Dict[111] = 2345;
	        passes = compiledRule(objDict);
	        Assert.IsFalse(passes);
        }

        [TestMethod]
        public void SelfReferenialTest()
        {
            Order order = ExampleUsage.GetOrder();

            Rule rule = Rule.Create("Items[1].Cost", mreOperator.Equal, "*.Items[0].Cost");

            MRE engine = new MRE();
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
            Rule rule = Rule.Create("Customer.LastName", mreOperator.Equal, "Doe")
                          & (Rule.Create("Customer.FirstName", mreOperator.Equal, "John") | Rule.Create("Customer.FirstName", mreOperator.Equal, "Jane"));

            MRE engine = new MRE();
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

            MRE engine = new MRE();
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

            MRE engine = new MRE();
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
            Rule rule = Rule.Create("Customer.FirstName", mreOperator.IsMatch, @"^[A-Z][aeiou][bcdfghjklmnpqrstvwxyz]{2}$");
 
            MRE engine = new MRE();
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
            Rule rule = Rule.Any("Items", Rule.Create("ItemCode", mreOperator.Equal, "Test"));

            MRE engine = new MRE();
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
            Rule rule = Rule.All("Items", Rule.Create("Cost", mreOperator.GreaterThan, "2.00"));

            MRE engine = new MRE();
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
            MRE engine = new MRE();
            Rule rule1 = Rule.MethodOnChild("Customer.FirstName", "EndsWith", "ohn");
            Rule rule2 = Rule.Create("Customer.Country.CountryCode", mreOperator.Equal, "AUS");

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
