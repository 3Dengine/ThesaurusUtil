// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Binary Enterprises Ltd">
//   Copyright 2019 (c) Binary Enterprises Ltd. All rights reserved.
// </copyright>
// <summary>
//  
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace BinaryEnterprises.ThesaurusUtil
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

    /// <summary>
    /// Defines the <see cref="Program" />
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The Main
        /// </summary>
        /// <param name="args">The args<see cref="string[]"/></param>
        internal static void Main(string[] args)
        {
            if (args != null && args.Length >= 1)
            {
                if (args != null && args.Length > 0 && args[0].Contains("?"))
                {
                    ShowUsage();
                }
                else if (!args[0].ToLowerInvariant().EndsWith(".csv") && !args[0].ToLowerInvariant().EndsWith(".xml"))
                {
                    ShowUsage("Invalid file extension on input file!");
                }
                else
                {
                    string removalList = string.Empty;

                    if (args[0].ToLowerInvariant().EndsWith(".csv"))
                    {
                        if (args.Length == 1)
                        {
                            removalList = CreateXMLFromCSV(args[0]);
                        }
                        else
                        {
                            removalList = CreateXMLFromCSV(args[0], args[1]);
                        }
                    }
                    else
                    {
                        if (args.Length == 1)
                        {
                            CreateCSVFromXML(args[0]);
                        }
                        else
                        {
                            CreateCSVFromXML(args[0], args[1]);
                        }
                    }

                    if (!string.IsNullOrEmpty(removalList))
                    {
                        Console.WriteLine("Names removed as they were found duplicated in the source file, or removal of duplicates left an expansion with only one name:");
                        Console.WriteLine(removalList + Environment.NewLine);
                    }

                    Console.WriteLine("File generated successfully!");
                }
            }
            else
            {
                ShowUsage("Invalid parameters supplied!");
            }

            Console.ReadKey();
        }

        /// <summary>
        /// The CreateXMLFromCSV
        /// </summary>
        /// <param name="inputfile">The input file<see cref="string"/></param>
        /// <param name="outputfile">The output file<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string CreateXMLFromCSV(string inputfile, string outputfile = "")
        {
            List<string> allNames = new List<string>();
            List<string> removals = new List<string>();

            string[] data = File.ReadAllLines(inputfile);

            XNamespace xNamespace = "x-schema:tsSchema.xml";
            XElement root = new XElement("XML", new XAttribute("ID", "Microsoft Search Thesaurus"));
            XElement thesaurus = new XElement(xNamespace + "thesaurus");
            thesaurus.Add(new XElement(xNamespace + "diacritics_sensitive", "0"));

            foreach (var s in data)
            {
                var names = s.Replace("\"", string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> checkedNames = new List<string>();

                foreach (var n in names)
                {
                    if (!allNames.Contains(n))
                    {
                        allNames.Add(n);
                        checkedNames.Add(n);
                    }
                    else
                    {
                        // Log removals to emit on completion
                        if (!removals.Contains(n))
                        {
                            removals.Add(n);
                        }
                    }
                }

                if (checkedNames.Count > 1)
                {
                    XElement expansion = new XElement(xNamespace + "expansion");
                    foreach (var w in checkedNames)
                    {
                        expansion.Add(new XElement(xNamespace + "sub", w.ToLower()));
                    }

                    thesaurus.Add(expansion);
                }
                else if (checkedNames.Count == 1)
                {
                    // Log removals to emit on completion
                    if (!removals.Contains(checkedNames[0]))
                    {
                        removals.Add(checkedNames[0]);
                    }
                }
            }

            root.Add(thesaurus);

            string outputPath = "tsenu.xml";
            if (!string.IsNullOrEmpty(outputfile))
            {
                outputPath = outputfile;
            }

            File.WriteAllText(outputPath, root.ToString());

            return string.Join(",", removals.OrderBy(x => x));
        }

        /// <summary>
        /// The CreateCSVFromXML
        /// </summary>
        /// <param name="inputfile">The input file<see cref="string"/></param>
        /// <param name="outputfile">The output file<see cref="string"/></param>
        private static void CreateCSVFromXML(string inputfile, string outputfile = "")
        {
            StringBuilder sb = new StringBuilder();
            XDocument xml = XDocument.Load(inputfile);
            XNamespace xNamespace = "x-schema:tsSchema.xml";

            foreach (var node in xml.Root.Element(xNamespace + "thesaurus").Elements(xNamespace + "expansion"))
            {
                sb.AppendLine(string.Join(",", node.Elements(xNamespace + "sub").Select(x => x.Value.ToLower())));
            }

            string outputPath = "tsenu.csv";
            if (!string.IsNullOrEmpty(outputfile))
            {
                outputPath = outputfile;
            }

            File.WriteAllText(outputPath, sb.ToString());
        }

        /// <summary>
        /// The ShowUsage
        /// </summary>
        /// <param name="error">The error<see cref="string"/></param>
        private static void ShowUsage(string error = "")
        {
            StringBuilder usage = new StringBuilder();
            usage.AppendLine("THESAURUSUTIL");
            usage.AppendLine("------------");
            usage.AppendLine("Converts a CSV file into a SQL FTS thesaurus file or vice versa (depending on extension of inputfile).");
            usage.AppendLine("CSV needs to be one set of synonyms per line, seperated by commas, no header row." + Environment.NewLine);
            usage.AppendLine("Usage: THESAURUSUTIL inputfile [outputfile]");
            usage.AppendLine("inputfile (Required) - Path of the input file, either CSV or XML");
            usage.AppendLine("outputfile           - Path of the output file, default: tsenu.xml");

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine(error);
                Console.WriteLine();
            }

            Console.WriteLine(usage.ToString());
        }
    }
}
