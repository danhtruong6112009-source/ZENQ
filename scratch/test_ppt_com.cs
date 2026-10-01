using System;

class Program
{
    static void Main()
    {
        try
        {
            Type pptType = Type.GetTypeFromProgID("PowerPoint.Application");
            if (pptType == null)
            {
                Console.WriteLine("PowerPoint COM not registered.");
                return;
            }
            dynamic ppt = Activator.CreateInstance(pptType);
            Console.WriteLine("PowerPoint version: " + ppt.Version);
            ppt.Quit();
            Console.WriteLine("COM Test SUCCESS");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
