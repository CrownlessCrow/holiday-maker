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




// ── MENY
bool running = true;                                      // menyn körs tills användaren väljer 0 för avslut
while (running)
{
    Console.WriteLine();
    Console.WriteLine("=== HOLIDAY MAKER ===");
    Console.WriteLine("1. Visa kunder");
    Console.WriteLine("2. Ny kund");
    Console.WriteLine("0. Avsluta");
    Console.Write("Val: ");
    string? choice = Console.ReadLine();                  // läser vad användaren skriver

    switch (choice)                                       // väljer vad som ska hända
    {
        case "1":                                         // ── visa kunder
            foreach (Customer c in await customers.GetCustomersAsync())
            {
                Console.WriteLine($"{c.Id}. {c.FirstName} {c.LastName}, {c.Phone}, {c.Email ?? "(ingen email)"}");
            }
            break;

        case "2":                                         // ── ny kund
            string firstName = ReadRequired("Förnamn: ");
            string lastName  = ReadRequired("Efternamn: ");
            string phone     = ReadRequired("Telefon: ");
            Console.Write("Email (frivillig, Enter för att hoppa över): ");
            string? email = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(email)) email = null;   // tomt → NULL i databasen

            try
            {
                int newId = await customers.CreateCustomerAsync(firstName, lastName, phone, email);
                Console.WriteLine($"✅ Kund skapad med id {newId}");
            }
            catch (MySqlException ex)                     // t.ex. email finns redan (UNIQUE)
            {
                Console.WriteLine($"❌ Kunde inte skapa kund: {ex.Message}");
            }
            break;

        case "0":                                         // ── avsluta
            running = false;
            break;

        default:                                          // ── allt annat
            Console.WriteLine("Ogiltigt val, försök igen.");
            break;
    }
}

// Frågar tills användaren skriver något (tomt svar godkänns inte)
static string ReadRequired(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input)) return input.Trim();
        Console.WriteLine("Fältet får inte vara tomt.");
    }
}
