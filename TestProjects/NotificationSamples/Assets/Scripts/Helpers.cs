using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helpers : MonoBehaviour
{
    public static void LogProperties(object obj, Logger logger)
    {
        List<string> nullProperties = new List<string>();
        foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties().Where(property => property.GetGetMethod() != null))
        {
            if (propertyInfo.GetValue(obj, null) == null)
            {
                nullProperties.Add(propertyInfo.Name);
            }
            else
            {
                logger.Blue($"{propertyInfo.Name}: {propertyInfo.GetValue(obj, null)}", 1);
            }
        }
        if (nullProperties.Count > 0)
        {
            logger.Gray($"Null Properties: [{string.Join(",", nullProperties)}]", 1);
        }
    }
}
