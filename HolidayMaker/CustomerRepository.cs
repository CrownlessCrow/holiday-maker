using MySqlConnector;                                              // verktyg för mySQL

// Roboten för kunder, all SQL för customer tabellen samlas här
public class CustomerRepository(MySqlConnection connection)        // roboten får telefonen (connection)
{
    // Order: hämta alla kunder
    public async Task<List<Customer>> GetCustomersAsync()          // svarar med en lista av kunder, men vi får vänta med den. (lovar en lista)
    {
        await using var command = new MySqlCommand(                // SQL frågan som ska skickas (vänta)
            "SELECT id, first_name, last_name, phone, email FROM Customer ORDER BY id;", connection);

        await using var reader = await command.ExecuteReaderAsync();   // ring MySQL, få svar att läsa (kundlistan)

        var customers = new List<Customer>();                      // tom lista att fylla, kan bara innehålla customer typ.
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
}