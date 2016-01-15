using Xunit;
using System.Data.SqlClient;
using ObjectStore.OrMapping;
using System;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using ObjectStore.Test.Fixtures;
using ObjectStore.Test.Resources;
using Xunit.Abstractions;

namespace ObjectStore.Test
{
    [Collection("Database collection")]
    public class PreparedEntitiesTests : IClassFixture<InitEnitiesFixture>
    {
        DatabaseFixture _fixture;
        ITestOutputHelper _output;

        public PreparedEntitiesTests(DatabaseFixture fixture, InitEnitiesFixture initEntitiesFixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;

            initEntitiesFixture.Init(fixture.ObjectProvider);
        }

        [Fact]
        public void TestBasicExpression()
        {
            _output.WriteLine("Not jet implemented!!!");
            // TODO
        }

        //[Fact]
        //public void TestJoinExpression()
        //{
        //}

        //[Fact]
        //public void TestSubExpression()
        //{
        //}

    }
}
