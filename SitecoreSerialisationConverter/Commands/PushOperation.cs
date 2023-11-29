using Sitecore.DevEx.Serialization.Client;

namespace SitecoreSerialisationConverter.Commands
{
    using System;
    using Models;

    public static class PushOperation
    {
        public static AllowedPushOperations Get(ItemDeploymentType deploymentType)
        {
            switch (deploymentType)
            {
                case ItemDeploymentType.AlwaysUpdate:
                    return AllowedPushOperations.CreateUpdateAndDelete;
                case ItemDeploymentType.NeverDeploy:
                case ItemDeploymentType.DeployOnce:
                default:
                    return AllowedPushOperations.CreateOnly;
            }
        }
    }
}
