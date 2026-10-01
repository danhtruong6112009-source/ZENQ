using System;
using System.IO;
using GenOffice.Core;
using GenOffice.UI;

namespace GenOffice.Tests
{
    public static class CheckAiConfig
    {
        public static void Main()
        {
            var config = AiConfig.Load();
            Console.WriteLine("Profiles count: " + config.Profiles.Count);
            for (int i = 0; i < config.Profiles.Count; i++)
            {
                var p = config.Profiles[i];
                Console.WriteLine(string.Format("Profile {0}: Name='{1}', Provider='{2}', Model='{3}', ApiKey='{4}'", i, p.Name, p.Provider, p.SelectedModel, p.ApiKey));
            }
        }
    }
}
