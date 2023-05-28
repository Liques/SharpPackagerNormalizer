using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

namespace ComparePackageVersions
{
    class Program
    {
        static void Main(string[] args)
        {
            // Assume the paths of the two files are passed as arguments
            string file1 = args[0];
            string file2 = args[1];

            // Load the XML documents of the files
            XDocument doc1 = XDocument.Load(file1);
            XDocument doc2 = XDocument.Load(file2);

            // Get the root element names of the documents
            string root1 = doc1.Root.Name.LocalName;
            string root2 = doc2.Root.Name.LocalName;

            // Create dictionaries to store the packages and versions from each document
            var dict1 = new Dictionary<string, string>();
            var dict2 = new Dictionary<string, string>();

            // Populate the dictionaries with the package id and version from each document
            PopulateDictionary(dict1, doc1, root1);
            PopulateDictionary(dict2, doc2, root2);

            // Compare the packages and versions from each dictionary and output a table
            OutputTable(dict1, dict2, file1, file2);
        }

        // A method to populate a dictionary with the package id and version from an XML document
        static void PopulateDictionary(Dictionary<string, string> dict, XDocument doc, string root)
        {
            // Get the default namespace from the root element
            XNamespace ns = doc.Root.GetDefaultNamespace();

            // Get the package elements from the document depending on the file type
            var packages = root == "Project" ? doc.Descendants(ns + "PackageReference") : doc.Descendants("package");

            // Loop through each package element and add it to the dictionary
            foreach (var package in packages)
            {
                // Get the id and version of the package depending on the file type and the element type
                string id = root == "Project" ? package.Attribute("Include").Value : package.Attribute("id").Value;
                string version = root == "Project" ? (package.Element(ns + "Version") != null ? package.Element(ns + "Version").Value : package.Attribute("Version").Value) : package.Attribute("version").Value;
                dict.Add(id, version);
            }
        }

        // A method to compare the packages and versions from two dictionaries and output a table
        static void OutputTable(Dictionary<string, string> dict1, Dictionary<string, string> dict2, string file1, string file2)
        {
            // Get the names of the folders that contain the files
            string folder1 = Path.GetFileName(Path.GetDirectoryName(file1));
            string folder2 = Path.GetFileName(Path.GetDirectoryName(file2));

            // Print the table header with the folder names
            Console.WriteLine($"| Package name | {folder1} | {folder2} |");

            // Print a separator line for the table header
            Console.WriteLine("|--------------|----------|----------|");

            // Loop through each key-value pair in the first dictionary
            foreach (var pair1 in dict1)
            {
                // Get the id and version of the package
                string id1 = pair1.Key;
                string version1 = pair1.Value;

                // Find the matching key-value pair in the second dictionary
                var pair2 = dict2.FirstOrDefault(p => p.Key == id1);

                // If there is no matching key-value pair, print a message in the second column
                if (pair2.Equals(default(KeyValuePair<string, string>)))
                {
                    Console.WriteLine($"| {id1} | {version1} | missing |");
                }
                else
                {
                    // Get the version of the matching package
                    string version2 = pair2.Value;

                    // Print both versions in the table row
                    Console.WriteLine($"| {id1} | {version1} | {version2} |");
                }
            }

            // Loop through each key-value pair in the second dictionary
            foreach (var pair2 in dict2)
            {
                // Get the id of the package
                string id2 = pair2.Key;

                // Find the matching key-value pair in the first dictionary
                var pair1 = dict1.FirstOrDefault(p => p.Key == id2);

                // If there is no matching key-value pair, print a message in the first column
                if (pair1.Equals(default(KeyValuePair<string, string>)))
                {
                    Console.WriteLine($"| {id2} | missing | {pair2.Value} |");
                }
            }
        }

        // A helper method to compare two version strings
        static int CompareVersions(string v1, string v2)
        {
            // Split the strings by dots and convert to integers
            int[] parts1 = v1.Split('.').Select(int.Parse).ToArray();
            int[] parts2 = v2.Split('.').Select(int.Parse).ToArray();

            // Loop through each part and compare them
            for (int i = 0; i < Math.Min(parts1.Length, parts2.Length); i++)
            {
                if (parts1[i] > parts2[i])
                {
                    return 1; // v1 is greater than v2
                }
                else if (parts1[i] < parts2[i])
                {
                    return -1; // v1 is less than v2
                }
            }

            // If all parts are equal so far, compare the length of the strings
            if (parts1.Length > parts2.Length)
            {
                return 1; // v1 is greater than v2
            }
            else if (parts1.Length < parts2.Length)
            {
                return -1; // v1 is less than v2
            }
            else
            {
                return 0; // v1 is equal to v2
            }
        }
    }

}
