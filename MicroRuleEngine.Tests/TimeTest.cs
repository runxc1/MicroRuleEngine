using System;
using MicroRuleEngine.Tests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroRuleEngine.Tests
{
	[TestClass]
	public class TimeTest
	{
		[TestMethod]
		public void Time_InRange_Minutes()
		{
			Order order = ExampleUsage.GetOrder();
			Rule rule = Rule.Create("OrderDate", mreOperator.GreaterThanOrEqual, "#NOW-90M");
			order.OrderDate = DateTime.Now.AddMinutes(-60);

			MRE engine = new MRE();
			var boolMethod = engine.CompileRule<Order>(rule);
			bool passes = boolMethod(order);
			Assert.IsTrue(passes);

		}

		[TestMethod]
		public void Time_OutOfRange_Minutes()
		{
			Order order = ExampleUsage.GetOrder();
			Rule rule = Rule.Create("OrderDate", mreOperator.GreaterThanOrEqual, "#NOW-90M");
			order.OrderDate = DateTime.Now.AddMinutes(-100);

			MRE engine = new MRE();
			var boolMethod = engine.CompileRule<Order>(rule);
			bool passes = boolMethod(order);
			Assert.IsFalse(passes);

		}

		[TestMethod]
		[ExpectedException(typeof(FormatException))]			// TODO: Make throw RuleException
		public void Time_BadTarget()
		{
			Order order = ExampleUsage.GetOrder();
			Rule rule = Rule.Create("OrderId", mreOperator.GreaterThanOrEqual, "#NOW-90M");
			order.OrderDate = DateTime.Now.AddMinutes(-100);

			MRE engine = new MRE();
			var boolMethod = engine.CompileRule<Order>(rule);
			bool passes = boolMethod(order);
			Assert.IsFalse(passes);
		}

		[TestMethod]
		[ExpectedException(typeof(FormatException))]            // TODO: Make throw RuleException
		public void Time_BadDateString()
		{
			Order order = ExampleUsage.GetOrder();
			Rule rule = Rule.Create("OrderDate", mreOperator.GreaterThanOrEqual, "#NOW*90M");
			order.OrderDate = DateTime.Now.AddMinutes(-100);

			MRE engine = new MRE();
			var boolMethod = engine.CompileRule<Order>(rule);
			bool passes = boolMethod(order);
			Assert.IsFalse(passes);

		}

		[TestMethod]
		public void Time_InRange_Days()
		{
			Order order = ExampleUsage.GetOrder();
			Rule rule = Rule.Create("OrderDate", mreOperator.GreaterThanOrEqual, "#NOW-90D");
			order.OrderDate = DateTime.Now.AddDays(-60);

			MRE engine = new MRE();
			var boolMethod = engine.CompileRule<Order>(rule);
			bool passes = boolMethod(order);
			Assert.IsTrue(passes);
		}

		[TestMethod]
		public void Time_OutOfRange_Days()
		{
			Order order = ExampleUsage.GetOrder();
			Rule rule = Rule.Create("OrderDate", mreOperator.GreaterThanOrEqual, "#NOW-90D");
			order.OrderDate = DateTime.Now.AddDays(-100);

			MRE engine = new MRE();
			var boolMethod = engine.CompileRule<Order>(rule);
			bool passes = boolMethod(order);
			Assert.IsFalse(passes);

		}


	}
}
