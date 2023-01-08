// See https://aka.ms/new-console-template for more information

// ms
// srd
// v
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;

const string SRC_FOLDER = "src/";
bool Verbose;
const string PARAM_MICRO_SERVICE_NAME = "ms";
const string PARAM_SOLUTION_ROOT_PATH = "srd";
const string PARAM_VERBOSE = "v";
const string PARAM_OBJECTS = "o";
const string PARAM_TEMPLATES = "t";
const string PARAM_ADD_CONTROLLERS = "addc";
const string PARAM_OVERRIDING = "override";
Dictionary<string, string> AcceptedParams = new()
{
    {PARAM_MICRO_SERVICE_NAME, "Micro service name" },
    {PARAM_SOLUTION_ROOT_PATH, "Solution root directory" },
    {PARAM_VERBOSE, "Verbose" },
    {PARAM_OBJECTS, "Objects" },
    {PARAM_TEMPLATES, "Templates" },
    {PARAM_ADD_CONTROLLERS, "Add Controllers to controller namespaces" },
    {PARAM_OVERRIDING, "Override existing file or not" },
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
    Verbose = CheckBooleanParam(PARAM_VERBOSE, Options);
    if (Verbose)
    {
        ShowFoundParams(Options);
    }

    string SolutionRootDirectory = GetSolutionRootDirectoryPath(Options);
    string MicroServiceName = GetMicroServiceName(Options);
    Dictionary<string, string> PathsToProjects = GetPathsToProjects(SolutionRootDirectory, MicroServiceName);
    bool AddControllersToControllerNameSpace = CheckBooleanParam(PARAM_ADD_CONTROLLERS, Options);
    List<ObjectInfo> AllObjectInfo = GetAllObjectInfo(PathsToProjects["Domain"], AddControllersToControllerNameSpace);
    List<ObjectInfo> RequiredObjectInfo = GetRequiredObjectInfo(Options, AllObjectInfo);
    List<TemplateInfo> AllTemplateInfo = GetAllTemplateInfo();
    List<TemplateInfo> RequiredTemplateInfo = GetRequiredTemplateInfo(Options, AllTemplateInfo);
    bool Overriding = CheckBooleanParam(PARAM_OVERRIDING, Options);
    CreateTemplatesForObjects(RequiredObjectInfo, RequiredTemplateInfo, PathsToProjects, Overriding);
    ConsoleWriteLineInfo("Generation finished.");
}
catch (Exception e)
{
    ConsoleWriteLineError(e.Message);
}

void CreateTemplatesForObjects(List<ObjectInfo> objectInfoList, List<TemplateInfo> templateInfoList, 
    Dictionary<string, string> pathToProjects, bool overrding)
{
    if (Verbose)
    {
        string isNot = overrding ? "" : "NOT";
        ConsoleWriteLineInfo($"Overriding existing files will {isNot} be performed.");
    }
    foreach (ObjectInfo objectInfo in objectInfoList)
    {
        if (Verbose)
        {
            ConsoleWriteLineInfo($"Generation begins for object {objectInfo}.");
        }
        foreach (TemplateInfo templateInfo in templateInfoList)
        {
            TemplateHandler templateHander = new TemplateHandler(objectInfo, templateInfo, pathToProjects);
            if (!overrding)
            {
                if (File.Exists(templateHander.FilePath))
                {
                    if (Verbose)
                    {
                        Console.WriteLine($"Skipped template {templateInfo} generation for object {objectInfo} (file already exists).");
                    }
                    continue;
                }
            }
            WriteToFile(templateHander.FilePath, templateHander.Content);
            Console.WriteLine($"Template {templateInfo} generated for object {objectInfo}.");
        }
    }
}

void ShowFoundParams(Dictionary<string, string> options)
{
    if (options.Count > 0)
    {
        ConsoleWriteLineInfo("The following known params found:");
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
            ConsoleWriteLineInfo($"Solution root directory found in args: {srdString}.");
        }
        return srdString;
    }
    string result = $"{Directory.GetCurrentDirectory()}\\..\\..\\";
    if (Verbose)
    {
        ConsoleWriteLineInfo($"Solution root directory not found in args. Using current directory and go back two levels: {result}.");
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
    if (Verbose)
    {
        ConsoleWriteLineInfo($"Finding path to all projects...");
    }
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
    if (Verbose)
    {
        ConsoleWriteLineInfo($"All paths to projects found.");
    }
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
            ConsoleWriteLineInfo($"Micro service found in args: {msString}.");
        }
        return result;
    }
    throw new Exception("Micro service name is required. Example: \"dotnet run ms=mdmservice\".");
}

