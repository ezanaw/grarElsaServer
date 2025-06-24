using System.Text.Json;

namespace ElsaServer.Extensions
{
    public static class JsonElementExtensions
    {
        /// <summary>
        /// Attempts to get a boolean value from the specified JsonElement.
        /// </summary>
        /// <param name="element">The JsonElement instance.</param>
        /// <param name="value">When this method returns, contains the boolean value if successful, or default if not.</param>
        /// <returns><c>true</c> if the element represents a boolean; otherwise <c>false</c>.</returns>
        public static bool TryGetBoolean(this JsonElement element, out bool value)
        {
            if (element.ValueKind == JsonValueKind.True)
            {
                value = true;
                return true;
            }

            if (element.ValueKind == JsonValueKind.False)
            {
                value = false;
                return true;
            }

            value = default;
            return false;
        }
    }
}
