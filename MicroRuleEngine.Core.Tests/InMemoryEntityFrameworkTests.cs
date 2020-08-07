// Copyright (c) 2019 Jeremy Oursler All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MicroRuleEngine.Tests
{
    [TestClass]
    public class InMemoryEntityFrameworkTests
    {
        private DbContextOptions<TestDbContext> options;

        [TestMethod]
        public void CheckSetup()
        {
            using (var context = new TestDbContext(options))
            {
                var count = context.Students.Count();

                Assert.AreEqual(8,
                                count);
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            options = new DbContextOptionsBuilder<TestDbContext>()
                      .UseInMemoryDatabase(Guid.NewGuid()
                                               .ToString())
                      .Options;

            using (var context = new TestDbContext(options))
            {
                context.Students.Add(new Student
                                     {
                                         FirstName = "Bob",
                                         LastName  = "Smith",
                                         Gpa       = 2.0m
                                     });

                context.Students.Add(new Student
                                     {
                                         FirstName = "John",
                                         LastName  = "Smith",
                                         Gpa       = 3.5m
                                     });

                context.Students.Add(new Student
                                     {
                                         FirstName = "Bob",
                                         LastName  = "Jones",
                                         Gpa       = 3.0m
                                     });

                context.Students.Add(new Student
                                     {
                                         FirstName = "John",
                                         LastName  = "Jones",
                                         Gpa       = 4.0m
                                     });

                context.Students.Add(new Student
                                     {
                                         FirstName = "Jane",
                                         LastName  = "Smith",
                                         Gpa       = 3.75m
                                     });

                context.Students.Add(new Student
                                     {
                                         FirstName = "Sally",
                                         LastName  = "Smith",
                                         Gpa       = 1.0m
                                     });

                context.Students.Add(new Student
                                     {
                                         FirstName = "Jane",
                                         LastName  = "Jones",
                                         Gpa       = 1.5m
                                     });

                context.Students.Add(new Student
                                     {
                                         FirstName = "Sally",
                                         LastName  = "Jones",
                                         Gpa       = 2.5m
                                     });

                context.SaveChanges();
            }
        }

        [TestMethod]
        public void MoreComplicated()
        {
            var rule =
                new Rule
                {
                    Operator = "OrElse",
                    Rules = new List<Rule>
                            {
                                new Rule
                                {
                                    MemberName  = "FirstName",
                                    Operator    = "Equal",
                                    TargetValue = "Sally"
                                },
                                new Rule
                                {
                                    MemberName  = "FirstName",
                                    Operator    = "Equal",
                                    TargetValue = "Jane"
                                }
                            }
                };

            var expression = MRE.ToExpression<Student>(rule, false);

            using (var context = new TestDbContext(options))
            {
                var count = context.Students.Where(expression)
                                   .Count();

                Assert.AreEqual(4,
                                count);
            }
        }

        [TestMethod]
        public void ReallyComplicated()
        {
            var rule =
                new Rule
                {
                    Operator = "AndAlso",
                    Rules = new List<Rule>
                            {
                                new Rule
                                {
                                    Operator = "OrElse",
                                    Rules = new List<Rule>
                                            {
                                                new Rule
                                                {
                                                    MemberName  = "FirstName",
                                                    Operator    = "Equal",
                                                    TargetValue = "Sally"
                                                },
                                                new Rule
                                                {
                                                    MemberName  = "FirstName",
                                                    Operator    = "Equal",
                                                    TargetValue = "Jane"
                                                }
                                            }
                                },
                                new Rule
                                {
                                    MemberName  = "Gpa",
                                    Operator    = "GreaterThan",
                                    TargetValue = "2.0"
                                }
                            }
                };

            var expression = MRE.ToExpression<Student>(rule, false);

            using (var context = new TestDbContext(options))
            {
                var count = context.Students.Where(expression)
                                   .Count();

                Assert.AreEqual(2,
                                count);
            }
        }

        [TestMethod]
        public void SimpleRule()
        {
            var rule = new Rule
                       {
                           MemberName  = "FirstName",
                           Operator    = "Equal",
                           TargetValue = "Sally"
                       };

            var expression = MRE.ToExpression<Student>(rule, false);

            using (var context = new TestDbContext(options))
            {
                var count = context.Students.Where(expression)
                                   .Count();

                Assert.AreEqual(2,
                                count);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void SimpleRule_IncludeTryCatch()
        {
            var rule = new Rule
                       {
                           MemberName  = "FirstName",
                           Operator    = "Equal",
                           TargetValue = "Sally"
                       };

            var expression = MRE.ToExpression<Student>(rule, true);

            using (var context = new TestDbContext(options))
            {
                var count = context.Students.Where(expression)
                                   .Count();

                Assert.AreEqual(2,
                                count);
            }
        }
    }

    public class Student
    {
        [MaxLength(32)]
        public string FirstName { get; set; }

        public decimal Gpa { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [MaxLength(32)]
        public string LastName { get; set; }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
    }
}