List<ObjectInfo> GetAllObjectInfo(string pathToDomain, bool addControllers)
{
    List<ObjectInfo> result = new();
    List<string> allDirectories = Directory.GetDirectories(pathToDomain, "*", SearchOption.TopDirectoryOnly).ToList();
    foreach (string directory in allDirectories)
    {
        var objectInfo = GetObjectInfo(directory, addControllers);
        if (objectInfo == null)
        {
            continue;
        }
        result.Add(objectInfo);
    }
    if (Verbose)
    {
        ConsoleWriteLineInfo("Found the following objects:");
        foreach (ObjectInfo objectInfo in result)
        {
            Console.WriteLine(objectInfo);
        }
    }
    return result;
}

ObjectInfo? GetObjectInfo(string directory, bool addControllers)
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
    ObjectInfo result = new(objectName, plurals, nameSpace, addControllers);
    return result;
}

List<TemplateInfo> GetAllTemplateInfo()
{
    List<TemplateInfo> result = new();
    string templatePath = Path.Combine(Directory.GetCurrentDirectory(), TemplateInfo.TEMPLATE_PATH);
    string[] templateFileNames = Directory.GetFiles(templatePath, "*.xml", SearchOption.TopDirectoryOnly);
    foreach (string fileName in templateFileNames)
    {
        using (var fileStream = File.Open(fileName, FileMode.Open))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TemplateInfo));
            var templateInfo = (TemplateInfo?)serializer.Deserialize(fileStream);
            if (templateInfo == null)
            {
                continue;
            }
            result.Add(templateInfo);
        }
    }
    if (Verbose)
    {
        ConsoleWriteLineInfo("Found the following templates:");
        foreach (TemplateInfo templateInfo in result)
        {
            Console.WriteLine(templateInfo);
        }
    }
    return result;
}

string? GetShortestStrings(string[] strings)
{
    // this gets you the shortest length of all elements in names
    int minLength = strings.Min(y => y.Length);
    return strings.FirstOrDefault(x => x.Length == minLength);
}

