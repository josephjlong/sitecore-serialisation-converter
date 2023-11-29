using System.Xml.Serialization;

namespace SitecoreSerialisationConverter.Models
{
    [XmlRoot(ElementName = "SitecoreItem")]
    public class SitecoreItem
    {
        [XmlAttribute(AttributeName = "Include")]
        public string Include { get; set; }

        public string SitecoreName { get; set; }
        public ItemDeploymentType ItemDeployment { get; set; }
        public ChildSynchronizationType ChildItemSynchronization { get; set; }

    }
}
