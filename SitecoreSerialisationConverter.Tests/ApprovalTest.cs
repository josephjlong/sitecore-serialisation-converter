using System.Reflection;

namespace SitecoreSerialisationConverter.Tests
{
    using FluentAssertions.Json;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ApprovalTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void AddItem()
        {
            var execPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var expectedCoreJson = JToken.Load(new JsonTextReader(new StreamReader(execPath + "\\ExpectedModuleFiles\\Tests.Expected.Data.Core.module.json")));
            var expectedMasterJson = JToken.Load(new JsonTextReader(new StreamReader(execPath + "\\ExpectedModuleFiles\\Tests.Expected.Data.Master.module.json")));

            Program.Main(new string[] { });

            var actualCoreJson = JToken.Load(new JsonTextReader(new StreamReader(execPath + "\\ApprovalTestConverted\\Tests.Data.Core.module.json")));
            var actualMasterJson = JToken.Load(new JsonTextReader(new StreamReader(execPath + "\\ApprovalTestConverted\\Tests.Data.Master.module.json")));

            actualCoreJson.Should().BeEquivalentTo(expectedCoreJson);
            actualMasterJson.Should().BeEquivalentTo(expectedMasterJson);
        }
    }
}