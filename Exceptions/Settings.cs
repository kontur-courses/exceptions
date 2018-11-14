using System;

namespace Exceptions
{
    public class Settings
    {
        public static Settings Default = new Settings("ru", false);
        public string SourceCultureName;
        public bool Verbose;

        public Settings()
        {
        }

        private Settings(string sourceCultureName, bool verbose)
        {
            SourceCultureName = sourceCultureName;
            Verbose = verbose;
        }
    }
}