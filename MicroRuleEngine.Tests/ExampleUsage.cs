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
            var compiledRule = MRE.Instance.Compile<Order>(rule);
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
            var compiledRule = MRE.Instance.Compile<Order>(rule);
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
                                Rules =
                                    new List<Rule>
                                        {
                                            new Rule
                                                {
                                                    MemberName = "Customer.LastName",
                                                    TargetValue = "Doe",
                                                    Operator = "Equal"
                                                },
                                            new Rule
                                                {
                                                    Operator = "Or",
                                                    Rules = new List<Rule>
                                                                {
                                                                    new Rule
                                                                        {
                                                                            MemberName = "Customer.FirstName",
                                                                            TargetValue = "John",
                                                                            Operator = "Equal"
                                                                        },
                                                                    new Rule
                                                                        {
                                                                            MemberName = "Customer.FirstName",
                                                                            TargetValue = "Jane",
                                                                            Operator = "Equal"
                                                                        }
                                                                }
                                                }
                                        }
                            };
            var fakeName = MRE.Instance.Compile<Order>(rule);
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
                                Operator = "HasItem", //The Order Object Contains a method named 'HasItem' that returns true/false
                                Inputs = new List<object> { "Test" }
                            };
            var boolMethod = MRE.Instance.Compile<Order>(rule);
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
                                Operator = "EndsWith", //Regular method that exists on string.. As a note expression methods are not available
                                Inputs = new List<object> { "ohn" }
                            };
            var childPropCheck = MRE.Instance.Compile<Order>(rule);
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
            var childPropCheck = MRE.Instance.Compile<Order>(rule);
            bool passes = childPropCheck(order);
            Assert.IsFalse(passes);
        }

        public void RegexIsMatch() //Had to add a Regex evaluator to make it feel 'Complete'
        {
            Order order = GetOrder();
            Rule rule = new Rule
                            {
                                MemberName = "Customer.FirstName",
                                Operator = "IsMatch",
                                TargetValue = @"^[a-zA-Z0-9]*$"
                            };
            var regexCheck = MRE.Instance.Compile<Order>(rule);
            bool passes = regexCheck(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "--NoName";
            passes = regexCheck(order);
            Assert.IsFalse(passes);
        }

        public Order GetOrder()
        {
            return new Order
                       {
                           OrderId = 1,
                           Customer = Customer.Make("John", "Doe", "AUS"),
                           Items = new List<Item>
                                       {
                                           Item.Make("MM23", 5.25M),
                                           Item.Make("LD45", 5.25M),
                                           Item.Make("Test", 3.33M),
                                       }
                       };
        }
    }
}
