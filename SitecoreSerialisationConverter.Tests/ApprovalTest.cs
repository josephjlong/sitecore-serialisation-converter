namespace SitecoreSerialisationConverter.Tests
{
    using System.Reflection;
    using FluentAssertions.Json;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestFixture]
    public class ApprovalTest
    {
        [Test]
        public void GoldenMasterForCoreProject()
        {
            var execPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var expectedCoreJson = JToken.Load(new JsonTextReader(new StreamReader(execPath + "\\ExpectedModuleFiles\\Tests.Expected.Data.Core.module.json")));
            Program.AppSettingsJsonFile = "appSettings.Core.json";
            Program.Main(new string[] {  });

            var actualCoreJson = JToken.Load(new JsonTextReader(new StreamReader(execPath + "\\ApprovalTestConverted\\Tests.Data.Core.module.json")));

            actualCoreJson.Should().BeEquivalentTo(expectedCoreJson);
        }
        
        [Test]
        public void GoldenMasterForMasterProject()
        {
            var execPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var expectedMasterJson = JToken.Load(new JsonTextReader(new StreamReader(execPath + "\\ExpectedModuleFiles\\Tests.Expected.Data.Master.module.json")));
            Program.AppSettingsJsonFile = "appSettings.Master.json";
            Program.Main(new string[] { });

            var actualMasterJson = JToken.Load(new JsonTextReader(new StreamReader(execPath + "\\ApprovalTestConverted\\Tests.Data.Master.module.json")));

            actualMasterJson.Should().BeEquivalentTo(expectedMasterJson);
        }
    }
}