using System;

[AttributeUsage(AttributeTargets.Class)]
public class ModInfoAttribute : Attribute
{
    public string Author { get; }
    public string Description { get; }

    public ModInfoAttribute(string author, string description)
    {
        Author = author;
        Description = description;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ModDescriptionAttribute : Attribute
{
    public string Lang { get; }
    public string Text { get; }

    public ModDescriptionAttribute(string lang, string text)
    {
        Lang = lang;
        Text = text;
    }
}