using System;
using System.IO;
using System.Text;

namespace EmbedTemplate
{
    class Program
    {
        static void Main()
        {
            string validPptx = @"C:\Users\Danh\Desktop\genofficepy\scratch\scratch\valid_powerpoint.pptx";
            byte[] bytes = File.ReadAllBytes(validPptx);
            string base64 = Convert.ToBase64String(bytes);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// Auto-generated OpenXML PPTX Base Template for 100% Microsoft PowerPoint Compatibility");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.IO;");
            sb.AppendLine();
            sb.AppendLine("namespace GenOffice.Format");
            sb.AppendLine("{");
            sb.AppendLine("    public static class PptxTemplate");
            sb.AppendLine("    {");
            sb.AppendLine("        private const string BASE64_TEMPLATE = @\"" + base64 + "\";");
            sb.AppendLine();
            sb.AppendLine("        public static byte[] GetBaseTemplateBytes()");
            sb.AppendLine("        {");
            sb.AppendLine("            return Convert.FromBase64String(BASE64_TEMPLATE);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static MemoryStream GetBaseTemplateStream()");
            sb.AppendLine("        {");
            sb.AppendLine("            return new MemoryStream(GetBaseTemplateBytes());");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(@"C:\Users\Danh\Desktop\genofficepy\src\format\PptxTemplate.cs", sb.ToString(), new UTF8Encoding(false));
            Console.WriteLine("PptxTemplate.cs CREATED_SUCCESSFULLY!");
        }
    }
}
