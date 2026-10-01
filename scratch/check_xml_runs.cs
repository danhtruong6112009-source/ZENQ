using System;
using System.Xml;

class CheckXmlRuns
{
    static void Main()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(@"scratch\slide8_dump.xml");
        XmlNode sp = doc.SelectSingleNode("//*[local-name()='cNvPr'][@name='TextBox 12']/../..");
        if (sp == null) { Console.WriteLine("TextBox 12 not found"); return; }

        XmlNodeList pList = sp.SelectNodes(".//*[local-name()='p']");
        Console.WriteLine("p count: " + pList.Count);
        for (int pi = 0; pi < pList.Count; pi++)
        {
            XmlNode p = pList[pi];
            XmlNodeList rList = p.SelectNodes(".//*[local-name()='r']");
            Console.WriteLine(string.Format("p {0}: r count = {1}", pi, rList.Count));
            for (int ri = 0; ri < rList.Count; ri++)
            {
                XmlNode r = rList[ri];
                XmlNode t = r.SelectSingleNode(".//*[local-name()='t']");
                Console.WriteLine(string.Format("  r {0}: t = '{1}'", ri, t != null ? t.InnerText : "(null)"));
            }
        }
    }
}
