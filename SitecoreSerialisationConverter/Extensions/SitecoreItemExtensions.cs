namespace SitecoreSerialisationConverter.Extensions;

using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;

public static class SitecoreItemExtensions{
    public static T DeserializeSitecoreItem<T>(this XElement xmlFragment)
    {
        CleanoutNs<T>(xmlFragment);
        var xmlData = xmlFragment.ToString();

        var mySerializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xmlData);
        return (T)mySerializer.Deserialize(reader);
    }

    private static void CleanoutNs<T>(XElement xmlFragment)
    {
        foreach (var node in xmlFragment.DescendantsAndSelf())
        {
            node.Name = node.Name.LocalName;
        }
    }
}