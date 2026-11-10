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
var rooms = new RoomRepository(connection); // Robot för rum



// ── MENY
bool running = true; // menyn körs tills användaren väljer 0 för avslut
while (running)
{
    Console.WriteLine();
    Console.WriteLine("=== HOLIDAY MAKER ===");
    Console.WriteLine("1. Visa kunder");
    Console.WriteLine("2. Ny kund");
    Console.WriteLine("3. Sök lediga rum");
    Console.WriteLine("0. Avsluta");
    Console.Write("Val: ");
    string? choice = Console.ReadLine();  // läser vad användaren skriver

    switch (choice)      //Gjort switch case för o väljer vad som ska hända
    {
        case "1": //visa kunder
            foreach (Customer c in await customers.GetCustomersAsync())
            {
                Console.WriteLine($"{c.Id}. {c.FirstName} {c.LastName}, {c.Phone}, {c.Email ?? "(ingen email)"}");
            }
            break;

        case "2":// ny kund
            string firstName = ReadRequired("Förnamn: ");
            string lastName = ReadRequired("Efternamn: ");
            string phone = ReadRequired("Telefon: ");
            Console.Write("Email (frivillig, Enter för att hoppa över): ");
            string? email = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(email)) email = null; // tomt → NULL i databasen

            try
            {
                int newId = await customers.CreateCustomerAsync(firstName, lastName, phone, email);
                Console.WriteLine($"✅ Kund skapad med id {newId}");
            }
            catch (MySqlException ex) // t.ex. email finns redan (UNIQUE)
            {
                Console.WriteLine($"❌ Kunde inte skapa kund: {ex.Message}");
            }
            break;


        case "3": //sök lediga rum
            {
                string city = ReadRequired("Stad: "); // måste skriva något
                DateTime checkIn = ReadDate("Incheckning (ÅÅÅÅ-MM-DD): "); // Måste skriva ett giltigt datum.
                DateTime checkOut = ReadDate("Utcheckning (ÅÅÅÅ-MM-DD): ");
                if (checkOut <= checkIn)  // samma regel som CHECK i databasen
                {
                    Console.WriteLine("Utcheckning måste vara efter incheckning."); //Återgår till menyn
                    break;
                }
                int guests = ReadPositiveInt("Antal gäster: "); //minst 1 gäst, fråga igen vid 0

                List<AvailableRoom> available = await rooms.SearchAvailableRoomsAsync(city, checkIn, checkOut, guests); // roboten söker lediga rum som rymmer alla gäster
                if (available.Count == 0)
                {
                    Console.WriteLine("Inga lediga rum hittades.");
                    break; //tillbaka till meny om rum inte hittades
                }

                int nights = (checkOut - checkIn).Days;       // antal nätter t.ex (13 dec utcheck) - (10 dec incheck) blir 3 nätter
                Console.WriteLine($"Lediga rum i {city}, {nights} nätter:"); 
                foreach (AvailableRoom r in available) //Går genom alla lediga rum
                {
                    Console.WriteLine($"Rum-id {r.RoomId}: {r.AccommodationName}, rum {r.RoomNumber}, {r.Capacity} pers, {r.PricePerNight} kr/natt"); //texten
                }
                break;
            }

        case "0": //avsluta
            running = false;
            break;

        default: //allt annat
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

// Frågar tills användaren skriver ett giltigt datum (ÅÅÅÅ-MM-DD)
static DateTime ReadDate(string prompt)// IN frågan, UT ett datum
{
    while (true)
    {
        Console.Write(prompt); // visar frågan, t.ex incheckning (ÅÅÅÅ-MM-DD)
        if (DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd", null,  // försöker göra om texten till ett datum (ÅÅÅÅ-MM-DD)
                System.Globalization.DateTimeStyles.None, out DateTime date)) // text till datum i formatet ÅÅÅÅ-MM-DD (null = datorns land)
            return date;   // giltigt, skicka tillbaka datumet
        Console.WriteLine("Skriv datum som ÅÅÅÅ-MM-DD, t.ex. 2026-12-10."); // ogiltigt. säg till och fråga igen
    }
}

// Frågar tills användaren skriver ett heltal större än 0
static int ReadPositiveInt(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int number) && number > 0) // gör om texten till ett heltal och  att den ska vara större än 0.
            return number;
        Console.WriteLine("Skriv ett heltal större än 0.");
    }
}
