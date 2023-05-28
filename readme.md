# Sharp Packages Normalizer 📦

Have you ever faced the problem of having multiple C# projects and solutions with different packages and versions? 😱

Do you want to quickly and easily normalize your packages across all your projects and solutions? 😎

If yes, then Sharp Packages Normalizer is the tool for you! 🙌

Sharp Packages Normalizer is a simple console application that can scan two folders containing C# projects or app.config files and compare the packages and versions used in each folder. It can also update one folder with the packages and versions from the other folder, or vice versa. 🚀

This tool was created very very very quickly using smart tools so no coding styles or code standards have been set up. It was created due to a personal issue with a lot of projects and solutions, and it is on GitHub if someone needs it. 🤖

## How to use it 💻

To use Sharp Packages Normalizer, you need to pass the paths of the two folders as arguments to the console application. For example:

```bash
SharpPackagesNormalizer.exe C:\Projects\Folder1 C:\Projects\Folder2
```

The application will then find the csproj or app.config file in each folder and load the XML documents of the files. It will create dictionaries to store the packages and versions from each document and output a table comparing them. For example:

```
 Folder1.csproj            Folder2.csproj
 Microsoft.NET.Test.Sdk    16.9.4                    16.9.4
 MSTest.TestAdapter        2.2.3                     2.2.3
 MSTest.TestFramework      2.2.3                     2.2.3
 coverlet.collector        3.0.3                     -
 NUnit                     -                         3.13.2
 NUnit3TestAdapter         -                         4.0.0
```

The application will then check if there are any differences among packages versions (excluding missing packages) and ask you if you want to normalize the packages. If you answer yes, it will ask you which folder to use as a source and update the other folder with the source folder's packages and versions.

## Contributing 🙋‍♂️

Sharp Packages Normalizer is an open source project and welcomes contributions from the community. If you have any suggestions, feedback, or improvements, feel free to open an issue or a pull request on GitHub. You can also fork this repository and make your own changes.

## License 📄

Sharp Packages Normalizer is licensed under the MIT License. See [LICENSE](LICENSE) for more details.

## Disclaimer ⚠️

Sharp Packages Normalizer is provided "as is" without any warranty of any kind. Use it at your own risk and responsibility. The author is not liable for any damages or losses caused by this tool.