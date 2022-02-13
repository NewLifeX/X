using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XCode.DataAccessLayer;
using Xunit;

namespace XUnitTest.XCode.Model
{
    public class ModelResolverTests
    {
        [Fact]
        public void Test1()
        {
            var mr = new ModelResolver();

            Assert.Equal("Class", mr.GetName("class"));
            Assert.Equal("Class", mr.GetName("CLASS"));

            Assert.Equal("Class", mr.GetName("$class"));
            Assert.Equal("Class", mr.GetName("(class)"));
            Assert.Equal("Class", mr.GetName("（class）"));
            Assert.Equal("Classid", mr.GetName("class id"));
            Assert.Equal("Classid", mr.GetName("class  id"));
            Assert.Equal("ClassId", mr.GetName("class/id"));
            Assert.Equal("ClassId", mr.GetName("class\\id"));

            Assert.Equal("ClassId", mr.GetName("class_id"));
            Assert.Equal("ClassId", mr.GetName("CLASS_ID"));
        }

        [Fact]
        public void UnderlineTest()
        {
            var mr = new ModelResolver
            {
                Underline = true
            };

            Assert.Equal("Class", mr.GetName("class"));
            Assert.Equal("Class", mr.GetName("CLASS"));

            Assert.Equal("Class", mr.GetName("$class"));
            Assert.Equal("Class", mr.GetName("(class)"));
            Assert.Equal("Class", mr.GetName("（class）"));
            Assert.Equal("Classid", mr.GetName("class id"));
            Assert.Equal("Classid", mr.GetName("class  id"));
            Assert.Equal("Class_id", mr.GetName("class/id"));
            Assert.Equal("Class_id", mr.GetName("class\\id"));

            Assert.Equal("Class_id", mr.GetName("class_id"));
            Assert.Equal("Class_id", mr.GetName("CLASS_ID"));
        }

        [Fact]
        public void CamelTest()
        {
            var mr = new ModelResolver
            {
                Camel = false
            };

            Assert.Equal("class", mr.GetName("class"));
            Assert.Equal("CLASS", mr.GetName("CLASS"));

            Assert.Equal("class", mr.GetName("$class"));
            Assert.Equal("class", mr.GetName("(class)"));
            Assert.Equal("class", mr.GetName("（class）"));
            Assert.Equal("classid", mr.GetName("class id"));
            Assert.Equal("classid", mr.GetName("class  id"));
            Assert.Equal("classid", mr.GetName("class/id"));
            Assert.Equal("classid", mr.GetName("class\\id"));

            Assert.Equal("classid", mr.GetName("class_id"));
            Assert.Equal("CLASSID", mr.GetName("CLASS_ID"));
        }

        [Fact]
        public void UnderlineCamelTest()
        {
            var mr = new ModelResolver
            {
                Underline = true,
                Camel = false
            };

            Assert.Equal("class", mr.GetName("class"));
            Assert.Equal("CLASS", mr.GetName("CLASS"));

            Assert.Equal("class", mr.GetName("$class"));
            Assert.Equal("class", mr.GetName("(class)"));
            Assert.Equal("class", mr.GetName("（class）"));
            Assert.Equal("classid", mr.GetName("class id"));
            Assert.Equal("classid", mr.GetName("class  id"));
            Assert.Equal("class_id", mr.GetName("class/id"));
            Assert.Equal("class_id", mr.GetName("class\\id"));

            Assert.Equal("class_id", mr.GetName("class_id"));
            Assert.Equal("CLASS_ID", mr.GetName("CLASS_ID"));
        }
    }
}