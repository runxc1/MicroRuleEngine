MicroRuleEngine is a single file rule engine
============================================

#### Fork Note:
On this fork, I've added a new API, since the original is rather unwieldy.   With the new API, the Rule defined below in `ConditionalLogic()` can be written as:
```csharp
Rule rule = Rule.Create("Customer.LastName", mreOperator.Equal, "Doe")
         & (Rule.Create("Customer.FirstName", mreOperator.Equal, "John") | Rule.Create("Customer.FirstName", mreOperator.Equal, "Jane"));
```
 NewApi.c in the UnitTest Project contains all the original unit tests re-written with the new API.
 
 (end fork note)


A .Net Rule Engine for dynamically evaluating business rules compiled on the fly.  If you have business rules that you don't want to hard code then 
the MicroRuleEngine is your friend.   The rule engine is easy to groc and is only about 2 hundred lines.  Under the covers it creates a Linq expression tree
that is compiled so even if your business rules get pretty large or you run them against thousands of items the performance should still compare nicely with a
hard coded solution.

How To Install It?
------------------
Drop the code file into your app and change it as you wish.

How Do You Use It?
------------------
The best examples of how to use the MicroRuleEngine (MRE) can be found in the Test project included in the Solution.
Below is one of the tests.

```csharp
		[TestMethod]
		public void ChildProperties()
		{
			Order order = this.GetOrder();
			Rule rule = new Rule()
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
```

You'll want to look at the Test project but just to give another snippet here is an example of Conditional logic in a test

```csharp
		[TestMethod]
		public void ConditionalLogic()
		{
			Order order = this.GetOrder();
			Rule rule = new Rule()
			{
				Operator = "AndAlso",
				Rules = new List<Rule>()
				{
					new Rule(){ MemberName = "Customer.LastName", TargetValue = "Doe", Operator = "Equal"},
					new Rule(){ 
						Operator = "Or",
						Rules = new List<Rule>(){
							new Rule(){ MemberName = "Customer.FirstName", TargetValue = "John", Operator = "Equal"},
							new Rule(){ MemberName = "Customer.FirstName", TargetValue = "Jane", Operator = "Equal"}
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
```

How do I store Rules?
---------------------
The Rule Class is just a POCO so you can store your rules as serialized XML, JSON etc.