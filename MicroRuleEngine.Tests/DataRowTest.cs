using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;

namespace MicroRuleEngine.Tests
{
    [TestClass]
    public class DataRowTests
    {
        [TestMethod]
        public void DataRowTest()
        {
            var dr = GetDataRow();

            Rule rule = DataRule.Create<int>("Column2", mreOperator.Equal, 123) & DataRule.Create<string>("Column1", mreOperator.Equal, "Test");

            MRE engine = new MRE();
            var c1_123 = engine.CompileRule<DataRow>(rule);
            bool passes = c1_123(dr);
            Assert.IsTrue(passes);

            dr["Column2"] = 456;
            dr["Column1"] = "Hello";
            passes = c1_123(dr);
            Assert.IsFalse(passes);
        }

        DataRow GetDataRow()
        {
            var dt = new DataTable();
            dt.Columns.Add("Column1", typeof(string));
            dt.Columns.Add("Column2", typeof(int));
            var dr = dt.NewRow();
            dr.ItemArray = new object[] { "Test", 123 };

            return dr;
        }
    }
}
