using System;
using GenOffice.Core;

class CheckPresRoot
{
    static void Main()
    {
        PresentationRoot p = new PresentationRoot();
        Console.WriteLine("Slides in new PresentationRoot: " + p.Slides.Count);
    }
}
