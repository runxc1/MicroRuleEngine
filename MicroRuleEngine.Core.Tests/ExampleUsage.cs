﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MicroRuleEngine.Core.Tests.Models;
using Newtonsoft.Json;

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
        public void GetAllField()
        {
            Order order = GetOrder();

            var type = order.GetType();
            var members = MRE.Member.GetFields(type);
            Assert.IsTrue(
                members.Where(x=> x.Name == "Customer.Country.CountryCode" && x.PossibleOperators.Any(y=> y.Name == "StartsWith")).Any()
            );
        }

        [TestMethod]
        public void CoerceMethod()
        {
            Order order = GetOrder();
            Rule rule = new Rule
            {
                MemberName = "Codes",
                Operator = "Contains",
                TargetValue = "243",
                Inputs = new List<object>() { "243" }
            };
            MRE engine = new MRE();
            var compiledRule = engine.CompileRule<Order>(rule);
            bool passes = compiledRule(order);
            Assert.IsTrue(passes);

            order.Codes.Clear();
            passes = compiledRule(order);
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

        public class OrderParent
        {
            public Order PlacedOrder { get; set; }
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

        [TestMethod]
        public void EnumerableFilterAndAggregation()
        {
            


            Order order = GetOrder();

            if (order.Items.Where(x => x.ItemCode.StartsWith("M"))
                     .Sum(x => x.Cost) > 6)
            {

            }


            Rule rule = new Rule
                        {
                            MemberName = "Items",
                            EnumerableFilter = new Rule
                                               {
                                                   MemberName = "ItemCode",
                                                   Operator   = "StartsWith",
                                                   Inputs     = new []{"M"}
                                               },
                            EnumerableValueExpression = new Selector
                                                        {
                                                            MemberName = "Cost",
                                                            Operator   = "Sum"
                                                        },
                            Operator    = "GreaterThan",
                            TargetValue = 5
                        };

            MRE  engine       = new MRE();
            var  compiledRule = engine.CompileRule<Order>(rule);
            bool passes       = compiledRule(order);
            Assert.IsTrue(passes);

            order.Items[0].Cost = 4m;
            passes              = compiledRule(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void CountAggregation()
        {

            Order order = GetOrder();

            Rule rule = new Rule
                        {
                            MemberName = "Items",
                            EnumerableValueExpression = new Selector
                                                        {
                                                            Operator   = "Count"
                                                        },
                            Operator    = "GreaterThan",
                            TargetValue = 3
                        };

            MRE  engine       = new MRE();
            var  compiledRule = engine.CompileRule<Order>(rule);
            bool passes       = compiledRule(order);
            Assert.IsFalse(passes);

            order.Items.Add(new Item());
            order.Items.Add(new Item());

            passes = compiledRule(order);

            Assert.IsTrue(passes);

        }

        [TestMethod]
        public void EnumerableAggregationOnChild()
        {
            


            Order order = GetOrder();

            var orderParent = new OrderParent() {PlacedOrder = order};


            Rule rule = new Rule
                        {
                            MemberName = "PlacedOrder.Items",
                            EnumerableValueExpression = new Selector
                                                        {
                                                            Operator = "Count"
                                                        },
                            Operator    = "GreaterThan",
                            TargetValue = 3
                        };

            MRE  engine       = new MRE();
            var  compiledRule = engine.CompileRule<OrderParent>(rule);
            bool passes       = compiledRule(orderParent);
            Assert.IsFalse(passes);

            order.Items.Add(new Item());
            order.Items.Add(new Item());

            passes = compiledRule(orderParent);

            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void SerializeThenDeserialize()
        {
            


            Order order = GetOrder();

            var orderParent = new OrderParent() {PlacedOrder = order};


            Rule rule = new Rule
                        {
                            MemberName = "PlacedOrder.Items",
                            EnumerableValueExpression = new Selector
                                                        {
                                                            Operator = "Count"
                                                        },
                            Operator    = "GreaterThan",
                            TargetValue = 3
                        };

            var jsonString = JsonConvert.SerializeObject(rule);

            var deserializedRule = JsonConvert.DeserializeObject<Rule>(jsonString);



            MRE  engine       = new MRE();
            var  compiledRule = engine.CompileRule<OrderParent>(deserializedRule);
            bool passes       = compiledRule(orderParent);
            Assert.IsFalse(passes);

            order.Items.Add(new Item());
            order.Items.Add(new Item());

            passes = compiledRule(orderParent);

            Assert.IsTrue(passes);
        }

        [TestMethod]
        public void SerializeThenDeserializeComplexRules()
        {
            


            Order order = GetOrder();

            var orderParent = new OrderParent() {PlacedOrder = order};


            Rule rule = new Rule
                        {
                            Operator = "AndAlso",
                            Rules = new List<Rule>
                                    {

                                        new Rule
                                        {
                                            MemberName = "PlacedOrder.Items",
                                            EnumerableValueExpression = new Selector
                                                                        {
                                                                            Operator = "Count"
                                                                        },
                                            Operator    = "GreaterThan",
                                            TargetValue = 3
                                        },

                                        new Rule
                                        {
                                            MemberName = "PlacedOrder.Items",
                                            EnumerableValueExpression = new Selector
                                                                        {
                                                                            MemberName = "Cost",
                                                                            Operator   = "Sum"
                                                                        },
                                            Operator    = "GreaterThan",
                                            TargetValue = 5
                                        }
                                    }
                        };

            var jsonString = JsonConvert.SerializeObject(rule);

            var deserializedRule = JsonConvert.DeserializeObject<Rule>(jsonString);



            MRE  engine       = new MRE();
            var  compiledRule = engine.CompileRule<OrderParent>(deserializedRule);
            bool passes       = compiledRule(orderParent);
            Assert.IsFalse(passes);

            order.Items.Add(new Item());
            order.Items.Add(new Item());

            passes = compiledRule(orderParent);

            Assert.IsTrue(passes);
        }
    }
}
