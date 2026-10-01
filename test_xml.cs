using System;
using System.Xml;

class Program {
    static void Main() {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cols><col min=\"1\" max=\"1\" width=\"20\"/></cols><row r=\"1\" ht=\"45\"><c r=\"A1\"><v>123</v></c></row></worksheet>";
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);
        Console.WriteLine("GetElementsByTagName('col'): " + doc.GetElementsByTagName("col").Count);
        Console.WriteLine("GetElementsByTagName('col', '*'): " + doc.GetElementsByTagName("col", "*").Count);
        Console.WriteLine("SelectNodes(//*[local-name()='col']): " + doc.SelectNodes("//*[local-name()='col']").Count);
        Console.WriteLine("GetElementsByTagName('row'): " + doc.GetElementsByTagName("row").Count);
    }
}
