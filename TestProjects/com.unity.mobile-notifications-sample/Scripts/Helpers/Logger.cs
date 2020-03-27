﻿using System;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;

public class Logger
{
    public Text logsText;

    public static class Colors
    {
        public const string White = "#ffffff";
        public const string Gray = "#7f8c8d";
        public const string Blue = "#3498db";
        public const string Green = "#2ecc71";
        public const string Orange = "#f1c40f";
        public const string Red = "#e74c3c";
    }

    public Logger (Text textComponent)
    {
        logsText = textComponent;
    }

    private string ProcessText(string text, int tabs = 0)
    {
        return $"{new String('\t', tabs)}{text}\n";
    }

    public Logger Clear()
    {
        logsText.text = "";
        return this;
    }

    public Logger Separator()
    {
        logsText.text += $"\n";
        return this;
    }

    public Logger White(string text, int tabs = 0)
    {
        logsText.text += $"<color={Colors.White}>{ProcessText(text, tabs)}</color>";
        return this;
    }

    public Logger Gray(string text, int tabs = 0)
    {
        logsText.text += $"<color={Colors.Gray}>{ProcessText(text, tabs)}</color>";
        return this;
    }

    public Logger Blue(string text, int tabs = 0)
    {
        logsText.text += $"<color={Colors.Blue}>{ProcessText(text, tabs)}</color>";
        return this;
    }

    public Logger Green(string text, int tabs = 0)
    {
        logsText.text += $"<color={Colors.Green}>{ProcessText(text, tabs)}</color>";
        return this;
    }

    public Logger Orange(string text, int tabs = 0)
    {
        logsText.text += $"<color={Colors.Orange}>{ProcessText(text, tabs)}</color>";
        return this;
    }

    public Logger Red(string text, int tabs = 0)
    {
        logsText.text += $"<color={Colors.Red}>{ProcessText(text, tabs)}</color>";
        return this;
    }

    public Logger Properties(object obj, int tabs = 0)
    {
        foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties().Where(property => property.GetGetMethod() != null))
        {
            object value = propertyInfo.GetValue(obj, null);
            if (string.IsNullOrEmpty($"{value}") || string.IsNullOrWhiteSpace($"{value}")) continue; // Skip empty values
            Blue($"{propertyInfo.Name}: {value}", tabs);
        }
        return this;
    }

}
