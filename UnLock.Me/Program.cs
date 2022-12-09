using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using System.Reflection;
using System.Text;

namespace UnLock.Me
{
    public class Program
    {
        public static async Task Main(string[] urls)
        {
            var options = new EdgeOptions();
            var driverService = EdgeDriverService.CreateDefaultService(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            driverService.HideCommandPromptWindow = true;
            driverService.SuppressInitialDiagnosticInformation = true;
            options.AddArgument("headless");
            options.AddArgument("--silent");
            options.AddArgument("log-level=3");

            var driver = new EdgeDriver(driverService, options);
            _ = driver.Manage().Timeouts().ImplicitWait;

            new WebDriverWait(driver, TimeSpan.FromSeconds(1))
                .Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")
                .Equals("complete"));

            foreach (var url in urls)
            {
                driver.Url = url;
                await foreach (var room in GetRoomsData(driver))
                {
                    StoreData(room);
                }
            }

            driver.Quit();
        }

        static void StoreData(Room room)
        {
            var builder = new StringBuilder();

            builder.Append(room.Name);
            builder.Append(',');
            builder.Append(DateTime.Now.Date.ToString("yyyy-MM-dd"));
            builder.Append(',');
            builder.Append(DateTime.Now.DayOfWeek);
            builder.Append(',');
            builder.Append(room.TotalHours);
            builder.Append(',');
            builder.Append(room.BookedHours);

            var text = builder.ToString();

            Console.WriteLine(text);
        }

        static async IAsyncEnumerable<Room> GetRoomsData(IWebDriver driver)
        {
            var roomElements = await RetryCount(() => driver.FindElements(By.CssSelector("li.room")));

            foreach (var roomElement in roomElements)
            {
                yield return GetRoomData(roomElement);
            }
        }

        static Room GetRoomData(IWebElement room)
        {
            var hours = room.FindElements(By.CssSelector("a.hour"));
            var hoursBooked = room.FindElements(By.CssSelector("a.hour.booked"));
            var name = room.FindElement(By.CssSelector("h3"));

            return new Room
            {
                Name = name.Text,
                TotalHours = hours.Count,
                BookedHours = hoursBooked.Count,
            };
        }

        static async Task<IReadOnlyCollection<T>> RetryCount<T>(Func<IReadOnlyCollection<T>> act, int tries = 5, int ms = 1000)
        {
            var result = act();

            while(tries-- > 0 && result.Count == 0)
            {
                await Task.Delay(ms);
                result = act();
            }

            return result;
        }
    }
}