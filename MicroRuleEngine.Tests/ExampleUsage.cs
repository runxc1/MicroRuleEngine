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
        public void ChildProperties()
        {
            Order order = GetOrder();
            Rule rule = new Rule
            {
                MemberName = "Customer.Country.CountryCode",
                Operator = System.Linq.Expressions.ExpressionType.Equal.ToString("g"),
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
                Operator = System.Linq.Expressions.ExpressionType.AndAlso.ToString("g"),
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

        public Order GetOrder()
        {
            Order order = new Order
            {
                OrderId = 1,
                                  Customer = new Customer
                {
                    FirstName = "John",
                    LastName = "Doe",
                                                     Country = new Country
                    {
                        CountryCode = "AUS"
                    }
                },
                                  Items = new List<Item>
                {
                                                  new Item {ItemCode = "MM23", Cost = 5.25M},
                                                  new Item {ItemCode = "LD45", Cost = 5.25M},
                                                  new Item {ItemCode = "Test", Cost = 3.33M},
                                              }
            };
            return order;
        }
    }
}
