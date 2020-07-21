using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroRuleEngine.Tests.Models;

namespace MicroRuleEngine.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class ExampleUsage
    {
        [TestMethod]
        public void ChildPropertiesOfNull()
        {
            Order order = GetOrder();
            order.Customer = null;
            Rule rule = new Rule
            {
                MemberName = "Customer.Country.CountryCode",
                Operator = ExpressionType.Equal.ToString("g"),
                TargetValue = "AUS"
            };
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void ChildProperties()
        {
            Order order = GetOrder();
            Rule rule = new Rule
            {
                MemberName = "Customer.Country.CountryCode",
                Operator = ExpressionType.Equal.ToString("g"),
                TargetValue = "AUS"
            };
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Customer.Country.CountryCode = "USA";
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]

        public void ConditionalLogic()
        {
            Order order = GetOrder();
            Rule rule = new Rule
            {
                Operator = ExpressionType.AndAlso.ToString("g"),
                                Rules = new List<Rule>
                            {
                    new Rule { MemberName = "Customer.LastName", TargetValue = "Doe", Operator = "Equal"},
                    new Rule
                {
                        Operator = "Or",
                        Rules = new List<Rule>
                                    {
                            new Rule { MemberName = "Customer.FirstName", TargetValue = "John", Operator = "Equal"},
                            new Rule { MemberName = "Customer.FirstName", TargetValue = "Jane", Operator = "Equal"}
                        }
                    }
                }
            };
            MRE engine = new MRE();
            var fakeName = engine.CompileRule<Order>(rule);
            bool passes = fakeName(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "Philip";
            passes = fakeName(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void BooleanMethods()
        {
            Order order = GetOrder();
            Rule rule = new Rule
            {
                Operator = "HasItem",//The Order Object Contains a method named 'HasItem' that returns true/false
                Inputs = new List<object> { "Test" }
            };
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
        public void BooleanMethods_ByType()
        {
            Order order = GetOrder();
            Rule rule = new Rule
            {
                Operator = "HasItem",//The Order Object Contains a method named 'HasItem' that returns true/false
                Inputs = new List<object> { "Test" }
            };
            MRE engine = new MRE();

            var boolMethod = engine.CompileRule(typeof(Order), rule);
            bool passes =(bool)  boolMethod.DynamicInvoke(order);
            Assert.IsTrue(passes);

            var item = order.Items.First(x => x.ItemCode == "Test");
            item.ItemCode = "Changed";
            passes = (bool)boolMethod.DynamicInvoke(order);
            Assert.IsFalse(passes);
        }

         [TestMethod]
        public void AnyOperator()
        {
            Order order = GetOrder();
            //order.Items.Any(a => a.ItemCode == "test")
            Rule rule = new Rule
            {
                MemberName = "Items",// The array property
                Operator = "Any",
                Rules = new[]
                {
                     new Rule
                     {
                         MemberName = "ItemCode", // the property in the above array item
                        Operator = "Equal",
                         TargetValue = "Test",
                     }
                 }
            };
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
         public void ChildPropertyBooleanMethods()
        {
            Order order = GetOrder();
            Rule rule = new Rule
            { 
                MemberName = "Customer.FirstName",
                Operator = "EndsWith",//Regular method that exists on string.. As a note expression methods are not available
                Inputs = new List<object> { "ohn" }
            };
            MRE engine = new MRE();
            var childPropCheck = engine.CompileRule<Order>(rule);
            bool passes = childPropCheck(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "jane";
            passes = childPropCheck(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
         public void ChildPropertyOfNullBooleanMethods()
         {
             Order order = GetOrder();
             order.Customer = null;
             Rule rule = new Rule
             {
                 MemberName = "Customer.FirstName",
                 Operator = "EndsWith", //Regular method that exists on string.. As a note expression methods are not available
                 Inputs = new List<object> { "ohn" }
             };
            MRE engine = new MRE();
            var childPropCheck = engine.CompileRule<Order>(rule);
             bool passes = childPropCheck(order);
             Assert.IsFalse(passes);
         }
        [TestMethod]
        public void RegexIsMatch()//Had to add a Regex evaluator to make it feel 'Complete'
        {
            Order order = GetOrder();
            Rule rule = new Rule
            {
                MemberName = "Customer.FirstName",
                Operator = "IsMatch",
                TargetValue = @"^[a-zA-Z0-9]*$"
            };
            MRE engine = new MRE();
            var regexCheck = engine.CompileRule<Order>(rule);
            bool passes = regexCheck(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "--NoName";
            passes = regexCheck(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void BareString()
        {
            var rule = new Rule()
            {
                Operator = "StartsWith",
                Inputs = new[] { "FDX" }
            };

            var engine = new MRE();
            var childPropCheck = engine.CompileRule<string>(rule);
            var passes = childPropCheck("FDX 123456");
            Assert.IsTrue(passes);


            passes = childPropCheck("BOB 123456");
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void IsInInput_SingleValue()
        {
            var value = "hello";

            var rule = new Rule()
            {
                Operator = "IsInInput",
                Inputs = new List<string> { "hello" }
            };

            var mre = new MRE();

            var ruleFunc = mre.CompileRule<string>(rule);

            Assert.IsTrue(ruleFunc(value));
        }

        [TestMethod]
        public void IsInInput_MultiValue()
        {
            var value = "hello";

            var rule = new Rule()
            {
                Operator = "IsInInput",
                Inputs = new List<string> { "hello", "World" }
            };

            var mre = new MRE();

            var ruleFunc = mre.CompileRule<string>(rule);

            Assert.IsTrue(ruleFunc(value));
        }

        [TestMethod]
        public void IsInInput_NoExactMatch()
        {
            var value = "world";

            var rule = new Rule()
            {
                Operator = "IsInInput",
                Inputs = new List<string> { "hello", "World" }
            };

            var mre = new MRE();

            var ruleFunc = mre.CompileRule<string>(rule);

            Assert.IsFalse(ruleFunc(value));
        }

        [TestMethod]
        public void MemberEqualsMember()
        {
            var testObj = new MemberOperaterMemberTestObject()
            {
                Source = "bob",
                Target = "bob"
            };

            var rule = new Rule
            {
                MemberName = "Source",
                Operator = "Equal",
                TargetValue = "*.Target"
            };

            var mre = new MRE();

            var func = mre.CompileRule<MemberOperaterMemberTestObject>(rule);

            Assert.IsTrue(func(testObj));

            testObj.Target = "notBob";

            Assert.IsFalse(func(testObj));
        }

        public static Order GetOrder()
        {
            Order order = new Order()
            {
                OrderId = 1,
                Customer = new Customer()
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Country = new Country()
                    {
                        CountryCode = "AUS"
                    }
                },
                Items = new List<Item>()
                {
                    new Item { ItemCode = "MM23", Cost=5.25M},
                    new Item { ItemCode = "LD45", Cost=5.25M},
                    new Item { ItemCode = "Test", Cost=3.33M},
                },
                Codes = new List<int>()
                {
                    555,
                    321,
                    243
                },
                Total = 13.83m,
                OrderDate = new DateTime(1776, 7, 4),
                Status = Status.Open

            };
            return order;
        }
    }
}
