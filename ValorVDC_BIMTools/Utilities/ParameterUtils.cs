using System.Diagnostics;

namespace ValorVDC_BIMTools.Utilities
{
    /// <summary>
    /// Provides utility methods for working with Revit parameters.
    /// </summary>
    public static class ParameterUtils
    {
        /// <summary>
        /// Gets a parameter from an element, ignoring case.
        /// </summary>
        /// <param name="element">The element to get the parameter from.</param>
        /// <param name="parameterName">The case-insensitive name of the parameter.</param>
        /// <returns>The parameter if found; otherwise, null.</returns>

        private static Parameter GetParameterCaseInsenitive(Element element, string parameterName)
        {
            var parameters = element.Parameters;

            foreach (Parameter parameter in parameters)
                if (string.Equals(parameter.Definition.Name, parameterName, StringComparison.OrdinalIgnoreCase))
                    return parameter;

            return null;
        }
    }
}