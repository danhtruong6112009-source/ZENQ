using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Windows.Forms;
namespace ZenQOffice.UI
{
 public static partial class WriterMath
 {
  const string ClipboardFormat="ZenQOffice.MathText.v1";
  const string PresentationNamespace="urn:zenqoffice:math-presentation:1";
  public static bool IsLinear(char token)=>Equations.TryGetValue(token,out var xml)&&xml.GetAttribute("linear",PresentationNamespace)=="1";
  public static Color? GetColor(char token)=>Equations.TryGetValue(token,out var xml)&&int.TryParse(xml.GetAttribute("color",PresentationNamespace),System.Globalization.NumberStyles.HexNumber,null,out int rgb)?Color.FromArgb(255,(rgb>>16)&255,(rgb>>8)&255,rgb&255):null;
  public static char WithColor(char token,Color color)
  {
   if(!Equations.TryGetValue(token,out var source))return token;
   string hex=$"{color.R:X2}{color.G:X2}{color.B:X2}";
   if(GetColor(token)?.ToArgb()==Color.FromArgb(255,color.R,color.G,color.B).ToArgb())return token;
   var doc=new XmlDocument();var root=(XmlElement)doc.ImportNode(source,true);doc.AppendChild(root);
   const string mc="http://schemas.openxmlformats.org/markup-compatibility/2006";
   const string wNs="http://schemas.openxmlformats.org/wordprocessingml/2006/main";
   root.SetAttribute("xmlns:zq",PresentationNamespace);root.SetAttribute("xmlns:mc",mc);
   var attr=doc.CreateAttribute("zq","color",PresentationNamespace);attr.Value=hex;root.Attributes.Append(attr);
   string existing=root.GetAttribute("Ignorable",mc);var ignored=doc.CreateAttribute("mc","Ignorable",mc);ignored.Value=string.Join(" ",(existing+" zq").Split(' ',StringSplitOptions.RemoveEmptyEntries).Distinct());root.Attributes.Append(ignored);
   foreach(XmlElement run in root.SelectNodes(".//*[local-name()='r']").OfType<XmlElement>())
   {
    var rPr=run.ChildNodes.OfType<XmlElement>().FirstOrDefault(c=>c.LocalName=="rPr"&&c.NamespaceURI==wNs);
    if(rPr==null){rPr=doc.CreateElement("w","rPr",wNs);run.PrependChild(rPr);}
    var cNode=rPr.ChildNodes.OfType<XmlElement>().FirstOrDefault(c=>c.LocalName=="color");
    if(cNode==null){cNode=doc.CreateElement("w","color",wNs);rPr.AppendChild(cNode);}
    var valAttr=doc.CreateAttribute("w","val",wNs);valAttr.Value=hex;cNode.Attributes.Append(valAttr);
   }
   return Register(root)[0];
  }
  public static char WithLinear(char token,bool linear)
  {
   if(!Equations.TryGetValue(token,out var source)||IsLinear(token)==linear)return token;
   var doc=new XmlDocument();var root=(XmlElement)doc.ImportNode(source,true);doc.AppendChild(root);
   const string mc="http://schemas.openxmlformats.org/markup-compatibility/2006";
   if(linear)
   {
    root.SetAttribute("xmlns:zq",PresentationNamespace);root.SetAttribute("xmlns:mc",mc);
    var attr=doc.CreateAttribute("zq","linear",PresentationNamespace);attr.Value="1";root.Attributes.Append(attr);
    string existing=root.GetAttribute("Ignorable",mc);var ignored=doc.CreateAttribute("mc","Ignorable",mc);ignored.Value=string.Join(" ",(existing+" zq").Split(' ',StringSplitOptions.RemoveEmptyEntries).Distinct());root.Attributes.Append(ignored);
   }
   else
   {
    root.RemoveAttribute("linear",PresentationNamespace);
    if(string.IsNullOrEmpty(root.GetAttribute("color",PresentationNamespace)))
    {
     string ignored=string.Join(" ",root.GetAttribute("Ignorable",mc).Split(' ',StringSplitOptions.RemoveEmptyEntries).Where(p=>p!="zq"));
     if(ignored.Length==0)root.RemoveAttribute("Ignorable",mc);else root.SetAttribute("Ignorable",mc,ignored);
    }
   }
   return Register(root)[0];
  }
  public static string FromLinear(string formula)
  {
   var parser=new MathInput(formula??"");return Register(parser.Parse());
  }
  public static string ToLinear(string text)
  {
   if(!Contains(text))return text;
   var result=new StringBuilder();foreach(char c in text)result.Append(Equations.TryGetValue(c,out var equation)?Linear(equation):c.ToString());return result.ToString();
  }
  static string Linear(XmlNode node)
  {
   if(node==null)return "";
   string Part(string name)=>Linear(Child(node,name));
   string all()=>string.Concat(node.ChildNodes.Cast<XmlNode>().Where(n=>n.NodeType==XmlNodeType.Element).Select(Linear));
   if(node.LocalName.EndsWith("Pr",StringComparison.Ordinal))return "";
   switch(node.LocalName)
   {
    case "t":return node.InnerText;
    case "f":return "\\frac{"+Part("num")+"}{"+Part("den")+"}";
    case "sSub":return Part("e")+"_{"+Part("sub")+"}";
    case "sSup":return Part("e")+"^{"+Part("sup")+"}";
    case "sSubSup":return Part("e")+"_{"+Part("sub")+"}^{"+Part("sup")+"}";
    case "sPre":return "{}_{"+Part("sub")+"}^{"+Part("sup")+"}"+Part("e");
    case "rad":return "\\sqrt"+(Part("deg")==""?"":"["+Part("deg")+"]")+"{"+Part("e")+"}";
    case "acc":
     string accent=Val(Child(Child(node,"accPr"),"chr"));
     return (accent.Contains('⃗')||accent.Contains('→')?"\\vec":accent.Contains('̈')?"\\ddot":accent.Contains('̇')?"\\dot":accent.Contains('̃')?"\\tilde":"\\hat")+"{"+Part("e")+"}";
    case "bar":return (Val(Child(Child(node,"barPr"),"pos"))=="bot"?"\\underline":"\\overline")+"{"+Part("e")+"}";
    case "d":return Val(Child(Child(node,"dPr"),"begChr"),"(")+all()+Val(Child(Child(node,"dPr"),"endChr"),")");
    case "nary":return Val(Child(Child(node,"naryPr"),"chr"),"∫")+(Part("sub")==""?"":"_{"+Part("sub")+"}")+(Part("sup")==""?"":"^{"+Part("sup")+"}")+Part("e");
    case "limLow":return Part("e")+"_{"+Part("lim")+"}";
    case "limUpp":return Part("e")+"^{"+Part("lim")+"}";
    case "m":return "\\matrix{"+string.Join("\\\\",node.ChildNodes.Cast<XmlNode>().Where(n=>n.LocalName=="mr").Select(r=>string.Join("&",r.ChildNodes.Cast<XmlNode>().Where(n=>n.LocalName=="e").Select(Linear))))+"}";
    default:return all();
   }
  }
  sealed class ClipboardPayload
  {
   public string Text{get;set;}
   public Dictionary<char,string> Equations{get;set;}
  }
  public static string Pack(string text)=>JsonSerializer.Serialize(new ClipboardPayload{Text=text,Equations=text.Distinct().Where(Equations.ContainsKey).ToDictionary(c=>c,c=>Equations[c].OuterXml)});
  public static string Unpack(string data)
  {
   var payload=JsonSerializer.Deserialize<ClipboardPayload>(data);if(payload?.Text==null||payload.Equations==null)return null;
   var map=new Dictionary<char,string>();foreach(var pair in payload.Equations){var doc=new XmlDocument{XmlResolver=null};doc.LoadXml(pair.Value);if(doc.DocumentElement.NamespaceURI!=M)return null;map[pair.Key]=Register(doc.DocumentElement);}
   return string.Concat(payload.Text.Select(c=>map.TryGetValue(c,out var value)?value:c.ToString()));
  }
  public static void CopyText(string text)
  {
   if(!Contains(text)){Clipboard.SetText(text);return;}
   var data=new DataObject();data.SetText(ToLinear(text),TextDataFormat.UnicodeText);data.SetData(ClipboardFormat,Pack(text));Clipboard.SetDataObject(data,true);
  }
  public static string PasteText()
  {
   try{if(Clipboard.TryGetData<string>(ClipboardFormat,out var data)){string text=Unpack(data);if(text!=null)return text;}}catch{}
   return Clipboard.ContainsText()?Clipboard.GetText():null;
  }
  // The linear editor creates native OMML structures. Unknown commands stay visible
  // instead of being discarded, so unsupported input can still be corrected.
  sealed class MathInput
  {
   readonly XmlDocument doc=new(){XmlResolver=null};readonly string text;int index;
   static readonly Dictionary<string,string> Symbols=new(StringComparer.Ordinal)
   {
    ["pi"]="π",["alpha"]="α",["beta"]="β",["gamma"]="γ",["delta"]="δ",["theta"]="θ",["lambda"]="λ",["mu"]="μ",["sigma"]="σ",["omega"]="ω",["Delta"]="Δ",["Sigma"]="Σ",["Omega"]="Ω",["infty"]="∞",["pm"]="±",["mp"]="∓",["times"]="×",["cdot"]="·",["div"]="÷",["le"]="≤",["leq"]="≤",["ge"]="≥",["geq"]="≥",["ne"]="≠",["neq"]="≠",["approx"]="≈",["to"]="→",["rightarrow"]="→",["leftarrow"]="←",["partial"]="∂",["nabla"]="∇",["in"]="∈",["forall"]="∀",["exists"]="∃",["dots"]="…",["ldots"]="…"
   };
   public MathInput(string value){text=value;}
   XmlElement E(string name,params XmlNode[] children){var e=doc.CreateElement("m",name,M);foreach(var child in children)if(child!=null)e.AppendChild(child);return e;}
   XmlElement V(string name,string value){var e=E(name);var a=doc.CreateAttribute("m","val",M);a.Value=value;e.Attributes.Append(a);return e;}
   XmlElement R(string value,bool plain=false){var t=E("t");t.InnerText=value;return plain?E("r",E("rPr",V("sty","p")),t):E("r",t);}
   XmlElement A(string name,XmlElement content)
   {
    var argument=E(name);foreach(XmlNode n in content.ChildNodes)argument.AppendChild(n.CloneNode(true));return argument;
   }
   public XmlElement Parse(){var root=E("oMath");foreach(var n in Sequence('\0'))root.AppendChild(n);return root;}
   void Skip(){while(index<text.Length&&char.IsWhiteSpace(text[index]))index++;}
   List<XmlNode> Sequence(char end)
   {
    var nodes=new List<XmlNode>();while(index<text.Length)
    {
     if(end!='\0'&&text[index]==end){index++;break;}
     if(text[index]=='^'||text[index]=='_')
     {
      char script=text[index++];var value=Argument();XmlNode body=nodes.Count>0?nodes[^1]:R("");if(nodes.Count>0)nodes.RemoveAt(nodes.Count-1);
      if(body.LocalName==(script=='^'?"sSub":"sSup")){var previous=body;body=E("sSubSup",Child(previous,"e").CloneNode(true),script=='^'?Child(previous,"sub").CloneNode(true):A("sub",value),script=='^'?A("sup",value):Child(previous,"sup").CloneNode(true));}
      else body=E(script=='^'?"sSup":"sSub",E("e",body),A(script=='^'?"sup":"sub",value));
      nodes.Add(body);continue;
     }
     if(char.IsWhiteSpace(text[index])){index++;continue;}
     if(text[index]=='{'){index++;nodes.Add(E("box",E("e",Sequence('}').ToArray())));continue;}
     if(text[index]=='\\'){nodes.Add(Command());continue;}
     nodes.Add(R(text[index++].ToString()));
    }
    return nodes;
   }
   XmlElement Argument()
   {
    Skip();if(index>=text.Length)return E("e");
    if(text[index]=='{'){index++;return E("e",Sequence('}').ToArray());}
    if(text[index]=='\\')return E("e",Command());
    return E("e",R(text[index++].ToString()));
   }
   XmlElement Command()
   {
    index++;int start=index;while(index<text.Length&&char.IsLetter(text[index]))index++;
    string command=text.Substring(start,index-start);if(command.Length==0)return R(index<text.Length?text[index++].ToString():"\\");
    if(Symbols.TryGetValue(command,out var symbol))return R(symbol);
    if(command is "frac" or "dfrac" or "tfrac" or "binom")
    {
     var a=Argument();var b=Argument();var fraction=E("f",command=="binom"?E("fPr",V("type","noBar")):null,A("num",a),A("den",b));
     return command=="binom"?E("d",E("dPr",V("begChr","("),V("endChr",")")),E("e",fraction)):fraction;
    }
    if(command=="sqrt")
    {
     Skip();XmlElement degree=E("deg");if(index<text.Length&&text[index]=='['){index++;degree=E("deg",Sequence(']').ToArray());}
     return E("rad",E("radPr",V("degHide",degree.HasChildNodes?"0":"1")),degree,Argument());
    }
    if(command is "vec" or "overrightarrow" or "hat" or "widehat" or "tilde" or "dot" or "ddot")
     return E("acc",E("accPr",V("chr",command is "vec" or "overrightarrow"?"⃗":command=="tilde"?"̃":command=="dot"?"̇":command=="ddot"?"̈":"̂")),Argument());
    if(command=="matrix")
    {
     Skip();if(index>=text.Length||text[index]!='{')return R("\\matrix",true);
     index++;int cellStart=index,depth=0;var rows=new List<XmlNode>();var cells=new List<XmlNode>();
     void Cell(int end){var parser=new MathInput(text.Substring(cellStart,end-cellStart));var parsed=parser.Parse();var cell=E("e");foreach(XmlNode n in parsed.ChildNodes)cell.AppendChild(doc.ImportNode(n,true));cells.Add(cell);}
     while(index<text.Length)
     {
      char c=text[index];if(c=='{' )depth++;
      if(c=='}'){if(depth==0){Cell(index++);break;}depth--;}
      if(depth==0&&c=='&'){Cell(index);cellStart=++index;continue;}
      if(depth==0&&c=='\\'&&index+1<text.Length&&text[index+1]=='\\'){Cell(index);rows.Add(E("mr",cells.ToArray()));cells.Clear();index+=2;cellStart=index;continue;}
      index++;
     }
     if(cells.Count>0)rows.Add(E("mr",cells.ToArray()));return E("m",rows.ToArray());
    }
    if(command is "overline" or "bar" or "underline")return E("bar",E("barPr",V("pos",command=="underline"?"bot":"top")),Argument());
    if(command is "sin" or "cos" or "tan" or "log" or "ln" or "lim" or "max" or "min")return R(command,true);
    if(command is "sum" or "prod" or "int" or "iint" or "oint")return R(command=="sum"?"∑":command=="prod"?"∏":command=="iint"?"∬":command=="oint"?"∮":"∫",true);
    if(command is "left" or "right"){Skip();if(index<text.Length&&text[index]=='.'){index++;return R("");}return R(index<text.Length?text[index++].ToString():"");}
    if(command is "mathrm" or "text" or "operatorname"){var body=Argument();return R(string.Concat(body.SelectNodes(".//*[local-name()='t']").Cast<XmlNode>().Select(n=>n.InnerText)),true);}
    return R("\\"+command,true);
   }
  }
 }
}
