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
}