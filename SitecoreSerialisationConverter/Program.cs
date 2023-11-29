using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Sitecore.DevEx.Serialization;
using Sitecore.DevEx.Serialization.Client;
using Sitecore.DevEx.Serialization.Client.Configuration;
using Sitecore.DevEx.Serialization.Client.Datasources.Filesystem.Configuration;
using Sitecore.DevEx.Serialization.Models;
using Sitecore.DevEx.Serialization.Models.Roles;
using SitecoreSerialisationConverter.Commands;
using SitecoreSerialisationConverter.Models;

namespace SitecoreSerialisationConverter
{
    public class Program
    {
        public static Settings Settings;
        public static List<AliasItem> AliasList;

        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();

            Settings = config.GetRequiredSection("Settings").Get<Settings>();

            var solutionFolder = Settings.SolutionFolder;
            var tdsFiles = Directory.GetFiles(solutionFolder, "*.scproj", SearchOption.AllDirectories);
            var savePath = Settings.SavePath;
            bool useRelativeSavePath = Settings.UseRelativeSavePath;
            var relativeSavePath = Settings.RelativeSavePath;

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            foreach (var file in tdsFiles)
            {
                ConvertSerialisationFile(file, savePath, useRelativeSavePath, relativeSavePath);
            }
        }

        private static void ConvertSerialisationFile(string projectPath, string savePath, bool useRelativeSavePath, string relativeSavePath)
        {
            XDocument project = XDocument.Load(projectPath);
            XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";

            if (project != null)
            {
                AliasList = new List<AliasItem>();
                string projectName = project.Descendants(msbuild + "RootNamespace").Select(x => x.Value).FirstOrDefault();
                string database = project.Descendants(msbuild + "SitecoreDatabase").Select(x => x.Value).FirstOrDefault();

                SerializationModuleConfiguration newConfigModule = new SerializationModuleConfiguration()
                {
                    Description = Settings.ProjectDescription,
                    Namespace = projectName,
                    Items = new SerializationModuleConfigurationItems()
                    {
                        Includes = new List<FilesystemTreeSpec>()
                    }
                };

                var roles = new List<RolePredicateItem>();

                var ignoreSyncChildren = false;
                var ignoreDirectSyncChildren = false;

                foreach (var sitecoreItem in project.Descendants(msbuild + "SitecoreItem"))
                {
                    var includePath = sitecoreItem.Attribute("Include")?.Value;
                    var deploymentType = sitecoreItem.Descendants(msbuild + "ItemDeployment").Select(x => x.Value).FirstOrDefault();
                    var childSynchronisation = sitecoreItem.Descendants(msbuild + "ChildItemSynchronization").Select(x => x.Value).FirstOrDefault();
                    var sitecoreName = sitecoreItem.Descendants(msbuild + "SitecoreName").Select(x => x.Value).FirstOrDefault();

                    if (!string.IsNullOrEmpty(sitecoreName))
                    {
                        AliasItem aliasItem = new AliasItem()
                        {
                            AliasName = Path.GetFileNameWithoutExtension(includePath),
                            SitecoreName = sitecoreName
                        };

                        AliasList.Add(aliasItem);
                    }

                    RenderItem(database, newConfigModule, ref ignoreSyncChildren, ref ignoreDirectSyncChildren, includePath, deploymentType, childSynchronisation);
                }

                foreach (var sitecoreRole in project.Descendants(msbuild + "SitecoreRole"))
                {
                    var includePathSplit = sitecoreRole.Attribute("Include")?.Value?.Split('\\');

                    if (includePathSplit?.Length == 3)
                    {
                        var domain = includePathSplit[1];
                        var roleName = includePathSplit[2];

                        if (!string.IsNullOrEmpty(domain) && !string.IsNullOrEmpty(roleName))
                        {
                            roles.Add(new RolePredicateItem()
                            {
                                Domain = domain,
                                Pattern = roleName.Replace(".role", string.Empty)
                            });
                        }
                    }
                }

                if (roles.Count > 0)
                {
                    newConfigModule.Roles = roles;
                }

                if (newConfigModule.Items.Includes.Any() || newConfigModule.Roles.Any())
                {
                    if (useRelativeSavePath)
                    {
                        savePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(projectPath), @relativeSavePath));
                    }

                    WriteNewConfig(savePath, newConfigModule);
                }
            }
        }

        private static void RenderItem(string database, SerializationModuleConfiguration newConfigModule, ref bool ignoreSyncChildren, ref bool ignoreDirectSyncChildren, string includePath, string deploymentType, string childSynchronisation)
        {
            if (childSynchronisation == "NoChildSynchronization")
            {
                var path = SafePath.Get(includePath);

                if (database == "master")
                {
                    var matchedMasterPaths = GetIgnoredRoutes.Master(Settings).Where(x => Regex.IsMatch(x, path, RegexOptions.IgnoreCase));

                    if (!matchedMasterPaths.Any())
                    {
                        AddItem(database, newConfigModule, includePath, deploymentType, childSynchronisation);
                    }
                }
                else if (database == "core")
                {
                    var matchedCorePaths = GetIgnoredRoutes.Core(Settings).Where(x => Regex.IsMatch(x, path, RegexOptions.IgnoreCase));

                    if (!matchedCorePaths.Any())
                    {
                        AddItem(database, newConfigModule, includePath, deploymentType, childSynchronisation);
                    }
                }
            }

            if (childSynchronisation == "KeepAllChildrenSynchronized" && !ignoreSyncChildren)
            {
                ignoreSyncChildren = true;

                AddItem(database, newConfigModule, includePath, deploymentType, childSynchronisation);
            }

            if (childSynchronisation != "KeepAllChildrenSynchronized")
            {
                ignoreSyncChildren = false;
            }

            if (childSynchronisation == "KeepDirectDescendantsSynchronized" && !ignoreDirectSyncChildren)
            {
                ignoreDirectSyncChildren = true;

                AddItem(database, newConfigModule, includePath, deploymentType, childSynchronisation);
            }

            if (childSynchronisation != "KeepDirectDescendantsSynchronized")
            {
                ignoreDirectSyncChildren = false;
            }
        }

        private static void AddItem(string database, SerializationModuleConfiguration newConfigModule, string includePath, string deploymentType, string childSynchronisation)
        {
            includePath = PathAlias.Remove(includePath, AliasList);

            FilesystemTreeSpec newSpec = new FilesystemTreeSpec()
            {
                Name = SafeName.Get(includePath),
                Path = ItemPath.FromPathString(SafePath.Get(includePath)),
                AllowedPushOperations = PushOperation.Get(deploymentType),
                Scope = ProjectedScope.Get(childSynchronisation)
            };
            
            //if it's not default then set it.
            if (database != "master")
            {
                newSpec.Database = database;
            }

            //set defaults
            newConfigModule.Items.Includes.Add(newSpec);
        }

        private static void WriteNewConfig(string savePath, SerializationModuleConfiguration moduleConfiguration)
        {
            var path = Path.Combine(savePath, moduleConfiguration.Namespace + ".module.json");
            using (FileStream outputStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter textWriter = new StreamWriter(outputStream))
                {
                    JsonSerializer.Create(_serializerSettings).Serialize(textWriter, moduleConfiguration);
                }
            }

            Console.WriteLine(path);
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new JsonItemPathConverter(),
                new JsonItemPathMatchConverter(),
                new FilesystemTreeSpecRuleConverter(),
                new StringEnumConverter(new CamelCaseNamingStrategy())
            },
            Formatting = Newtonsoft.Json.Formatting.Indented,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            //if we do want to leave out the defaults.
            //DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
    }

    public class FilesystemTreeSpecRuleConverter : JsonConverter<TreeSpecRule>
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, TreeSpecRule value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override TreeSpecRule ReadJson(JsonReader reader, Type objectType, TreeSpecRule existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            FilesystemTreeSpecRule filesystemTreeSpecRule = new FilesystemTreeSpecRule();
            serializer.Populate(reader, filesystemTreeSpecRule);
            return filesystemTreeSpecRule;
        }
    }
}
