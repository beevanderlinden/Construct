using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Construct.Domain.Common
{


    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value, bool toLower = !true)
        {
            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString());
            var returnVal = value.ToString(); // fallback default

            if (memberInfo.Length > 0)
            {
                var displayAttr = memberInfo[0].GetCustomAttribute<DisplayAttribute>();
                if (displayAttr != null)
                {
                    // Check if the display name is set to "None" or empty
                    if (displayAttr.GetName() == "None" || string.IsNullOrEmpty(displayAttr.GetName()))
                        returnVal = string.Empty; // Return empty string if display name is "None" or empty
                    // Check if the display name is set to "Default"
                    if (displayAttr.GetName() == "Default")
                        returnVal = memberInfo[0].Name; // Return the enum name if display name is "Default"

                    // Otherwise, return the display name
                    var name = displayAttr.GetName();
                    if (name != null)
                        returnVal = name;
                }

                // Fallback: check for Description attribute
                var descriptionAttr = memberInfo[0].GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttr != null)
                    returnVal = descriptionAttr.Description;
            }

            // Fallback: default ToString() 
            if (toLower)
                returnVal = returnVal.ToLower();

            return returnVal;
        }

        public static string? GetDisplayShortName(this Enum value)
        {
            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString());

            if (memberInfo.Length > 0)
            {
                var displayAttr = memberInfo[0].GetCustomAttribute<DisplayAttribute>();
                if (displayAttr != null)
                {
                    return displayAttr.GetShortName();
                }
            }

            // default
            return null;
        }


    }

}
