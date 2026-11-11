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
    Console.WriteLine("7. Avboka");
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
            // Standard: inga filter (spelar ingen roll), sortera på pris
            bool? pool = null, restaurant = null; // null = spelar ingen roll
            int? maxBeach = null, maxCenter = null; // null = spelar ingen roll
            bool sortByRating = false;  // false = pris, true = betyg

            //  1. Vill operatören ha filter? 
            Console.Write("Vill du lägga till filter? (ja/nej): ");
            bool choosing = Console.ReadLine()?.Trim().ToLower() == "ja"; // ja -> filtermenyn, annat -> standard

            // ── 2. Filtermeny (bara om man svarade ja) ──
            while (choosing)
            {
                Console.WriteLine();
                Console.WriteLine("--- FILTER ---");
                Console.WriteLine($"1. {"Pool",-23}{TriText(pool)}"); // -23 = fyll ut till 23 tecken, texten hamnar till vänster.
                Console.WriteLine($"2. {"Restaurang",-23}{TriText(restaurant)}");
                Console.WriteLine($"3. {"Avstånd till stranden",-23}{(maxBeach is null ? "spelar ingen roll" : "max " + maxBeach + " m")}");
                Console.WriteLine($"4. {"Avstånd till centrum",-23}{(maxCenter is null ? "spelar ingen roll" : "max " + maxCenter + " m")}");
                Console.WriteLine($"5. {"Sortering",-23}{(sortByRating ? "betyg (högst först)" : "pris (lägst först)")}");
                Console.WriteLine("9. Fortsätt till sökning");
                Console.Write("Val: ");

                switch (Console.ReadLine())
                {
                    case "1": pool = NextTriState(pool); break;   // spelar ingen roll → ja → nej i Next_Tri_State funktionen.
                    case "2": restaurant = NextTriState(restaurant); break;
                    case "3": maxBeach  = ReadOptionalInt("Max meter till strand (Enter = spelar ingen roll): "); break;
                    case "4": maxCenter = ReadOptionalInt("Max meter till centrum (Enter = spelar ingen roll): "); break;
                    case "5": sortByRating = !sortByRating; break; // växla pris/betyg
                    case "9": choosing = false; break;    // klar -> vidare
                    default:  PrintError("Ogiltigt val."); break;
                }
            }

            // 3. Stad, datum och gäster 
            string city       = ReadRequired("Stad: ");
            DateTime checkIn  = ReadDate("Incheckning (ÅÅÅÅ-MM-DD): ");
            DateTime checkOut = ReadDate("Utcheckning (ÅÅÅÅ-MM-DD): ");
            if (checkOut <= checkIn)   // samma regel som CHECK i databasen
            {
                PrintError("Utcheckning måste vara efter incheckning.");
                break;
            }
            int guests = ReadPositiveInt("Antal gäster: ");

            //4. Sök
            List<AvailableRoom> available = await rooms.SearchAvailableRoomsAsync(
                city, checkIn, checkOut, guests, pool, restaurant, maxBeach, maxCenter, sortByRating);

            if (available.Count == 0)
            {
                PrintError("Inga lediga rum hittades.");

               
                if (maxBeach is not null)
                {
                    List<AvailableRoom> withoutBeach = await rooms.SearchAvailableRoomsAsync( //Ny låda withoutbeach, andra sökning då jag kalla denna för rekommendations sökning.
                        city, checkIn, checkOut, guests, pool, restaurant, null, maxCenter, sortByRating);

                    if (withoutBeach.Count > 0) //Om jag inte hitta så händer inget.
                    {
                        int nearest = withoutBeach.Min(r => r.DistanceToBeach); // kortaste avståndet bland lediga rum LinQ metoden.
                                                Console.WriteLine($"Tips: närmaste lediga alternativ ligger {nearest} m från stranden:");
                        foreach (AvailableRoom r in withoutBeach.Where(r => r.DistanceToBeach == nearest))  // Where behåller bara rummen på närmaste avstånd, t.ex. nearest = 50 -> Demacia 102.
                        {
                            Console.WriteLine($"  Rum-id {r.RoomId}: {r.AccommodationName}, rum {r.RoomNumber}, {r.Capacity} pers, {r.PricePerNight} kr/natt");
                        }
                    }
                }

       
                if (maxCenter is not null)
                {
                    List<AvailableRoom> withoutCenter = await rooms.SearchAvailableRoomsAsync(
                        city, checkIn, checkOut, guests, pool, restaurant, maxBeach, null, sortByRating);

                    if (withoutCenter.Count > 0)
                    {
                        int nearest = withoutCenter.Min(r => r.DistanceToCenter);
                                                Console.WriteLine($"Tips: närmaste lediga alternativ ligger {nearest} m från centrum:");
                        foreach (AvailableRoom r in withoutCenter.Where(r => r.DistanceToCenter == nearest))
                        {
                            Console.WriteLine($"  Rum-id {r.RoomId}: {r.AccommodationName}, rum {r.RoomNumber}, {r.Capacity} pers, {r.PricePerNight} kr/natt");
                        }
                    }
                }
                break;
            }
            int nights = (checkOut - checkIn).Days; // antal nätter

            // Visa vilka filter som användes
            Console.WriteLine($"Filter: pool {TriText(pool)}, restaurang {TriText(restaurant)}, " +
                              $"avstånd till stranden {(maxBeach is null ? "spelar ingen roll" : "max " + maxBeach + " m")}, " + //ternary operator
                              $"avstånd till centrum {(maxCenter is null ? "spelar ingen roll" : "max " + maxCenter + " m")}, " + //ternary operator
                              $"sortering {(sortByRating ? "betyg" : "pris")}"); //ternary operator

            Console.WriteLine($"Lediga rum i {city}, {nights} nätter:");
            foreach (AvailableRoom r in available)                   // ett rum i taget
            {
                string facilities = (r.HasPool ? "pool " : "") + (r.HasRestaurant ? "restaurang " : "");
                if (facilities == "") facilities = "inga faciliteter ";
                Console.WriteLine($"Rum-id {r.RoomId}: {r.AccommodationName} ({r.City}), rum {r.RoomNumber}, {r.Capacity} pers, " +
                                  $"{r.PricePerNight} kr/natt, betyg {r.Rating}/5 | {facilities}| " +
                                  $"{r.DistanceToBeach} m till strand, {r.DistanceToCenter} m till centrum");
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
        case "7": //avboka
            {
                int bookingId = ReadPositiveInt("Bokningsnummer att avboka: "); 

                Console.Write($"Är du säker på att du vill avboka bokning {bookingId}? (ja/nej): ");
                if (Console.ReadLine()?.Trim().ToLower() != "ja") // allt utom "j" = avbryt
                {
                    Console.WriteLine("Avbokningen avbröts.");
                    break;
                }

                bool deleted = await bookings.DeleteBookingAsync(bookingId); // roboten försöker ta bort
                if (!deleted) // 0 rader = fanns inte
                {
                    PrintError($"❌ Bokning {bookingId} finns inte.");
                    break;
                }
                Console.WriteLine($"✅ Bokning {bookingId} är avbokad.");
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

// Skriver ut ett felmeddelande i röd färg
static void PrintError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;   // byt textfärg till röd
    Console.WriteLine(message);  // skriv meddelandet
    Console.ResetColor(); // tillbaka till vanlig färg
}

static void PrintSuccess(string message)
{
    Console.ForegroundColor = ConsoleColor.Green;   // grön = lyckadtes
    Console.WriteLine(message);
    Console.ResetColor();
}
static void PrintTip(string message)
{
    Console.ForegroundColor = ConsoleColor.Cyan;    // blå = tips/förslag
    Console.WriteLine(message);
    Console.ResetColor();
}


// Byter läge: spelar ingen roll (null) → ja (true) → nej (false) → spelar ingen roll ...
static bool? NextTriState(bool? value) => value switch
{
    null  => true,
    true  => false,
    false => null
};

// Text för ett filter i tre lägen
static string TriText(bool? value) => value switch
{
    null  => "spelar ingen roll",
    true  => "ja",
    false => "nej"
};

// Frågar efter ett heltal >= 0, Enter = spelar ingen roll (null)
static int? ReadOptionalInt(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return null;                        // Enter → spelar ingen roll
        if (int.TryParse(input, out int number) && number >= 0) return number;   // giltigt tal
        PrintError("Skriv ett heltal (0 eller mer), eller tryck Enter.");
    }
}