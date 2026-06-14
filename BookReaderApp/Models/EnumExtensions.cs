using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace BookReaderApp.Models;

public static class EnumExtensions
{
    // Returns the [Display(Name = ...)] text for an enum value, or its name as a fallback.
    public static string DisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.ToString();
    }
}
