MicroRuleEngine is a single file rule engine
============================================

A `.Net` Rule Engine for **dynamically** evaluating business rules compiled on the fly.  If you have business rules that you don't want to hard code then the `MicroRuleEngine` is your friend.   The rule engine is easy to groc and is only about 200 lines of code.  Under the covers it creates a `Linq` expression tree that is compiled so even if your business rules get pretty large or you run them against thousands of items the performance should still compare nicely with a hard coded solution.

How To Install It?
------------------
Drop the code file into your app and change it as you wish.

How Do You Use It?
------------------
The best examples of how to use the `MicroRuleEngine (MRE)` can be found in the Test project included in the Solution.

One of the tests:
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

What Kinds of Rules can I express
--------------------------------
In addition to comparative operators such as `Equals`, `GreaterThan`, `LessThan` etc.   You can also call methods on the object that return a `boolean` value such as `Contains` or `StartsWith` on a string. In addition to comparative operators, additional operators such as `IsMatch` or `IsInteger` have been added and demonstrates how you could edit the code to add your own operator(s). Rules can also be `AND`'d or `OR`'d together:
```csharp

	Rule rule =
		Rule.Create("Customer.LastName", "Contains", "Do")
		& (
			Rule.Create("Customer.FirstName", "StartsWith", "Jo")
			| Rule.Create("Customer.FirstName", "StartsWith", "Bob")
		);
```

You can reference member properties which are `Arrays` or `List<>` by their index:
```csharp
	Rule rule = Rule.Create("Items[1].Cost", mreOperator.GreaterThanOrEqual, "5.25");
```

Similarly, you can reference element of a string- or integer-keyed dictionary:
```csharp
	Rule rule = Rule.Create("Items['myKey'].Cost", mreOperator.GreaterThanOrEqual, "5.25");
```


You can also compare an object to itself indicated by the `*.` at the beginning of the `TargetValue`:
```csharp
	Rule rule = Rule.Create("Items[1].Cost", mreOperator.Equal, "*.Items[0].Cost");
```

There are a lot of examples in the test cases but, here is another snippet demonstrating nested `OR` logic:
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
				new Rule() { MemberName = "Customer.LastName", TargetValue = "Doe", Operator = "Equal"},
				new Rule() { 
					Operator = "Or",
					Rules = new List<Rule>() {
						new Rule(){ MemberName = "Customer.FirstName", TargetValue = "John", Operator = "Equal"},
						new Rule(){ MemberName = "Customer.FirstName", TargetValue = "Judy", Operator = "Equal"}
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

If you need to run your comparison against an ADO.NET DataSet you can also do that as well:
```csharp
	var dr = GetDataRow();
	// (int) dr["Column2"] == 123 &&  (string) dr["Column1"] == "Test"
	Rule rule = DataRule.Create<int>("Column2", mreOperator.Equal, "123") & DataRule.Create<string>("Column1", mreOperator.Equal, "Test");
```
  
  

####  #NOW and time-based rules.
You can test a property for a time range from the current time, using the special case `#NOW` keyword.   The member must be a `DataTime` or `DateTime?`,
and the target value must be a string in the form :`#NOW+90D`   (The sign can be plus or minus, but must be given.  The Suffix can be
'S' for Seconds, `M` for Minutes, `H` for Hours, `D` for Days, or `Y` for Years.   The number must be an integer.)

examples:

`		Rule rule = Rule.Create("OrderDate", mreOperator.GreaterThanOrEqual, "#NOW-90M");`

`OrderDate` must be within the last 90 minutes.

`		Rule rule = Rule.Create("ExpirationDate", mreOperator.LessThanOrEqual, "#NOW+1Y");`

`ExpirationDate` must be within the next year.




How Can I Store Rules?
---------------------
The `Rule` Class is just a **POCO** so you can store your rules as serialized `XML`, `JSON`, etc.

#### Forked many times and now updated to pull in a lot of the great work done by jamescurran, nazimkov and others that help improve the API
