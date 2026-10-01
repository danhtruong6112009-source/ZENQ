using System;
using System.IO;
using System.Xml;
using GenOffice.Core;

class Program
{
    static void Main()
    {
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GenOffice", "ai_config.xml");
        Console.WriteLine("Reading file: " + path);
        Console.WriteLine(File.ReadAllText(path));

        XmlDocument doc = new XmlDocument();
        doc.Load(path);
        XmlNode root = doc.SelectSingleNode("AiConfig");
        XmlNode nodeProfiles = root.SelectSingleNode("Profiles");
        Console.WriteLine("nodeProfiles: " + (nodeProfiles != null ? nodeProfiles.ChildNodes.Count.ToString() : "null"));
        foreach (XmlNode pNode in nodeProfiles.SelectNodes("Profile"))
        {
            Console.WriteLine("pNode innerText: " + pNode.InnerXml);
            XmlNode nName = pNode.SelectSingleNode("Name");
            Console.WriteLine("nName: " + (nName != null ? "'" + nName.InnerText + "'" : "null"));
            XmlNode nProv = pNode.SelectSingleNode("Provider");
            Console.WriteLine("nProv: " + (nProv != null ? "'" + nProv.InnerText + "'" : "null"));
            if (nProv != null)
            {
                try
                {
                    AiProviderType pt = (AiProviderType)Enum.Parse(typeof(AiProviderType), nProv.InnerText);
                    Console.WriteLine("Parsed pt: " + pt + " (" + (int)pt + ")");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Enum.Parse error: " + ex.Message);
                }
            }
        }
    }
}
