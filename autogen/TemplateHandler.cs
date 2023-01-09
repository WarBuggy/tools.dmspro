using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DMSpro.OMS.Tools.AutoGen;
public class TemplateHandler
{
    public string NewContent { get; }
    public string FilePath { get; }
    public string ModContent { get; }

    public TemplateHandler(ObjectInfo objectInfo, TemplateInfo templateInfo, Dictionary<string, string> pathToProjects)
    {
        FilePath = CreateFilePath(templateInfo, objectInfo, pathToProjects);
        NewContent = "";
        if (!string.IsNullOrEmpty(templateInfo.Content))
        {
            NewContent = CreateContent(templateInfo, objectInfo);
        }
        ModContent = "";
        if (templateInfo.ModList.Count > 0)
        {
            ModContent = ModifyContent(templateInfo, objectInfo, FilePath);
        }
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

    static string CreateContent(TemplateInfo template, ObjectInfo objectInfo)
    {
        StringBuilder builder = new(template.Content);
        builder.Replace("|||Name|||", objectInfo.Name);
        builder.Replace("|||NamePlural|||", objectInfo.NamePlural);
        builder.Replace("|||NameSpace|||", objectInfo.NameSpace);
        builder.Replace("|||NameLowerCase|||", objectInfo.NameLowerCase);
        builder.Replace("|||NamePluralLowerCase|||", objectInfo.NamePluralLowerCase);
        builder.Replace("|||ControllerNameSpace|||", objectInfo.ControllerNameSpace);
        return builder.ToString();
    }

    static string ModifyContent(TemplateInfo template, ObjectInfo objectInfo, string filePath)
    {
        string content = File.ReadAllText(filePath, Encoding.UTF8);
        List<Job> jobs = ProcessTemplatJobList(template, objectInfo);
        foreach (Job job in jobs)
        {
            content = content.Replace(job.Old, job.New);
        }
        return content;
    }

    static List<Job> ProcessTemplatJobList(TemplateInfo templateInfo, ObjectInfo objectInfo)
    {
        List<Job> result = new();
        foreach (Job job in templateInfo.ModList)
        {
            string processOld = job.Old.Replace("|||NamePlural|||", objectInfo.NamePlural);
            processOld = processOld.Replace("|||Name|||", objectInfo.Name);
            string processNew = job.New.Replace("|||NamePlural|||", objectInfo.NamePlural);
            processNew = processNew.Replace("|||Name|||", objectInfo.Name);
            result.Add(new Job() { Old = processOld, New = processNew });
        }
        return result;
    }
}
