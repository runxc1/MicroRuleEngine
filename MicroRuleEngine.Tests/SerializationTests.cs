using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.Serialization;
using System.IO;
using MicroRuleEngine.Tests.Models;
using System.Xml;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MicroRuleEngine.Tests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void XmlSerialization()
        {
            var serializer = new DataContractSerializer(typeof(Rule));
            string text;

            using (var writer = new StringWriter())
            {
                Rule rule = Rule.Create("Customer.LastName", mreOperator.Equal, "Doe")
                            & (Rule.Create("Customer.FirstName", mreOperator.Equal, "John")
                               | Rule.Create("Customer.FirstName", mreOperator.Equal, "Jane"));

                using (var xw = XmlWriter.Create(writer))
                    serializer.WriteObject(xw, rule);
                text = writer.ToString();
            }

            Rule newRule; // add breakpoint here, to view XML in text.

            using (var reader = new StringReader(text))
            using (var xr = XmlReader.Create(reader))
            {
                newRule = (Rule)serializer.ReadObject(xr);
            }

            var order = ExampleUsage.GetOrder();

            MRE engine = new MRE();
            var fakeName = engine.CompileRule<Order>(newRule);
            bool passes = fakeName(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "Philip";
            passes = fakeName(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void JsonSerialization()
        {

            var serializer = new DataContractJsonSerializer(typeof(Rule));

            string text;

            using (var stream1 = new MemoryStream())
            {
                Rule rule = Rule.Create("Customer.LastName", mreOperator.Equal, "Doe")
                            & (Rule.Create("Customer.FirstName", mreOperator.Equal, "John") |
                               Rule.Create("Customer.FirstName", mreOperator.Equal, "Jane"))
                               & Rule.Create("Items[1].Cost", mreOperator.Equal, 5.25m);

                serializer.WriteObject(stream1, rule);

                stream1.Position = 0;
                var sr = new StreamReader(stream1);
                text = sr.ReadToEnd();
            }

            Rule newRule; // add breakpoint here, to view JSON in text.

            using (var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                newRule = (Rule)serializer.ReadObject(stream2);
            }

            var order = ExampleUsage.GetOrder();

            MRE engine = new MRE();
            var fakeName = engine.CompileRule<Order>(newRule);
            bool passes = fakeName(order);
            Assert.IsTrue(passes);

            order.Customer.FirstName = "Philip";
            passes = fakeName(order);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void JsonVisualizationTest()
        {
            var order = ExampleUsage.GetOrder();
            var members = MRE.Member.GetFields(order.GetType());
            var serializer = new DataContractJsonSerializer(members.GetType());

            string text;

            using (var stream1 = new MemoryStream())
            {

                serializer.WriteObject(stream1, members);

                stream1.Position = 0;
                var sr = new StreamReader(stream1);
                text = sr.ReadToEnd();
            }
            Assert.IsTrue(text.Length > 100);
        }
    }
}
