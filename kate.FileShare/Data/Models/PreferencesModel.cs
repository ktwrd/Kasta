﻿using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace kate.FileShare.Data.Models;

public class PreferencesModel
{
    [Required]
    [MaxLength(100)]
    public string Key { get; set; }

    [MaxLength(2000)]
    public string Value { get; set; } = "";

    [MaxLength(30)]
    public string ValueKind { get; set; } = "string";

    public void Set(string value)
    {
        ValueKind = "string";
        Value = value;
    }

    public void Set(int value)
    {
        ValueKind = "int";
        Value = value.ToString();
    }

    public void Set(bool value)
    {
        ValueKind = "int";
        Value = (value ? 1 : 0).ToString();
    }

    public int GetInt(int defaultValue)
    {
        if (string.IsNullOrEmpty(Value) || ValueKind != "int")
        {
            return defaultValue;
        }

        if (int.TryParse(Value, out var v))
            return v;

        return defaultValue;
    }

    public string GetString(string defaultValue)
    {
        if (string.IsNullOrEmpty(Value))
            return defaultValue;

        return Value;
    }

    public bool GetBool(bool defaultValue)
    {
        return Value == "1";
    }
}