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
var bookings = new BookingRepository(connection); // robot för bokningar.



// ── MENY
bool running = true; // menyn körs tills användaren väljer 0 för avslut
while (running)
{
    Console.WriteLine();
    Console.WriteLine("--- HOLIDAY MAKER ---");
    Console.WriteLine("1. Visa kunder");
    Console.WriteLine("2. Ny kund");
    Console.WriteLine("3. Sök lediga rum");
    Console.WriteLine("4. Boka rum");
    Console.WriteLine("5. Visa kundens bokning");
    Console.WriteLine("6. Ändra antal gäster");
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
                PrintError($"❌ Kunde inte skapa kund: {ex.Message}");
            }
            break;


        case "3": //sök lediga rum
            {
                string city = ReadRequired("Stad: "); // måste skriva något
                DateTime checkIn = ReadDate("Incheckning (ÅÅÅÅ-MM-DD): "); // Måste skriva ett giltigt datum.
                DateTime checkOut = ReadDate("Utcheckning (ÅÅÅÅ-MM-DD): ");
                if (checkOut <= checkIn)  // samma regel som CHECK i databasen
                {
                    PrintError("Utcheckning måste vara efter incheckning."); //Återgår till menyn
                    break;
                }
                int guests = ReadPositiveInt("Antal gäster: "); //minst 1 gäst, fråga igen vid 0

                List<AvailableRoom> available = await rooms.SearchAvailableRoomsAsync(city, checkIn, checkOut, guests); // roboten söker lediga rum som rymmer alla gäster
                if (available.Count == 0)
                {
                    PrintError("Inga lediga rum hittades.");
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

        case "4": // boka rum
            {
                int customerId = ReadPositiveInt("Kund-id: ");

                if (!await customers.CustomerExistsAsync(customerId))     // finns kunden inte?
                {
                    PrintError("❌ Kund-id finns inte. Välj 1 för att se kunderna.");
                    break;  // tillbaka till menyn
                }


                int roomId = ReadPositiveInt("Rum-id: ");
                DateTime checkIn = ReadDate("Incheckning (ÅÅÅÅ-MM-DD): ");
                DateTime checkOut = ReadDate("Utcheckning (ÅÅÅÅ-MM-DD): ");
                if (checkOut <= checkIn)  // samma regel som CHECK i databasen
                {
                    PrintError("Utcheckning måste vara efter incheckning.");
                    break;
                }
                int guests = ReadPositiveInt("Antal gäster: ");

                try
                {
                    BookingConfirmation? booking = await bookings.CreateBookingAsync(customerId, roomId, guests, checkIn, checkOut);
                    if (booking is null)  // 0 rader sparades, nekad om bokningen är null.
                    {
                        PrintError("❌ Kunde inte boka: rummet är upptaget, finns inte eller rymmer inte så många gäster.");
                        break;
                    }
                    Console.WriteLine($"✅ Bokning skapad! Bokningsnummer: {booking.BookingId}");
                    Console.WriteLine($"   {booking.Nights} nätter × {booking.PricePerNight} kr = {booking.TotalPrice} kr");
                }
                catch (MySqlException ex)  // t.ex. kund-id finns inte.
                {
                    PrintError($"❌ Kunde inte boka: {ex.Message}");
                }
                break;
            }
        case "5": //visa kundens bokningar
            {
                int customerId = ReadPositiveInt("Kund-id: ");// vilken kund
                List<CustomerBooking> customerBookings = await bookings.GetBookingsForCustomerAsync(customerId); // trycker på knappen och får listan

                if (customerBookings.Count == 0) //om listan är tom = kunden har inga bokningar (eller finns inte)
                {
                    PrintError("Kunden har inga bokningar (eller finns inte).");
                    break;                                    // tillbaka till menyn
                }

                Console.WriteLine($"Bokningar för kund {customerId}:");
                foreach (CustomerBooking b in customerBookings) // går igenom alla bokningar, en i taget
                {
                    Console.WriteLine($"Bokning {b.BookingId}: {b.AccommodationName}, rum {b.RoomNumber}, {b.CheckIn:yyyy-MM-dd} → {b.CheckOut:yyyy-MM-dd}, {b.Guests} gäster, {b.TotalPrice} kr");
                }
                break;  // tillbaka till menyn
            }
        case "6":
            {
                int bookingId = ReadPositiveInt("Bokningsnummer: ");
                int guests = ReadPositiveInt("Nytt antal gäster: ");

                bool updated = await bookings.UpdateGuestsAsync(bookingId, guests);
                if (!updated)
                {
                    PrintError("❌ Kunde inte ändra: bokningen finns inte eller rummet rymmer inte så många gäster.");
                    break;
                }
                Console.WriteLine($"✅ Bokning {bookingId} har nu {guests} gäster.");
                break;
            }




        case "0": //avsluta
            running = false;
            break;

        default: //allt annat
            PrintError("Ogiltigt val, försök igen.");
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
        PrintError("Fältet får inte vara tomt.");
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
        PrintError("Skriv datum som ÅÅÅÅ-MM-DD, t.ex. 2026-12-10."); // ogiltigt. säg till och fråga igen
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
        PrintError("Skriv ett heltal större än 0.");
    }
}

// Skriver ut ett felmeddelande i gul färg
static void PrintError(string message)
{
    Console.ForegroundColor = ConsoleColor.Yellow;   // byt textfärg till gul
    Console.WriteLine(message);  // skriv meddelandet
    Console.ResetColor(); // tillbaka till vanlig färg
}