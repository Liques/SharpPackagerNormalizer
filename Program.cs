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
            // Declare variables to store the paths of the source and target folders
            string sourceFolder;
            List<string> targetFolders = new List<string>();

            // Check if any arguments were passed
            if (args.Length > 0)
            {
                // Assume the path of the source folder is passed as the first argument
                sourceFolder = args[0];
                // Assume the paths of the target folders are passed as the remaining arguments
                for (int i = 1; i < args.Length; i++)
                {
                    targetFolders.Add(args[i]);
                }
            }
            else
            {
                // Ask the user to enter the path of the source folder
                Console.WriteLine("What folder/csproj/.config file would you like to use as source?");
                sourceFolder = Console.ReadLine();
                // Ask the user to enter the paths of the target folders
                Console.WriteLine("What folders/csproj(s)/.config(s) files would you like to use as targets?");
                string input = Console.ReadLine();
                // Split the input by comma and add each item to the list of target folders
                var items = input.Split(", ;".ToArray());
                foreach (var item in items)
                {
                    targetFolders.Add(item.Trim());
                }
            }

            // Find the csproj or app.config file in the source folder
            string sourceFile = FindFile(sourceFolder);

            // Find multiple csproj or app.config files in each target folder
            List<string> targetFiles = new List<string>();
            foreach (var folder in targetFolders)
            {
                targetFiles.AddRange(FindFiles(folder));
            }

            // Load the XML documents of the files
            XDocument sourceDoc = XDocument.Load(sourceFile);
            List<XDocument> targetDocs = targetFiles.Select(f => XDocument.Load(f)).ToList();

            // Get the root element names of the documents
            string sourceRoot = sourceDoc.Root.Name.LocalName;
            List<string> targetRoots = targetDocs.Select(d => d.Root.Name.LocalName).ToList();

            // Create dictionaries to store the packages and versions from each document
            var sourceDict = new Dictionary<string, string>();
            var targetDicts = new List<Dictionary<string, string>>();

            // Populate the dictionaries with the package id and version from each document
            PopulateDictionary(sourceDict, sourceDoc, sourceRoot);
            foreach (var doc in targetDocs)
            {
                var dict = new Dictionary<string, string>();
                PopulateDictionary(dict, doc, doc.Root.Name.LocalName);
                targetDicts.Add(dict);
            }

            // Compare the packages and versions from each dictionary and output a table
            OutputTable(sourceDict, targetDicts, sourceFile, targetFiles);

            // Check if there are any differences among packages versions
            bool hasDifferences = HasDifferences(sourceDict, targetDicts);

            // If there are differences, ask the user if they want to normalize the packages and perform the update if yes
            if (hasDifferences)
            {
                NormalizePackages(sourceDict, targetDicts, sourceDoc, targetDocs, sourceRoot, targetRoots, sourceFile, targetFiles);
            }
        }

        // A helper method to check if there are any differences among packages versions
        static bool HasDifferences(Dictionary<string, string> sourceDict, List<Dictionary<string, string>> targetDicts)
        {
            // Loop through each key-value pair in the source dictionary
            foreach (var pair1 in sourceDict)
            {
                // Get the id and version of the package
                string id1 = pair1.Key;
                string version1 = pair1.Value;
                // Loop through each target dictionary and find the matching key-value pair 
                foreach (var dict in targetDicts)
                {
                    var pair2 = dict.FirstOrDefault(p => p.Key == id1);
                    // If there is a matching key-value pair and its version is different from the source version, return true
                    if (!pair2.Equals(default(KeyValuePair<string, string>)) && pair2.Value != version1)
                    {
                        return true;
                    }
                }

            }
            // If no differences are found, return false
            return false;
        }


        // A method to find the csproj or app.config file in a folder
        // A method to find the csproj or app.config file in a folder or a file path
        static string FindFile(string folder)
        {
            // Check if the folder is actually a file path with the extension csproj or config
            string extension = Path.GetExtension(folder);
            if (extension == ".csproj" || extension == ".config")
            {
                // Return the file path if it matches
                return folder;
            }

            // Get all the files in the folder
            var files = Directory.GetFiles(folder);

            // Loop through each file and check if it has the extension config
            foreach (var file in files)
            {
                extension = Path.GetExtension(file);
                if (extension == ".config")
                {
                    // Return the file path if it matches
                    return file;
                }
            }

            // Loop through each file and check if it has the extension csproj
            foreach (var file in files)
            {
                extension = Path.GetExtension(file);
                if (extension == ".csproj")
                {
                    // Return the file path if it matches
                    return file;
                }
            }

            // If no matching file is found, throw an exception
            throw new FileNotFoundException($"No csproj or app.config file found in {folder}");
        }

        // A method to find multiple csproj or app.config files in a folder or a file path
        static List<string> FindFiles(string folder)
        {
            // Create a list to store multiple files
            var files = new List<string>();
            // Check if the folder is actually a file path with extension csproj or config
            string extension = Path.GetExtension(folder);
            if (extension == ".csproj" || extension == ".config")
            {
                // Add only one file path to list if it matches
                files.Add(folder);
                return files;
            }
            // Get all files in folder and its subfolders
            var allFiles = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
            // Loop through each file and check if it has extension config
            foreach (var file in allFiles)
            {
                extension = Path.GetExtension(file);
                if (extension == ".config")
                {
                    // Add file path to list if it matches
                    files.Add(file);
                }
            }
            // If no config files are found, loop through each file and check if it has extension csproj
            if (files.Count == 0)
            {
                foreach (var file in allFiles)
                {
                    extension = Path.GetExtension(file);
                    if (extension == ".csproj")
                    {
                        // Add file path to list if it matches
                        files.Add(file);
                    }
                }
            }
            // If no matching files are found throw an exception
            if (files.Count == 0)
            {
                throw new FileNotFoundException($"No csproj or app.config files found in {folder}");
            }
            return files;
        }

        // A method to compare the packages and versions from two dictionaries and output a table
        static void OutputTable(Dictionary<string, string> sourceDict, List<Dictionary<string, string>> targetDicts, string sourceFile, List<string> targetFiles)
        {
            // Get the name of the folder that contains the source file
            string sourceFolder = Path.GetFileName(Path.GetDirectoryName(sourceFile));
            // Print the table header with the source folder name and the number of target projects
            Console.WriteLine($"| Package name | {sourceFolder} | {targetFiles.Count} target projects |");
            // Print a separator line for the table header
            Console.WriteLine("| -------------- ---------- -------------------|");
            // Loop through each key-value pair in the source dictionary
            foreach (var pair1 in sourceDict)
            {
                // Get the id and version of the package
                string id1 = pair1.Key;
                string version1 = pair1.Value;
                // Print the package id and version with white color
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"| ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{id1}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($" | ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"{version1}");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"({sourceFolder}) | ");
                // Loop through each target dictionary and print its version with green or red color depending on whether it matches or not
                for (int i = 0; i < targetDicts.Count; i++)
                {
                    var pair2 = targetDicts[i].FirstOrDefault(p => p.Key == id1);
                    // Get the name of the folder that contains the target file
                    string targetFolder = Path.GetFileName(Path.GetDirectoryName(targetFiles[i]));
                    // If there is no matching key-value pair, print a message with red color
                    if (pair2.Equals(default(KeyValuePair<string, string>)))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write($"missing ({targetFolder})");
                    }
                    else
                    {
                        // Get the version of the matching package
                        string version2 = pair2.Value;
                        // Compare the versions and print them with green color if they are equal or red color if they are different
                        int result = CompareVersions(version1, version2);
                        if (result == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write($"{version2}");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write($"({targetFolder})");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write($"{version2}");
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write($"({targetFolder})");
                        }
                    }
                    // Print a comma or a newline depending on whether it is the last target or not
                    if (i < targetDicts.Count - 1)
                    {
                        Console.Write(", ");
                    }
                    else
                    {
                        Console.WriteLine("|");
                    }
                }
            }
            // Reset text color to default
            Console.ResetColor();
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

        // A method to ask the user if they want to normalize the packages and perform the update if yes
        static void NormalizePackages(Dictionary<string, string> sourceDict, List<Dictionary<string, string>> targetDicts, XDocument sourceDoc, List<XDocument> targetDocs, string sourceRoot, List<string> targetRoots, string sourceFile, List<string> targetFiles)
        {
            // Ask the user if they want to normalize the packages
            Console.WriteLine();
            Console.WriteLine("Do you want to normalize the packages? [Y/N]");
            Console.WriteLine();

            var answer = Console.ReadKey();
            // If the user answers yes, use the source project as a source and update all the target projects
            if (answer.Key == ConsoleKey.Y)
            {
                // Get the name of the folder that contains the source file
                string sourceFolder = Path.GetFileName(Path.GetDirectoryName(sourceFile));
                // List all the packages that are going to be changed from source to targets
                ListPackagesToBeChanged(sourceDict, targetDicts, targetFiles);
                Console.WriteLine();

                Console.WriteLine("Do you confirm this change? After the change, it will be NOT possible to revert this step. [Y/N]");
                answer = Console.ReadKey();
                // If the user answers yes, use the source project as a source and update all the target projects
                if (answer.Key == ConsoleKey.Y)
                {
                    // Loop through each target project and update it with the source project's packages and versions
                    for (int i = 0; i < targetFiles.Count; i++)
                    {
                        UpdateProject(sourceDict, targetDicts[i], targetDocs[i], targetRoots[i], targetFiles[i]);
                    }
                }
                // If the user answers no, do nothing
                else if (answer.Key == ConsoleKey.N)
                {
                    Console.WriteLine();

                    Console.WriteLine("No changes made.");
                }
                // If the user answers anything else, print an invalid input message
                else
                {
                    Console.WriteLine();

                    Console.WriteLine("Invalid input. Please enter Y or N.");
                }
            }
            // If the user answers no, do nothing
            else if (answer.Key == ConsoleKey.N)
            {
                Console.WriteLine("No changes made.");
            }
            // If the user answers anything else, print an invalid input message
            else
            {
                Console.WriteLine("Invalid input. Please enter Y or N.");
            }
        }

        // A helper method to check if a target project is missing any package from the source project
        static bool IsMissingPackage(Dictionary<string, string> sourceDict, Dictionary<string, string> targetDict)
        {
            // Loop through each key-value pair in the source dictionary
            foreach (var pair in sourceDict)
            {
                // Get the id of the package
                string id = pair.Key;
                // Find the matching key-value pair in the target dictionary
                var match = targetDict.FirstOrDefault(p => p.Key == id);
                // If there is no matching key-value pair, return true
                if (match.Equals(default(KeyValuePair<string, string>)))
                {
                    return true;
                }
            }
            // If no missing package is found, return false
            return false;
        }

        // A helper method to list all the packages that are going to be changed
        static void ListPackagesToBeChanged(Dictionary<string, string> sourceDict, List<Dictionary<string, string>> targetDicts, List<string> targetFiles)
        {
            Console.WriteLine();
            // Print a message with the number of packages that are going to be changed
            Console.WriteLine($"The following packages are going to be changed.");
            // Loop through each key-value pair in the source dictionary
            foreach (var pair in sourceDict)
            {
                // Get the id and version of the package
                string id = pair.Key;
                string version = pair.Value;
                // Print the package id and version with white color
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($"{id}: {version} -> ");
                // Loop through each target dictionary and print its version with green or red color depending on whether it matches or not
                for (int i = 0; i < targetDicts.Count; i++)
                {
                    var match = targetDicts[i].FirstOrDefault(p => p.Key == id);
                    // Get the name of the folder that contains the target file
                    string targetFolder = Path.GetFileName(Path.GetDirectoryName(targetFiles[i]));

                    if (!match.Equals(default(KeyValuePair<string, string>)) && match.Value != version)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"{match.Value} ({targetFolder})");
                    }
                    else
                    {
                        continue;
                    }
                    // Print a comma or a newline depending on whether it is the last target or not
                    if (i < targetDicts.Count - 1)
                    {
                        Console.Write(", ");
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
            }
            // Reset text color to default
            Console.ResetColor();
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