using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Coverage.Analysis;
using System.IO;

// https://github.com/danielpalme/ReportGenerator/wiki/Visual-Studio-Coverage-Tools#vstestconsoleexe
namespace CoverageConverter {
    class Program {
        static int Main(string[] args) {

            if (args == null || args.Length < 3) {
                PrintUsage();
                return 2;
            }

            string coverageFilePath = args[0];
            string assemblyFolderPath = args[1];
            string outputFilePath = args[2];

            if (string.IsNullOrEmpty(coverageFilePath)) { throw new ArgumentNullException(nameof(coverageFilePath)); }
            if (string.IsNullOrEmpty(assemblyFolderPath)) { throw new ArgumentNullException(nameof(assemblyFolderPath)); }
            if (string.IsNullOrEmpty(outputFilePath)) { throw new ArgumentNullException(nameof(outputFilePath)); }

            if (!File.Exists(coverageFilePath)) {
                System.Console.WriteLine(string.Format("Coverage file not found", coverageFilePath));
                PrintUsage();
                return 3;
            }

            if (!Directory.Exists(assemblyFolderPath)) {
                System.Console.WriteLine(string.Format("Build ouput folder not found at [{0}]", assemblyFolderPath));
                PrintUsage();
                return 4;
            }
            
            if (File.Exists(outputFilePath)) {
                File.Delete(outputFilePath);
            }

            using (CoverageInfo info = CoverageInfo.CreateFromFile(coverageFilePath,
                new string[] {assemblyFolderPath },
                new string[] { })) {
                CoverageDS data = info.BuildDataSet();
                data.WriteXml(outputFilePath);
            }

            return 0;
        }
        static void PrintUsage() {
            var usageStr = @"CoverageConverter.exe [path to .coverage file] [path to folder with assemblies] [result file path (.coverage.xml)]";
            System.Console.WriteLine(usageStr);
        }
    }
}
