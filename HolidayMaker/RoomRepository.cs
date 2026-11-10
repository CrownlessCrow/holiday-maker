using MySqlConnector;

// mall roboten för rum, söker lediga rum
public class RoomRepository(MySqlConnection connection)
{
    // Order: sök lediga rum i en stad mellan två datum för x gäster
    public async Task<List<AvailableRoom>> SearchAvailableRoomsAsync(string city, DateTime checkIn, DateTime checkOut, int guests)
    {
        await using var command = new MySqlCommand(
            """
            SELECT Room.id, Accommodation.name AS accommodation_name, City.name AS city_name,
                   Room.room_number, Room.capacity, Room.price_per_night
            FROM Room
            JOIN Accommodation ON Room.accommodation_id = Accommodation.id
            JOIN City          ON Accommodation.city_id = City.id
            WHERE City.name = @city
              AND Room.capacity >= @guests
              AND NOT EXISTS (
                  SELECT 1 FROM Booking
                  WHERE Booking.room_id = Room.id
                    AND Booking.check_in_date  < @check_out
                    AND Booking.check_out_date > @check_in)
            ORDER BY Room.price_per_night ASC;
            """, connection);                                      // samma SQL som 2c, men fasta värden är utbytta mot @luckor (parametrar)

        command.Parameters.AddWithValue("@city", city);            // fyll luckorna
        command.Parameters.AddWithValue("@guests", guests);
        command.Parameters.AddWithValue("@check_in", checkIn);
        command.Parameters.AddWithValue("@check_out", checkOut);
        await command.PrepareAsync();

        await using var reader = await command.ExecuteReaderAsync();   // skicka frågan, få raderna

        var rooms = new List<AvailableRoom>();                     // tom mapp
        while (await reader.ReadAsync())                           // en rad i taget
        {
            rooms.Add(new AvailableRoom(                           // fyll i en blankett
                reader.GetInt32("id"),
                reader.GetString("accommodation_name"),
                reader.GetString("city_name"),
                reader.GetString("room_number"),
                reader.GetInt32("capacity"),
                reader.GetDecimal("price_per_night")));            // DECIMAL → decimal
        }
        return rooms;
    }
}