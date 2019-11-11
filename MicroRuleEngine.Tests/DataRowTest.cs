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
            // (int) dr["Column2"] == 123 &&  (string) dr["Column1"] == "Test"
            Rule rule = DataRule.Create<int>("Column2", MreOperator.Equal, "123") & DataRule.Create<string>("Column1", MreOperator.Equal, "Test");

            Mre engine = new Mre();
            var c1123 = engine.CompileRule<DataRow>(rule);
            bool passes = c1123(dr);
            Assert.IsTrue(passes);

            dr["Column2"] = 456;
            dr["Column1"] = "Hello";
            passes = c1123(dr);
            Assert.IsFalse(passes);
        }



        [TestMethod]
        public void DataRowTest_RuntimeType()
        {
            var dt = CreateEmptyDataTable();

            Rule rule = DataRule.Create("Column2", MreOperator.Equal, 123, dt.Columns["Column2"].DataType);

            Mre engine = new Mre();
            var c1123 = engine.CompileRule<DataRow>(rule);

            var dr = GetDataRow(dt);
            bool passes = c1123(dr);
            Assert.IsTrue(passes);

            dr["Column2"] = 456;
            dr["Column1"] = "Hello";
            passes = c1123(dr);
            Assert.IsFalse(passes);
        }

        [TestMethod]
        public void DataRowTest_DBNull()
        {
            Rule rule = DataRule.Create<int?>("Column2", MreOperator.Equal, (int?) null) &
                        DataRule.Create<string>("Column1", MreOperator.Equal, null);

            Mre engine = new Mre();
            var c1123 = engine.CompileRule<DataRow>(rule);

            var dt = CreateEmptyDataTable();
            var dr = GetDataRowDbNull(dt);

            bool passes = c1123(dr);
            Assert.IsTrue(passes);

            dr["Column2"] = 456;
            dr["Column1"] = "Hello";
            passes = c1123(dr);
            Assert.IsFalse(passes);
        }


        [TestMethod]
        public void DataRowTest_OldSytntax()
        {
            var dr = GetDataRow();

            Rule rule = new DataRule
            {
                Type = "System.Int32",
                TargetValue = "123",
                Operator = "Equal",
                MemberName = "Column2"
            };

            Mre engine = new Mre();
            var c1123 = engine.CompileRule<DataRow>(rule);
            bool passes = c1123(dr);
            Assert.IsTrue(passes);

            dr["Column2"] = 456;
            dr["Column1"] = "Hello";
            passes = c1123(dr);
            Assert.IsFalse(passes);
        }

        DataTable CreateEmptyDataTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("Column1", typeof(string));
            dt.Columns.Add("Column2", typeof(int));
            dt.Columns[0].AllowDBNull = true;
            return dt;
        }

        DataRow GetDataRow(DataTable dt = null)
        {
            dt = dt ?? CreateEmptyDataTable();
            var dr = dt.NewRow();
            dr.ItemArray = new object[] {"Test", 123};

            return dr;
        }

        DataRow GetDataRowDbNull(DataTable dt = null)
        {
            dt = dt ?? CreateEmptyDataTable();
            var dr = dt.NewRow();
            dr.ItemArray = new object[] { DBNull.Value, DBNull.Value };

            return dr;
        }
    }
}
