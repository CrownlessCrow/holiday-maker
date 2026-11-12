using MySqlConnector;

// mall roboten för rum, söker lediga rum
public class RoomRepository(MySqlConnection connection)
{
    // Order: sök lediga rum i en stad mellan två datum för x gäster
    // Order: sök lediga rum med filter och sortering (samma SQL som 2f i 03_queries.sql)
    // pool / restaurant: null = spelar ingen roll, true = MED, false = UTAN
    // maxBeach / maxCenter: null = spelar ingen roll, annars max meter
    public async Task<List<AvailableRoom>> SearchAvailableRoomsAsync(
        string city, DateTime checkIn, DateTime checkOut, int guests,
        bool? pool, bool? restaurant, int? maxBeach, int? maxCenter, bool sortByRating)
    {
        // Sortering: två fasta alternativ i koden (aldrig användarens text) → säkert
        string orderBy = sortByRating
            ? "Accommodation.rating DESC, Room.price_per_night ASC"     // betyg högst först
            : "Room.price_per_night ASC";                                // pris lägst först

        await using var command = new MySqlCommand(
            $"""
            SELECT Room.id, Accommodation.name AS accommodation_name, City.name AS city_name,
                   Room.room_number, Room.capacity, Room.price_per_night, Accommodation.rating,
                   Accommodation.has_pool, Accommodation.has_restaurant,
                   Accommodation.distance_to_beach_meters, Accommodation.distance_to_center_meters
            FROM Room
            JOIN Accommodation ON Room.accommodation_id = Accommodation.id
            JOIN City          ON Accommodation.city_id = City.id
            WHERE City.name = @city
              AND Room.capacity >= @guests
              AND (@pool       IS NULL OR Accommodation.has_pool       = @pool)
              AND (@restaurant IS NULL OR Accommodation.has_restaurant = @restaurant)
              AND (@max_beach  IS NULL OR Accommodation.distance_to_beach_meters  <= @max_beach)
              AND (@max_center IS NULL OR Accommodation.distance_to_center_meters <= @max_center)
              AND NOT EXISTS (
                  SELECT 1 FROM Booking
                  WHERE Booking.room_id = Room.id
                    AND Booking.check_in_date  < @check_out
                    AND Booking.check_out_date > @check_in)
            ORDER BY {orderBy};
            """, connection);

       // Samma som SET-raderna i 2f, men värdena kommer från användarens val och skickas till MySQL
        command.Parameters.AddWithValue("@city", city);
        command.Parameters.AddWithValue("@guests", guests);
        command.Parameters.AddWithValue("@check_in", checkIn);
        command.Parameters.AddWithValue("@check_out", checkOut);
        command.Parameters.AddWithValue("@pool", (object?)pool ?? DBNull.Value);                // TRUE / FALSE / NULL
        command.Parameters.AddWithValue("@restaurant", (object?)restaurant ?? DBNull.Value);   // TRUE / FALSE / NULL
        command.Parameters.AddWithValue("@max_beach", (object?)maxBeach ?? DBNull.Value);      // tal / NULL
        command.Parameters.AddWithValue("@max_center", (object?)maxCenter ?? DBNull.Value);    // tal / NULL
        await command.PrepareAsync(); // MySQL får mallen först, användarens text skickas separat så den aldrig körs som kod. (City, Guests)

        await using var reader = await command.ExecuteReaderAsync(); // skickar frågan till MySQL och får en läsare för svaret (raderna)

        var rooms = new List<AvailableRoom>(); // Skapar en tom lista där varje ledigt rum läggs till
        while (await reader.ReadAsync())
        {
            rooms.Add(new AvailableRoom(
                reader.GetInt32("id"),
                reader.GetString("accommodation_name"),
                reader.GetString("city_name"),
                reader.GetString("room_number"),
                reader.GetInt32("capacity"),
                reader.GetDecimal("price_per_night"),
                reader.GetInt32("rating"),
                reader.GetBoolean("has_pool"),                         // pool ja/nej
                reader.GetBoolean("has_restaurant"),                   // restaurang ja/nej
                reader.GetInt32("distance_to_beach_meters"),           // meter till strand
                reader.GetInt32("distance_to_center_meters")));        // meter till centrum
        }
        return rooms;
    }
}