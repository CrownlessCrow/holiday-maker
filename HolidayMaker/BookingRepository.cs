using MySqlConnector;

// roboten för bokningar
public class BookingRepository(MySqlConnection connection)
{
    // Order: boka ett rum. ger kvitto, eller null om rummet inte gick att boka
    public async Task<BookingConfirmation?> CreateBookingAsync(int customerId, int roomId, int guests, DateTime checkIn, DateTime checkOut)
    {
        // Steg 3 från 03_queries.sql: sparar BARA om rummet rymmer gästerna och är ledigt
        await using var command = new MySqlCommand( //skriver lappen: SQL mallen med tomma @luckor (skickas inte än)
            """
            INSERT INTO Booking (customer_id, room_id, number_of_guests, check_in_date, check_out_date)
            SELECT @customer_id, @room_id, @guests, @check_in, @check_out
            FROM Room
            WHERE Room.id = @room_id
              AND Room.capacity >= @guests
              AND NOT EXISTS (
                  SELECT 1 FROM Booking
                  WHERE Booking.room_id = @room_id
                    AND Booking.check_in_date  < @check_out
                    AND Booking.check_out_date > @check_in);
            """, connection);

        command.Parameters.AddWithValue("@customer_id", customerId);
        command.Parameters.AddWithValue("@room_id", roomId);
        command.Parameters.AddWithValue("@guests", guests);
        command.Parameters.AddWithValue("@check_in", checkIn);
        command.Parameters.AddWithValue("@check_out", checkOut);
        await command.PrepareAsync(); // MySQL läser mallen och gör sig redo, värdena kommer separat (förbereder)

        int rows = await command.ExecuteNonQueryAsync();  // MySQL försöker spara och sedan svarar med antal sparade rader: 1 = bokad, 0 = nekad
        if (rows == 0) return null;  // upptaget / rymmer inte / rummet finns inte

        int bookingId = checked((int)command.LastInsertedId);  // vår nya boknings nummer (AUTO_INCREMENT, som en kölapp)

        // Räkna ut priset: nätter × pris per natt
        await using var priceCommand = new MySqlCommand( // skriver en ny lapp (SELECT för priset)
            """
            SELECT DATEDIFF(Booking.check_out_date, Booking.check_in_date) AS nights,
                   Room.price_per_night,
                   DATEDIFF(Booking.check_out_date, Booking.check_in_date) * Room.price_per_night AS total_price
            FROM Booking
            JOIN Room ON Booking.room_id = Room.id
            WHERE Booking.id = @booking_id;
            """, connection);
        priceCommand.Parameters.AddWithValue("@booking_id", bookingId);
        await priceCommand.PrepareAsync();   // skickar mallen till MySQL så den kan förbereda sig

        await using var reader = await priceCommand.ExecuteReaderAsync(); //  // skickar bokningsnumret, kör frågan och tar emot svaret
        await reader.ReadAsync();   //läser en rad som den får alltså svaret.
        return new BookingConfirmation( //Leverera det lovade kvittot som vi lovade i början alltså Booking Confirmation.
            bookingId,
            reader.GetInt32("nights"),
            reader.GetDecimal("price_per_night"),
            reader.GetDecimal("total_price"));
    }

    // 4d i 03_queries.sql: kundens bokningar med totalpris (nätter × pris), sorterade på datumz
    public async Task<List<CustomerBooking>> GetBookingsForCustomerAsync(int customerId)// IN: kund-id, t.ex. 1 (Ailis). UT: lovar en lista med CustomerBooking (record), kan vara tom
    {
        await using var command = new MySqlCommand(
            """ 
            SELECT Booking.id, Accommodation.name AS accommodation_name, Room.room_number,
                   Booking.check_in_date, Booking.check_out_date, Booking.number_of_guests,
                   DATEDIFF(Booking.check_out_date, Booking.check_in_date) * Room.price_per_night AS total_price
            FROM Booking
            JOIN Room          ON Booking.room_id = Room.id
            JOIN Accommodation ON Room.accommodation_id = Accommodation.id
            WHERE Booking.customer_id = @customer_id
            ORDER BY Booking.check_in_date;
            """, connection);                                      // samma SQL som 4d, men med @lucka

        command.Parameters.AddWithValue("@customer_id", customerId);  // fyll luckan med kund-id
        await command.PrepareAsync();  // skickar mallen till MySQL så den kan förbereda sig

        await using var reader = await command.ExecuteReaderAsync(); // skickar kund-id, kör frågan och tar emot svaret (raderna)

        var customerBookings = new List<CustomerBooking>(); // tom mapp, kan bara innehålla CustomerBooking
        while (await reader.ReadAsync()) // läser en bokning i taget tills det är slut (kan vara flera)
        {
            customerBookings.Add(new CustomerBooking( // fyller i ett bokningskort och lägger i mappen
                reader.GetInt32("id"),
                reader.GetString("accommodation_name"),
                reader.GetString("room_number"),
                reader.GetDateTime("check_in_date"),  // DATE till DateTime
                reader.GetDateTime("check_out_date"),
                reader.GetInt32("number_of_guests"),
                reader.GetDecimal("total_price")));
        }
        return customerBookings; //löftet uppfylls  och return.
    }
    // Order: ändra antal gäster på en bokning (steg 5 i 03_queries.sql)
    // IN: bokningsnummer + nytt antal gäster. UT: true = ändrad, false = nekad
    public async Task<bool> UpdateGuestsAsync(int bookingId, int guests)
    {
        await using var command = new MySqlCommand( // skriver lappen: UPDATE med @luckor
            """
            UPDATE Booking
            JOIN Room ON Booking.room_id = Room.id
            SET Booking.number_of_guests = @guests
            WHERE Booking.id = @booking_id
              AND Room.capacity >= @guests;
            """, connection); // ändrar BARA om gästerna ryms i rummet

        command.Parameters.AddWithValue("@guests", guests);  // fyll lucka: nytt antal
        command.Parameters.AddWithValue("@booking_id", bookingId); // fyll lucka: vilken bokning
        await command.PrepareAsync();  // MySQL förbereder sig

        int rows = await command.ExecuteNonQueryAsync();    // antal rader som matchade: 1 = ändrad, 0 = nekad
        return rows == 1;  // true om ändrad, annars false
    }
        // Order: avboka (ta bort) en bokning (steg 6 i 03_queries.sql)
    // IN: bokningsnummer. UT: true = avbokad, false = bokningen fanns inte
    public async Task<bool> DeleteBookingAsync(int bookingId)
    {
        await using var command = new MySqlCommand( // skriver lappen: DELETE med @lucka
            "DELETE FROM Booking WHERE id = @booking_id;", connection);

        command.Parameters.AddWithValue("@booking_id", bookingId); // fyll lucka: vilken bokning
        await command.PrepareAsync(); // MySQL förbereder sig

        int rows = await command.ExecuteNonQueryAsync();// antal borttagna rader: 1 = avbokad, 0 = fanns inte
        return rows == 1;   // true om avbokad, annars false
    }
}