bool CheckBooleanParam(string paramName, Dictionary<string, string> options)
{
    if (!options.ContainsKey(paramName))
    {
        return false;
    }
    string value = options[paramName].ToLower();
    if (value == "t")
    {
        value = "true";
        options[paramName] = value;
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

List<ObjectInfo> GetRequiredObjectInfo(Dictionary<string, string> options, List<ObjectInfo> allObjectInfo)
{
    if (!options.ContainsKey("o"))
    {
        if (Verbose)
        {
            ConsoleWriteLineInfo("No input object found. Templates will be generated for all found objects.");
        }
        return allObjectInfo;
    }
    List<string> inputObject = options["o"].Split(',').ToList();
    List<ObjectInfo> result = new();
    foreach (string input in inputObject)
    {
        foreach (ObjectInfo objectInfo in allObjectInfo)
        {
            if (objectInfo.Name == input)
            {
                result.Add(objectInfo);
            }
        }
    }
    if (Verbose)
    {
        ConsoleWriteLineInfo("Templates will be generated for the following objects:");
        foreach (ObjectInfo objectInfo in result)
        {
            Console.WriteLine(objectInfo);
        }
    }
    return result;
}

void ConsoleWriteLineWithColor(string message, ConsoleColor color)
{
    ConsoleColor currentForegroundColor = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ForegroundColor = currentForegroundColor;
}

void ConsoleWriteLineInfo(string message)
{
    ConsoleWriteLineWithColor(message, ConsoleColor.Green);
}

void ConsoleWriteLineError(string message)
{
    ConsoleWriteLineWithColor(message, ConsoleColor.Red);
}

List<TemplateInfo> GetRequiredTemplateInfo(Dictionary<string, string> options, List<TemplateInfo> allTemplateInfo)
{
    if (!options.ContainsKey("t"))
    {
        if (Verbose)
        {
            ConsoleWriteLineInfo("No input template found. All templates will be used for generation.");
        }
        return allTemplateInfo;
    }
    List<string> inputTemplate = options["t"].Split(',').ToList();
    List<TemplateInfo> result = new();
    foreach (string input in inputTemplate)
    {
        foreach (TemplateInfo templateInfo in allTemplateInfo)
        {
            if (templateInfo.ParamName == input)
            {
                result.Add(templateInfo);
            }
        }
    }
    if (Verbose)
    {
        ConsoleWriteLineInfo("The following templates will be used for generation:");
        foreach (TemplateInfo templateInfo in result)
        {
            Console.WriteLine(templateInfo);
        }
    }
    return result;
}

//Taken from
//https://learn.microsoft.com/en-us/dotnet/api/system.io.file.create?view=net-7.0
void WriteToFile(string path, string content)
{
    try
    {
        // Create the file, or overwrite if the file exists.
        using FileStream fs = File.Create(path);
        byte[] info = new UTF8Encoding(true).GetBytes(content);
        // Add some information to the file.
        fs.Write(info, 0, info.Length);
    }
    catch (Exception ex)
    {
        throw new Exception($"Cannot create file: {ex.Message}");
    }
}

public class ObjectInfo
{
    public string Name { get; }
    public string NamePlural { get; }
    public string NameSpace { get; }
    public string NameLowerCase { get; }
    public string ControllerNameSpace { get; }

    public ObjectInfo(string name, string namePlural, string nameSpace, bool addControllers)
    {
        Name = name;
        NamePlural = namePlural;
        NameSpace = nameSpace;
        if (!char.IsUpper(name[0]))
        {
            NameLowerCase = name;
        }
        else if (name.Length == 1)
        {
            NameLowerCase = char.ToLower(name[0]).ToString();
        }
        else
        {
            NameLowerCase = char.ToLower(name[0]) + name[1..];
        }
        if (!addControllers)
        {
            ControllerNameSpace = nameSpace;
        }
        else
        {
            List<string> nameSpaceParts = nameSpace.Split('.').ToList();
            nameSpaceParts.Insert(nameSpaceParts.Count - 1, "Controllers");
            ControllerNameSpace = string.Join(".", nameSpaceParts);
        }
    }

    public override string ToString()
    {
        return Name;
    }
}

[XmlRoot("TemplateInfo")]
public class TemplateInfo
{
    public static string TEMPLATE_PATH = "Templates\\";
    public string? TemplateName { get; set; }
    public string? ParamName { get; set; }
    public string? ProjectName { get; set; }
    public string? Path { get; set; }
    public string? FileName { get; set; }
    public string? Content { get; set; }

    public override string ToString()
    {
        return $"{TemplateName} ({ParamName})";
    }
}

public class TemplateHandler
{
    public string Content { get; }
    public string FilePath { get; }

    public TemplateHandler(ObjectInfo objectInfo, TemplateInfo templateInfo, Dictionary<string, string> pathToProjects)
    {
        Content = ReplaceContent(templateInfo.Content, objectInfo);
        FilePath = CreateFilePath(templateInfo, objectInfo, pathToProjects);
    }

    static string ReplaceContent(string content, ObjectInfo objectInfo)
    {
        StringBuilder builder = new(content);
        builder.Replace("|||Name|||", objectInfo.Name);
        builder.Replace("|||NamePlural|||", objectInfo.NamePlural);
        builder.Replace("|||NameSpace|||", objectInfo.NameSpace);
        builder.Replace("|||NameLowerCase|||", objectInfo.NameLowerCase);
        builder.Replace("|||ControllerNameSpace|||", objectInfo.ControllerNameSpace);
        return builder.ToString();
    }

    static string CreateFilePath(TemplateInfo templateInfo, ObjectInfo objectInfo, Dictionary<string, string> pathToProjects)
    {
        string pathToProject = pathToProjects[templateInfo.ProjectName];
        string path = Path.Combine(pathToProject, templateInfo.Path, templateInfo.FileName);
        StringBuilder builder = new(path);
        builder.Replace("|||NamePlural|||", objectInfo.NamePlural);
        builder.Replace("|||Name|||", objectInfo.Name);
        return builder.ToString();
    }
}