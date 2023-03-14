using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Maneea.Diff.Tests
{
    public static class EnumExtensions
    {
        /// <summary>
        /// Gets the display name for an enum.
        /// </summary>
        /// <param name="enumValue"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public static string GetDisplayName(this Enum enumValue)
        {
            var enumType = enumValue.GetType();
            var names = new List<string>();
            if (enumType.GetCustomAttribute<FlagsAttribute>() is null)
                return enumValue.GetSingleDisplayName() ?? string.Empty;
            foreach (var e in Enum.GetValues(enumType))
            {
                var flag = (Enum)e;
                if (enumValue.HasFlag(flag))
                {
                    names.Add(GetSingleDisplayName(flag) ?? string.Empty);
                }
            }
            if (names.Count <= 0) throw new ArgumentException();
            if (names.Count == 1) return names.First();
            return string.Join(", ", names);
        }

        /// <summary>
        /// Gets the display value for a single enum flag or 
        /// name of that flag if the display value is not set
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        private static string? GetSingleDisplayName(this Enum flag)
        {
            try
            {
                return flag.GetType()
                    .GetMember(flag.ToString())
                    .First()
                    .GetCustomAttribute<DisplayAttribute>()?.GetName();
            }
            catch
            {
                return flag.ToString();
            }
        }
    }
}

