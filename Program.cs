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

            // Ask the user if they want to normalize the packages
            Console.WriteLine("Do you want to normalize the packages? [Y/N]");
            string answer = Console.ReadLine();

            // If the user answers yes, ask which project to use as a source
            if (answer.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                // Get the names of the folders that contain the files
                string folder1 = Path.GetFileName(Path.GetDirectoryName(file1));
                string folder2 = Path.GetFileName(Path.GetDirectoryName(file2));

                // Ask the user which project to use as a source
                Console.WriteLine($"From {folder1} to {folder2}? [Y/N]");
                answer = Console.ReadLine();

                // If the user answers yes, use the first project as a source and update the second project
                if (answer.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateProject(dict1, dict2, doc2, root2, file2);
                }
                // If the user answers no, use the second project as a source and update the first project
                else if (answer.Equals("N", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateProject(dict2, dict1, doc1, root1, file1);
                }
                // If the user answers anything else, print an invalid input message
                else
                {
                    Console.WriteLine("Invalid input. Please enter Y or N.");
                }
            }
            // If the user answers no, do nothing
            else if (answer.Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("No changes made.");
            }
            // If the user answers anything else, print an invalid input message
            else
            {
                Console.WriteLine("Invalid input. Please enter Y or N.");
            }
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

                // If there is no matching key-value pair, print a message in the second column with red color
                if (pair2.Equals(default(KeyValuePair<string, string>)))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"| {id1} | {version1} | missing |");
                    Console.ResetColor();
                }
                else
                {
                    // Get the version of the matching package
                    string version2 = pair2.Value;

                    // Compare the versions and print them in the table row with green color if they are equal or red color if they are different
                    int result = CompareVersions(version1, version2);
                    if (result == 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"| {id1} | {version1} | {version2} |");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"| {id1} | {version1} | {version2} |");
                        Console.ResetColor();
                    }
                }
            }

            // Loop through each key-value pair in the second dictionary
            foreach (var pair2 in dict2)
            {
                // Get the id of the package
                string id2 = pair2.Key;

                // Find the matching key-value pair in the first dictionary
                var pair1 = dict1.FirstOrDefault(p => p.Key == id2);

                // If there is no matching key-value pair, print a message in the first column with red color
                if (pair1.Equals(default(KeyValuePair<string, string>)))
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"| {id2} | missing | {pair2.Value} |");
                    Console.ResetColor();
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

        // A method to update a project with the packages and versions from another project
        static void UpdateProject(Dictionary<string, string> sourceDict, Dictionary<string, string> targetDict, XDocument targetDoc, string targetRoot, string targetFile)
        {
            // Get the default namespace from the root element of the target document
            XNamespace ns = targetDoc.Root.GetDefaultNamespace();

            // Get the package elements from the target document depending on the file type
            var packages = targetRoot == "Project" ? targetDoc.Descendants(ns + "PackageReference") : targetDoc.Descendants("package");

            // Create a list to store the packages that have been changed
            var changedPackages = new List<string>();

            // Loop through each package element in the target document
            foreach (var package in packages)
            {
                // Get the id and version of the package depending on the file type and element type
                string id = targetRoot == "Project" ? package.Attribute("Include").Value : package.Attribute("id").Value;
                string version = targetRoot == "Project" ? (package.Element(ns + "Version") != null ? package.Element(ns + "Version").Value : package.Attribute("Version").Value) : package.Attribute("version").Value;

                // Find the matching key-value pair in the source dictionary
                var pair = sourceDict.FirstOrDefault(p => p.Key == id);

                // If there is a matching key-value pair and its version is different from the target version, update it and add it to the list of changed packages
                if (!pair.Equals(default(KeyValuePair<string, string>)) && pair.Value != version)
                {
                    if (targetRoot == "Project")
                    {
                        if (package.Element(ns + "Version") != null)
                        {
                            package.Element(ns + "Version").Value = pair.Value;
                        }
                        else
                        {
                            package.Attribute("Version").Value = pair.Value;
                        }
                    }
                    else
                    {
                        package.Attribute("version").Value = pair.Value;
                    }
                    changedPackages.Add($"{id}: {version} -> {pair.Value}");
                }
            }

            // Save the changes to the target document
            targetDoc.Save(targetFile);

            // Print a message with the number of packages changed
            Console.WriteLine($"{changedPackages.Count} packages changed in {targetFile}");

            // Print the list of packages changed
            foreach (var item in changedPackages)
            {
                Console.WriteLine(item);
            }
        }
    }
}