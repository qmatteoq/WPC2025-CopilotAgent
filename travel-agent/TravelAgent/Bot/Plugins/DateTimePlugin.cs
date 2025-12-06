using System.ComponentModel;

namespace TravelAgent.Bot.Plugins
{
    public class DateTimePlugin
    {
        /// <summary>
        /// Gets the current date and time information to help with relative date calculations.
        /// </summary>
        /// <returns>Formatted string with current date, time, and contextual information</returns>
        [Description("Gets the current date and time information including day of week, month name, and year. " +
            "Use this function when users ask about flights or travel using relative date expressions like 'next week', " +
            "'next month', 'tomorrow', 'this weekend', or any time-based references.")]
        public static string GetCurrentDateTime()
        {
            var now = DateTime.Now;
            
            return $"""
                Current date: {now:yyyy-MM-dd}
                Current time: {now:HH:mm:ss}
                Day of week: {now:dddd}
                Month: {now:MMMM}
                Year: {now:yyyy}
                """;
        }
    }
}
