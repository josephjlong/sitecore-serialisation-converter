using Sitecore.DevEx.Serialization;
using SitecoreSerialisationConverter.Models;

namespace SitecoreSerialisationConverter.Commands
{
    using System;

    public static class ProjectedScope
    {
        public static TreeScope Get(ChildSynchronizationType childSyncSetting)
        {
            switch (childSyncSetting)
            {
                case ChildSynchronizationType.NoChildSynchronization:
                    return TreeScope.SingleItem;
                case ChildSynchronizationType.KeepAllChildrenSynchronized:
                    return TreeScope.ItemAndDescendants;
                case ChildSynchronizationType.KeepDirectDescendantsSynchronized:
                    return TreeScope.ItemAndChildren;
                default:
                    return TreeScope.SingleItem;
            }
        }
    }
}
