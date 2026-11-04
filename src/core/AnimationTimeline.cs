using System;
using System.Collections.Generic;
namespace ZenQOffice.Core
{
    public static class AnimationTimeline
    {
        // Step zero belongs to slide entry; each OnClick creates the next click group.
        public static void Build(SlideNode slide, out float[] starts, out float[] durations, out int[] steps)
        {
            int count=slide?.Animations?.Count??0;
            starts=new float[count];durations=new float[count];steps=new int[count];
            int step=0, previous=-1;
            var triggeredPrevious = new Dictionary<string,int>();
            for(int i=0;i<count;i++)
            {
                var a=slide.Animations[i];durations[i]=Math.Max(.001f,a.Duration)*(a.AutoReverse?2:1);
                if(!string.IsNullOrEmpty(a.TriggerShapeId))
                {
                    steps[i]=-1;starts[i]=Math.Max(0,a.Delay);
                    if(a.Trigger!=AnimationTrigger.OnClick&&triggeredPrevious.TryGetValue(a.TriggerShapeId,out int prev))
                        starts[i]+=starts[prev]+(a.Trigger==AnimationTrigger.AfterPrevious?durations[prev]:0);
                    triggeredPrevious[a.TriggerShapeId]=i;continue;
                }
                if(a.Trigger==AnimationTrigger.OnClick){step++;starts[i]=Math.Max(0,a.Delay);}
                else if(previous<0) starts[i]=Math.Max(0,a.Delay);
                else starts[i]=starts[previous]+(a.Trigger==AnimationTrigger.AfterPrevious?durations[previous]:0)+Math.Max(0,a.Delay);
                steps[i]=step;previous=i;
            }
        }
        public static float[] PreviewStarts(SlideNode slide, out float total)
        {
            Build(slide,out var starts,out var durations,out var steps);
            var result=new float[starts.Length];float groupBase=0,groupEnd=0;int step=0;
            for(int i=0;i<starts.Length;i++)
            {
                if(steps[i]<0){result[i]=float.PositiveInfinity;continue;}
                if(steps[i]!=step){groupBase=groupEnd;step=steps[i];}
                result[i]=groupBase+starts[i];groupEnd=Math.Max(groupEnd,result[i]+durations[i]);
            }
            total=groupEnd;return result;
        }
    }
}
