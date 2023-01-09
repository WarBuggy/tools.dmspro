using System.Text;

namespace DMSpro.OMS.Tools.AutoGen;

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
        builder.Replace("|||NamePluralLowerCase|||", objectInfo.NamePluralLowerCase);
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