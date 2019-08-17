using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IoT.Simulator2.Tools
{
    public static class AssemblyInformationHelper
    {
        static Version assemblyVersion = Assembly.GetEntryAssembly().GetName().Version;
        static AssemblyFileVersionAttribute fileVersion = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>();
        static AssemblyInformationalVersionAttribute version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        static AssemblyCopyrightAttribute copyright = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>();
        static AssemblyDescriptionAttribute description = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>();
        static AssemblyProductAttribute product = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyProductAttribute>();
        static AssemblyCompanyAttribute company = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyCompanyAttribute>();
        static AssemblyTitleAttribute title = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>();

        static string _headerMesage = $@"{title.Title}-v{version.InformationalVersion}{Environment.NewLine}   Assembly version:{assemblyVersion.ToString()}{Environment.NewLine}   File version:{fileVersion.Version}{Environment.NewLine}   by {company.Company}-{copyright.Copyright}";
        public static string HeaderMessage
        {
            get { return _headerMesage; }
        }        
    }
}
