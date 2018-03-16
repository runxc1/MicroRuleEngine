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
            Order order = GetOrder();
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
            Order order = GetOrder();
            Rule rule = Rule.Create("OrderId", mreOperator.Equal, "1");

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
            Order order = GetOrder();
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
            Order order = GetOrder();
            Rule rule = Rule.Create("Total", mreOperator.GreaterThan, "12.00");

            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Total = 9.99m;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }


        [TestMethod, Ignore]
        public void Array_Test()
        {
            Order order = GetOrder();
            Rule rule = Rule.Create("Items[0].Cost", mreOperator.Equal, "5.25");

            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Items[0].Cost = 6.99m;
            passes = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void ConditionalLogic2()
        {
            Order order = GetOrder();
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
            Order order = GetOrder();

            //The Order Object Contains a method named 'HasItem' that returns true/false
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
            Order order = GetOrder();
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
            Order order = GetOrder();
            // Regex = Capital letter, vowel, then two constanants 
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
        public void Prebuild2()
        {
            MRE engine = new MRE();
            Rule rule1 = Rule.MethodOnChild("Customer.FirstName", "EndsWith", "ohn");
            Rule rule2 = Rule.Create("Customer.Country.CountryCode", mreOperator.Equal, "AUS");

            var endsWithOhn = engine.CompileRule<Order>(rule1);
            var inAus = engine.CompileRule<Order>(rule2);

            Order order = GetOrder();

            int reps = 1000;
            for (int i = 0; i < reps; ++i)
            {
                bool passes = endsWithOhn(order);
                Assert.IsTrue(passes);

                passes = inAus(order);
                Assert.IsTrue(passes);
            }
        }



        public Order GetOrder()
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
                Total = 13.83m,
                OrderDate = new DateTime(1776, 7, 4),
                Items = new List<Item>(){
                    new Item(){ ItemCode = "MM23", Cost=5.25M},
                    new Item(){ ItemCode = "LD45", Cost=5.25M},
                    new Item(){ ItemCode = "Test", Cost=3.33M},
                }
            };
            return order;
        }
    }
}
