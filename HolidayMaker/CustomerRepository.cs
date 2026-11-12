using MySqlConnector;                                              // verktyg för mySQL

// Roboten för kunder, all SQL för customer tabellen samlas här
public class CustomerRepository(MySqlConnection connection)        // roboten får telefonen (connection)
{
    // Order: hämta alla kunder
    public async Task<List<Customer>> GetCustomersAsync()          // svarar med en lista av kunder, men vi får vänta med den. (lovar en lista)
    {
        await using var command = new MySqlCommand(                 // förbereder SELECT frågan (skickas inte än)
            "SELECT id, first_name, last_name, phone, email FROM Customer ORDER BY id;", connection);

        await using var reader = await command.ExecuteReaderAsync();   // skicka frågorna till MySQL, få svar att läsa (raderna)

        var customers = new List<Customer>();                      // tom lista att fylla, kan bara innehålla customer typ eller ny mapp.   
        while (await reader.ReadAsync())                           // läser en rad i taget till det är slut
        {
            customers.Add(new Customer(                       // fyller i ett kundkort och lägger i listan
                reader.GetInt32("id"),                        // läser id som int
                reader.GetString("first_name"),               // läser first_name som string
                reader.GetString("last_name"),                // läser last_name som string
                reader.GetString("phone"),                    // läser phone som string
                reader.IsDBNull(reader.GetOrdinal("email"))   // är email NULL? //ternary operator
                    ? null                                    //   ja → null
                    : reader.GetString("email")));            //   nej → läser email som string
        }
        return customers;                                          // skicka tillbaka listan
    }

 // Order: skapa en ny kund, returnerar nya kundens id
    public async Task<int> CreateCustomerAsync(string firstName, string lastName, string phone, string? email) // skapar en kund, lovar tillbaka nya kundens id (nästa lediga, t.ex. 6)
    { //Auto_Increment
        await using var command = new MySqlCommand(               // förbereder INSERT-frågan med @luckor (skickas inte än)
            "INSERT INTO Customer (first_name, last_name, phone, email) " +
            "VALUES (@first_name, @last_name, @phone, @email);", connection);

        command.Parameters.AddWithValue("@first_name", firstName); // fyll lucka @first_name
        command.Parameters.AddWithValue("@last_name", lastName);   // fyll lucka @last_name
        command.Parameters.AddWithValue("@phone", phone);          // fyll lucka @phone
        command.Parameters.AddWithValue("@email", (object?)email ?? DBNull.Value); // email om den finns, annars NULL i databasen

        await command.PrepareAsync(); // MySQL förbereder frågan
        await command.ExecuteNonQueryAsync(); // kör INSERT (inga rader att läsa)

        return checked((int)command.LastInsertedId); // id som AUTO_INCREMENT just gav kunden
    }
    
    // Order: finns kunden? true/false
    public async Task<bool> CustomerExistsAsync(int customerId)
    {
        await using var command = new MySqlCommand(
            "SELECT EXISTS (SELECT 1 FROM Customer WHERE id = @id);", connection);   // 1 = finns, 0 = finns inte
        command.Parameters.AddWithValue("@id", customerId);
        await command.PrepareAsync();
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;   // ExecuteScalar = ett enda värde tillbaka
    }
}