using System;
using System.IO;
using System.Collections.Generic;
using GenOffice.Core;

namespace GenOffice.Tests
{
    public class RestoreGeminiConfig
    {
        public static void Main()
        {
            AiConfig config = AiConfig.Load();
            bool hasGemini = false;
            foreach (var p in config.Profiles)
            {
                if (p.Provider == AiProviderType.GoogleAIStudio || (p.Name != null && p.Name.ToLower().Contains("gemini")))
                {
                    hasGemini = true;
                    break;
                }
            }

            if (!hasGemini)
            {
                var geminiProf = config.AddProfile("Google Gemini", AiProviderType.GoogleAIStudio, "", "gemini-1.5-flash");
                geminiProf.SavedModels = new List<string> { "gemini-1.5-flash", "gemini-1.5-pro", "gemini-2.0-flash" };
                config.Save();
                Console.WriteLine("Added Google Gemini profile back to ai_config.xml!");
            }
            else
            {
                Console.WriteLine("Google Gemini profile already exists.");
            }

            foreach (var p in config.Profiles)
            {
                Console.WriteLine("Profile: " + p.Name + " (" + p.Provider + ") -> Model: " + p.SelectedModel);
            }
        }
    }
}
