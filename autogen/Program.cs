// See https://aka.ms/new-console-template for more information

// ms
// srd
// v
using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;

const string SRC_FOLDER = "src/";
bool Verbose;
Dictionary<string, string> AcceptedParams = new()
{
    {"ms", "Micro service name" },
    {"srd", "Solution root directory" },
    {"v", "Verbose" },
    {"o", "Objects" },
};

List<string> Projects = new()
{
    "Application",
    "Application.Contracts",
    "Domain",
    "Domain.Shared",
    "EntityFrameworkCore",
    "HttpApi",
    "HttpApi.Client",
    "HttpApi.Host",
    "Web",
};

try
{
    Dictionary<string, string> Options = ProcessArgs(args);
    Verbose = CheckIfVerbose(Options);
    if (Verbose)
    {
        ShowFoundParams(Options);
    }

    string SolutionRootDirectory = GetSolutionRootDirectoryPath(Options);
    string MicroServiceName = GetMicroServiceName(Options);
    Dictionary<string, string> PathsToProjects = GetPathsToProjects(SolutionRootDirectory, MicroServiceName);
    GetAllObjectInfo(PathsToProjects["Domain"]);
}
catch (Exception e)
{
    ConsoleColor currentForegroundColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(e.Message);
    Console.ForegroundColor = currentForegroundColor;
}

void ShowFoundParams(Dictionary<string, string> options)
{
    if (options.Count > 0)
    {
        Console.WriteLine("The following known params found:");
        foreach (string key in AcceptedParams.Keys)
        {
            if (!options.ContainsKey(key))
            {
                continue;
            }
            string keyExplanation = AcceptedParams[key];
            Console.WriteLine($"{keyExplanation}: {options[key]}");
        }
    }
}

string GetSolutionRootDirectoryPath(Dictionary<string, string> options)
{
    if (options.TryGetValue("srd", out var srdString))
    {
        if (Verbose)
        {
            Console.WriteLine($"Solution root directory found in args: {srdString}.");
        }
        return srdString;
    }
    string result = $"{Directory.GetCurrentDirectory()}\\..\\..\\";
    if (Verbose)
    {
        Console.WriteLine($"Solution root directory not found in args. Using current directory and go back two levels: {result}.");
    }
    return result;
}

Dictionary<string, string> ProcessArgs(string[] args)
{
    Dictionary<string, string> result = new();
    foreach (string argument in args)
    {
        string[] splitted = argument.Split('=');
        if (splitted.Length == 2)
        {
            string key = splitted[0];
            if (result.ContainsKey(key))
            {
                throw new Exception($"Duplicated parameter {key} found.");
            }
            result[splitted[0]] = splitted[1];
        }
    }
    return result;
}

Dictionary<string, string> GetPathsToProjects(string solutionRootPath, string microServiceName)
{
    string srcPath = Path.Combine(solutionRootPath, microServiceName, SRC_FOLDER);
    List<string> allDirectories = Directory.GetDirectories(srcPath, "*", SearchOption.TopDirectoryOnly).ToList();
    Dictionary<string, string> result = new();
    foreach (string project in Projects)
    {
        bool found = false;
        foreach (string directory in allDirectories)
        {
            if (!directory.EndsWith(project))
            {
                continue;
            }
            found = true;
            result.Add(project, Path.GetFullPath(directory));
            if (Verbose)
            {
                Console.WriteLine($"Path to project {project} found: {result[project]}.");
            }
            break;
        }
        if (!found)
        {
            throw new Exception($"Path to project {project} not found. Please specify solution root directory using \"srd=PATH_TO_SOLUTION_ROOT_DIRECTORY\".");
        }
    }
    Console.WriteLine($"All paths to projects found.");
    return result;
}

string GetMicroServiceName(Dictionary<string, string> options)
{
    string result = "";
    if (options.TryGetValue("ms", out var msString))
    {
        result = msString;
        if (Verbose)
        {
            Console.WriteLine($"Micro service found in args: {msString}.");
        }
        return result;
    }
    throw new Exception("Micro service name is required. Example: \"dotnet run ms=mdmservice\".");
}

List<ObjectInfo> GetAllObjectInfo(string pathToDomain)
{
    List<ObjectInfo> result = new();
    List<string> allDirectories = Directory.GetDirectories(pathToDomain, "*", SearchOption.TopDirectoryOnly).ToList();
    foreach (string directory in allDirectories)
    {
        var objectInfo = GetObjectInfo(directory);
        if (objectInfo == null)
        {
            continue;
        }
        result.Add(objectInfo);
    }
    return result;
}

ObjectInfo? GetObjectInfo(string directory)
{
    string[] managerFileNames = Directory.GetFiles(directory, "*Manager.cs", SearchOption.TopDirectoryOnly);
    if (managerFileNames.Length < 1)
    {
        return null;
    }
    string[] iRepositoryFileNames = Directory.GetFiles(directory, "I*Repository.cs", SearchOption.TopDirectoryOnly);
    if (iRepositoryFileNames.Length < 1)
    {
        return null;
    }
    var shortestManagerFileName = Path.GetFileNameWithoutExtension(GetShortestStrings(managerFileNames));
    if (string.IsNullOrEmpty(shortestManagerFileName))
    {
        return null;
    }
    var shortestIRepositoryFileName = Path.GetFileNameWithoutExtension(GetShortestStrings(iRepositoryFileNames));
    var possibleObjectName = shortestManagerFileName.Replace("Manager", "");
    if ($"I{possibleObjectName}Repository" != shortestIRepositoryFileName)
    {
        return null;
    }
    string objectName = possibleObjectName;
    string plurals = Path.GetFileName(directory);
    var nameSpace = GetNameSpaceFromFile(managerFileNames[0]);
    if (nameSpace == null)
    {
        return null;
    }
    ObjectInfo result = new (objectName, plurals, nameSpace);
    return result;
}

string? GetShortestStrings(string[] strings)
{
    int minLength = strings.Min(y => y.Length); // this gets you the shortest length of all elements in names
    return strings.FirstOrDefault(x => x.Length == minLength);
}

bool CheckIfVerbose(Dictionary<string, string> options)
{
    if (!options.ContainsKey("v"))
    {
        return false;
    }
    string value = options["v"].ToLower();
    if (value == "t")
    {
        value = "true";
        options["v"] = value;
    }
    if (value != "true")
    {
        return false;
    }
    return true;
}

string? GetNameSpaceFromFile(string pathToFile)
{
    foreach (var line in File.ReadAllLines(pathToFile))
    {
        if (!line.Contains("namespace"))
        {
            continue;
        }
        string[] parts = line.Split(" ");
        if (parts.Length < 2)
        {
            return null;
        }
        return parts[1];
    }
    return null;
}

class ObjectInfo
{
    public string Name { get; set; }
    public string NamePlural { get; set; }
    public string NameSpace { get; set; }

    public ObjectInfo(string name, string namePlural, string nameSpace)
    {
        Name = name;
        NamePlural = namePlural;
        NameSpace = nameSpace;
    }

    public override string ToString()
    {
        return $"{Name}, {NamePlural}, {NameSpace}";
    }
}

