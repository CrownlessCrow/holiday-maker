# Holiday Maker 🏖️

En konsolapp som heter Holiday Maker med C# där en operatör kan söka lediga hotellrum, boka, ändra och avboka.
All data ligger i en MySQL databas som jag har designat själv.

Individuellt projekt i kursen *Databaser och informationsstrukturer*



## Funktioner

| Menyval | Funktion | Databas |
|---|---|---|
| 1 | Visa kunder | SELECT |
| 2 | Ny kund | INSERT |
| 3 | Sök lediga rum (med filter och sortering) | SELECT + NOT EXISTS |
| 4 | Boka rum | INSERT, SELECT, WHERE NOT EXISTS |
| 5 | Visa en kunds bokningar | SELECT + JOIN |
| 6 | Ändra antal gäster | UPDATE + JOIN |
| 7 | Avboka (med bekräftelse) | DELETE |

**Extra:** (I menyval 3, Sök lediga rum, fråga om man vill ha filter)
- Filtermeny i tre lägen (ja / nej / spelar ingen roll) för pool och restaurang
- Max avstånd till strand och centrum
- Sortering på pris eller betyg
- Ger en sökning 0 träffar på grund av avståndet föreslår appen de närmaste lediga rummen
- Färgkodade meddelanden: 🔴 = fel, 🟢 = lyckat, 🔵 = tips

---

## Teknik

- C# / .NET
- MySqlConnector (async)
- MySQL i Docker
- DataGrip för att testa SQL
- DataGrip och VS Code öppnar samma projektmapp på min dator,
och båda ansluter till samma lokala MySQL-databas (localhost:3306)
- DrawSQL för ER-diagramet
- Mockaroo för att generera testkunder
- Claude AI som handledare under utveckling
- Github för versionshantering

---

## Kom igång

**1. Starta MySQL** (exempel med Docker):

```
Starta MySQL på `localhost:3306` (jag använder Docker)
```

**2. Skapa databasen**, kör filerna i den här ordningen:

```
- 01_schema_databas_tabell.sql   → databas, tabeller och constraints
- 02_testdata.sql                → testdata (städer, hotell, rum, kunder, bokningar)
- 03_queries     → frågorna appen använder, för att LÄSA och TESTA
```

**3. Lägg in lösenordet**

Kopiera `HolidayMaker/appsettings.example.json` och döp kopian till `HolidayMaker/appsettings.json`
och byt `DITT_LÖSENORD` mot ditt lösenord. Filen `appsettings.json` finns i `.gitignore`
och hamnar aldrig på GitHub.

**4. Kör appen**

```
dotnet run --project HolidayMaker
```

---

## Databasen

*ER-Diagram* ![alt text](<drawSQL - HolidayMaker_Martin.png>)

```
Så här hänger tabellerna ihop Exempel: (1 ──<) betyder 1:N,  en till många.
City 1 ──< Accommodation 1 ──< Room 1 ──< Booking >── 1 Customer
```

| Tabell | Innehåll |
|---|---|
| City | Städer (namnet är unikt) |
| Accommodation | Hotell: betyg 1–5, pool, restaurang, avstånd till strand och centrum |
| Room | Rum: rumsnummer, kapacitet, pris per natt |
| Customer | Kunder (e-post är unik) |
| Booking | Kopplar kund och rum med datum och antal gäster |

**Regler i databasen:**
- `PRIMARY KEY` och `FOREIGN KEY` på alla kopplingar
- `UNIQUE`: stadens namn, kundens e-post och en gemensam unik nyckel på (hotell + rumsnummer)
- `CHECK`: betyg 1–5, kapacitet och pris > 0, antal gäster > 0, utcheckning efter incheckning
- `INDEX` på bokningens rum och datum, som används vid varje sökning efter lediga rum

**Datumregel:** utcheckningsdagen räknas som ledig. En bokning 10–13 december
blockerar nätterna 10, 11 och 12, så rummet kan bokas igen från den 13:e.

---

## Struktur

```
Program.cs              → meny, inmatning och utskrift
CustomerRepository.cs   → all SQL för kunder
RoomRepository.cs       → all SQL för sökning
BookingRepository.cs    → all SQL för bokningar
Customer.cs, AvailableRoom.cs, BookingConfirmation.cs, CustomerBooking.cs
                        → records (en "blankett" per typ av data)
```

All SQL ligger i repository-klasserna. Program.cs pratar aldrig direkt med databasen.

`03_queries.sql` innehåller samma SQL-frågor som appen använder.
Jag testade dem först i DataGrip och flyttade sedan in dem i C#.


## Säkerhet

- **SQL-injection:** alla värden skickas som parametrar (`@city`, `@guests` ...) istället för att
  klistras in i SQL-koden. Skriver användaren t.ex. "Halmstad" hamnar det i `@city` och behandlas
  bara som text, aldrig som ett kommando. 
- Jag testade att skriva `x'; DROP TABLE Booking; --` (ett kommando som raderar tabellen) som stad.
Appen svarade "Inga lediga rum hittades" och alla bokningar fanns kvar.
- **Lösenord:** ligger bara i `appsettings.json`, som inte finns på GitHub.
- **Dubbelbokning:** bokningen kontrollerar krockar och sparar i samma SQL-sats.
- **Kapacitet:** både bokning och ändring av gäster kontrollerar att rummet räcker.
- **Sortering:** väljs bland två fasta alternativ i koden, ingen användartext hamnar i `ORDER BY`.
- **Inmatning:** datum, tal och obligatoriska fält valideras i loopar, så appen kraschar inte. (felmeddelande ochj ny fråga vid fel inmatning)

---

## Tester

Alla menyval är testade, både lyckade fall och felfall (27 tester).
Se testdokumentationen i inlämningen.

---

## Kända begränsningar och förbättringar

- Appen loggar in som `root`. En egen databasanvändare med bara SELECT, INSERT,
  UPDATE och DELETE vore säkrare.
- Om två bokningar görs exakt samtidigt kan de i teorin krocka.
  En transaktion som låser rummet (`SELECT FOR UPDATE`) skulle stänga det.
- Operatören måste känna till kund-id och rum-id. Sök på namn eller telefon vore smidigare.
- Det går att söka och boka datum bakåt i tiden.

---


Återkommer 

- Ändring av gäster ger samma felmeddelande oavsett om bokningen saknas eller rummet är för litet.
  Att först kontrollera om bokningen finns skulle ge ett tydligare meddelande.
- Vid avbokning frågar appen "Är du säker?" innan den vet om bokningen finns.
- Om MySQL inte är igång kraschar appen vid start istället för att visa ett tydligt felmeddelande.
- Byte av rum eller datum i en befintlig bokning ingår inte (enligt uppgiften), så operatören
  måste avboka och boka om.