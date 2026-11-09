using System.Text.Json; 
using MySqlConnector;

//Läs lössenordet från appsettings.json (som inte finns på GitHub)
string settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");// Limmar ihop appens mapp + appsettings.json = adressen till inloggningsfilen
using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(settingsPath));// Läser inloggningsfilen och gör om texten till JSON som går att leta i
string connectionString = settings.RootElement // Går in i ConnectionStrings, tar HolidayMaker och sparar inloggningen som text
    .GetProperty("ConnectionStrings")
    .GetProperty("HolidayMaker")
    .GetString()!;

await using var connection = new MySqlConnection(connectionString); // skapa koppling med inloggningen, kopplingen stängs automatiskt när appen är klar
await connection.OpenAsync();// inger upp och loggar in i MySQL

var customers = new CustomerRepository(connection); //skapar en robot som håller kopplingen och jobbar när den får en order

foreach (Customer c in await customers.GetCustomersAsync()) // kör SELECT, ta en kund i taget
{
    Console.WriteLine(c); //skriv ut kunden
}//appen klar o stänger