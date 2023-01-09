namespace DMSpro.OMS.Tools.AutoGen;
public class ObjectInfo
{
    public string Name { get; }
    public string NamePlural { get; }
    public string NameSpace { get; }
    public string NameLowerCase { get; }
    public string ControllerNameSpace { get; }

    public string NamePluralLowerCase { get; }

    public ObjectInfo(string name, string namePlural, string nameSpace, bool addControllers)
    {
        Name = name;
        NamePlural = namePlural;
        NameSpace = nameSpace;
        NameLowerCase = FirstCharInStringToLowerCase(name);
        NamePluralLowerCase = FirstCharInStringToLowerCase(namePlural);
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

    private static string FirstCharInStringToLowerCase(string input)
    {
        if (!char.IsUpper(input[0]))
        {
            return input;
        }
        else if (input.Length == 1)
        {
            return char.ToLower(input[0]).ToString();
        }
        else
        {
            return char.ToLower(input[0]) + input[1..];
        }
    }

    public override string ToString()
    {
        return Name;
    }
}
