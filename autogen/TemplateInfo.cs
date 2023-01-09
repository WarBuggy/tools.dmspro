using System.Xml.Serialization;

namespace DMSpro.OMS.Tools.AutoGen;

[XmlRoot("TemplateInfo")]
public class TemplateInfo
{
    public const string TEMPLATE_PATH = "Templates\\";
    public string TemplateName { get; set; }
    public string ParamName { get; set; }
    public string ProjectName { get; set; }
    public string Path { get; set; }
    public string FileName { get; set; }
    public string Content { get; set; }

    public TemplateInfo()
    {
        TemplateName = "";
        ParamName = "";
        ProjectName = "";
        Path = "";
        FileName = "";
        Content = "";
    }

    public override string ToString()
    {
        return $"{TemplateName} ({ParamName})";
    }
}
