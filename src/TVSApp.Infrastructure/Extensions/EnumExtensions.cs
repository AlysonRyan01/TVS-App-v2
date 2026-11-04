using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TVS_App.Infrastructure.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        var memberInfo = enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault();

        if (memberInfo == null)
            return enumValue.ToString();

        var displayAttribute = memberInfo.GetCustomAttribute<DisplayAttribute>();

        return displayAttribute?.Name ?? enumValue.ToString();
    }
}