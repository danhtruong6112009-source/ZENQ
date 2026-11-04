using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;
using ZenQOffice.Core;
namespace ZenQOffice.Format
{
 internal static class AnimationPptxWriter
 {
  static readonly XNamespace P="http://schemas.openxmlformats.org/presentationml/2006/main";
  static readonly XNamespace A="http://schemas.openxmlformats.org/drawingml/2006/main";
  static XElement Node(XElement parent,string name,params object[] attrs){var n=new XElement(P+name,attrs);parent?.Add(n);return n;}
  static XAttribute At(string name,object value)=>new(name,value);
  static XElement Time(XElement parent,int id,params object[] attrs){var t=Node(parent,"cTn",At("id",id));t.Add(attrs);return t;}
  static void Condition(XElement t,object delay){Node(Node(t,"stCondLst"),"cond",At("delay",delay));}
  static (string,int) Preset(ShapeAnimation a)
  {
   if(!string.IsNullOrEmpty(a.SourcePresetClass)&&a.SourceAnimationType==a.Type)return(a.SourcePresetClass,a.SourcePresetId);
   int id=a.Type switch {AnimationType.Appear or AnimationType.Disappear=>1,AnimationType.FlyIn or AnimationType.FlyOut=>2,
    AnimationType.FadeIn or AnimationType.FadeOut=>10,AnimationType.RandomBarsIn or AnimationType.RandomBarsOut=>14,
    AnimationType.SplitIn or AnimationType.SplitOut=>16,AnimationType.Swivel or AnimationType.SwivelOut=>19,
    AnimationType.WheelIn or AnimationType.WheelOut=>21,AnimationType.WipeIn or AnimationType.WipeOut=>22,
    AnimationType.ZoomIn or AnimationType.ZoomOut=>23,AnimationType.Bounce or AnimationType.BounceOut=>26,
    AnimationType.FloatIn or AnimationType.FloatOut=>30,AnimationType.GrowAndTurn or AnimationType.ShrinkAndTurn=>31,
    AnimationType.ShapeIn or AnimationType.ShapeOut=>6,AnimationType.Spin=>8,AnimationType.GrowShrink=>6,
    AnimationType.Transparency=>9,AnimationType.Teeter=>32,AnimationType.FillColor=>1,AnimationType.FontColor=>3,
    AnimationType.LineColor=>7,AnimationType.Darken=>24,AnimationType.Desaturate=>25,AnimationType.Lighten=>30,
    AnimationType.BoldFlash=>10,AnimationType.BoldReveal=>15,AnimationType.Underline=>18,AnimationType.Wave=>34,_=>0};
   return(a.IsExit?"exit":a.IsEmphasis?"emph":a.IsMotionPath?"path":"entr",id);
  }
  public static void Write(StreamWriter writer,SlideNode slide,Dictionary<ShapeNode,int> shapeIds)
  {
   int id=3;var root=Node(null,"timing");
   var tm=Time(Node(Node(root,"tnLst"),"par"),1,At("dur","indefinite"),At("restart","never"),At("nodeType","tmRoot"));
   var main=Node(Node(tm,"childTnLst"),"seq",At("concurrent",1),At("nextAc","seek"));
   var mainTime=Time(main,2,At("dur","indefinite"),At("nodeType","mainSeq"));var groups=Node(mainTime,"childTnLst");id=3;
   AnimationTimeline.Build(slide,out var starts,out var durations,out var steps);
   XElement groupList=null;int lastStep=-99;
   var interactiveGroups=new Dictionary<string,XElement>();
   for(int i=0;i<slide.Animations.Count;i++)
   {
    var anim=slide.Animations[i];var target=shapeIds.FirstOrDefault(pair=>pair.Key.Id==anim.ShapeId);
    if(target.Key==null)continue;
    XElement destination;
    if(!string.IsNullOrEmpty(anim.TriggerShapeId))
    {
     if(!interactiveGroups.TryGetValue(anim.TriggerShapeId,out destination))
     {
      var trigger=shapeIds.FirstOrDefault(pair=>pair.Key.Id==anim.TriggerShapeId);if(trigger.Key==null)continue;
      var seq=Node(tm.Element(P+"childTnLst"),"seq",At("concurrent",1),At("nextAc","seek"));
      var timing=Time(seq,id++,At("dur","indefinite"),At("nodeType","interactiveSeq"),At("restart","whenNotActive"));
      var cond=Node(Node(timing,"stCondLst"),"cond",At("evt","onClick"),At("delay",0));Node(Node(cond,"tgtEl"),"spTgt",At("spid",trigger.Value));
      destination=Node(timing,"childTnLst");interactiveGroups[anim.TriggerShapeId]=destination;
     }
    }
    else
    {
     if(groupList==null||steps[i]!=lastStep)
     {
      var group=Time(Node(groups,"par"),id++,At("fill","hold"));Condition(group,steps[i]>0?"indefinite":"0");
      groupList=Node(group,"childTnLst");lastStep=steps[i];
     }
     destination=groupList;
    }
    var wrapper=Time(Node(destination,"par"),id++,At("fill","hold"));Condition(wrapper,(int)(starts[i]*1000));
    var (cls,preset)=Preset(anim);
    int subtype=anim.SourceAnimationType==anim.Type&&anim.SourceDirection==anim.Direction&&!string.IsNullOrEmpty(anim.SourcePresetClass)?anim.SourcePresetSubtype:
     anim.Type is AnimationType.SplitIn or AnimationType.SplitOut?(anim.Direction==TransitionDirection.Horizontal?26:21):anim.Direction switch{TransitionDirection.FromTop=>1,TransitionDirection.FromRight=>2,TransitionDirection.FromLeft=>8,_=>4};
    var effect=Time(Node(Node(wrapper,"childTnLst"),"par"),id++,At("presetID",preset),At("presetClass",cls),At("presetSubtype",subtype),At("fill","hold"),At("nodeType",anim.Trigger==AnimationTrigger.OnClick?"clickEffect":anim.Trigger==AnimationTrigger.WithPrevious?"withEffect":"afterEffect"));if(anim.AutoReverse)effect.Add(At("autoRev",1));Condition(effect,0);
    var behaviors=Node(effect,"childTnLst");
    XElement Behavior(string name)
    {
     var n=Node(behaviors,name);var bhvr=Node(n,"cBhvr");Time(bhvr,id++,At("dur",Math.Max(1,(int)(anim.Duration*1000))),At("fill","hold"));
     var sp=Node(Node(bhvr,"tgtEl"),"spTgt",At("spid",target.Value));
     if(anim.TargetParagraphStart>=0)Node(Node(sp,"txEl"),"pRg",At("st",anim.TargetParagraphStart),At("end",Math.Max(anim.TargetParagraphStart,anim.TargetParagraphEnd)));
     else if(anim.TargetCharacterStart>=0)Node(Node(sp,"txEl"),"charRg",At("st",anim.TargetCharacterStart),At("end",Math.Max(anim.TargetCharacterStart,anim.TargetCharacterEnd-1)));
     return n;
    }
    string transition=anim.IsExit?"out":"in";
    if(anim.Type is AnimationType.FlyIn or AnimationType.FlyOut or AnimationType.FloatIn or AnimationType.FloatOut)
    {
     bool horizontal=anim.Direction is TransitionDirection.FromLeft or TransitionDirection.FromRight;
     string coordinate=horizontal?"ppt_x":"ppt_y";int sign=anim.Direction is TransitionDirection.FromLeft or TransitionDirection.FromTop?-1:1;
     var n=Behavior("anim");n.Add(At("calcmode","lin"),At("valueType","num"));
     Node(n.Element(P+"cBhvr"),"attrNameLst").Add(new XElement(P+"attrName",coordinate));
     var vals=Node(n,"tavLst");string home="#"+coordinate,away=home+(sign<0?"-1":"+1");
     Node(Node(vals,"tav",At("tm",0)),"val").Add(new XElement(P+"strVal",At("val",anim.IsExit?home:away)));
     Node(Node(vals,"tav",At("tm",100000)),"val").Add(new XElement(P+"strVal",At("val",anim.IsExit?away:home)));
    }
    else if(anim.Type is AnimationType.ZoomIn or AnimationType.ZoomOut or AnimationType.GrowShrink)
    {
     var n=Behavior("animScale");
     int from=anim.Type==AnimationType.ZoomIn?1:100000;
     int tx=anim.Type==AnimationType.GrowShrink?(int)(anim.GrowScaleX*100000):anim.IsExit?1:100000;
     int ty=anim.Type==AnimationType.GrowShrink?(int)(anim.GrowScaleY*100000):tx;
     Node(n,"from",At("x",from),At("y",from));Node(n,"to",At("x",tx),At("y",ty));
    }
    else if(anim.Type==AnimationType.Spin){var n=Behavior("animRot");n.Add(At("by",(int)(anim.RotationDegrees*60000)));}
    else if(anim.IsMotionPath)
    {
     var n=Behavior("animMotion");n.Add(At("origin","layout"),At("pathEditMode","relative"),At("path",anim.SourceMotionPath ?? string.Format(System.Globalization.CultureInfo.InvariantCulture,"M 0 0 L {0} {1} E",anim.EndOffsetX/1280f,anim.EndOffsetY/720f)));
    }
    else
    {
     string side=anim.Direction switch{TransitionDirection.FromLeft=>"left",TransitionDirection.FromRight=>"right",TransitionDirection.FromTop=>"up",_=>"down"};
     string filter=anim.Type switch{AnimationType.SplitIn or AnimationType.SplitOut=>"barn("+((subtype&32)!=0?"out":"in")+(anim.Direction==TransitionDirection.Horizontal?"Horizontal":"Vertical")+")",
      AnimationType.WipeIn or AnimationType.WipeOut=>"wipe("+side+")",AnimationType.WheelIn or AnimationType.WheelOut=>"wheel(4)",AnimationType.RandomBarsIn or AnimationType.RandomBarsOut=>"randombar(horizontal)",AnimationType.ShapeIn or AnimationType.ShapeOut=>"iris",_=>"fade"};
     if(anim.SourceAnimationType==anim.Type&&anim.SourceDirection==anim.Direction&&!string.IsNullOrEmpty(anim.SourceEffectFilter))filter=anim.SourceEffectFilter;
     var n=Behavior("animEffect");n.Add(At("transition",transition),At("filter",filter));
    }
   }
   foreach(string action in new[]{"prev","next"}){var conditions=Node(main,action+"CondLst");var cond=Node(conditions,"cond",At("evt",action=="prev"?"onPrev":"onNext"),At("delay",0));Node(Node(cond,"tgtEl"),"sldTgt");}
   writer.WriteLine(root.ToString(SaveOptions.DisableFormatting));
  }
 }
}
