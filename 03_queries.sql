USE holiday_maker;

-- 1. Registrera och välj kund

-- 1a. Välj kund: visa alla kunder.
SELECT id, first_name, last_name, phone, email
FROM Customer;

-- 1b. Test registrera en ny kund.
INSERT INTO Customer (first_name, last_name, phone, email)
VALUES ('Monkey D', 'Luffy', '070-123-1234', 'luffy@onepiece.com');


-- 2 Sök lediga rum.

-- 2a. Alla rum i en ort.
SELECT Accommodation.name, City.name, Room.room_number, Room.capacity, Room.price_per_night
FROM Room
Join Accommodation  ON Room.accommodation_id = Accommodation.id
Join City   ON Accommodation.city_id = City.id
WHERE City.name = 'Halmstad';

-- 2b.  + tillräcklig kapacitet.

SELECT Accommodation.name, City.name, Room.room_number, Room.capacity, Room.price_per_night
FROM Room
Join Accommodation  ON Room.accommodation_id = Accommodation.id
JOIN City   ON Accommodation.city_id = City.id
WHERE City.name = 'Halmstad'
AND Room.capacity >= 2;


-- 2c. TEST 1:   Visar bara lediga rum (NOT EXISTS: visa inte rum som har en krockande
--              bokning mellan 2026-12-10 och 2026-12-13).
--              Bokningar får inte ha någon natt 10, 11 eller 12 dec. Lediga rum som visas: 8/12.
SELECT Accommodation.name, City.name, Room.room_number, Room.capacity, Room.price_per_night
FROM Room
Join Accommodation  ON Room.accommodation_id = Accommodation.id
JOIN City   ON Accommodation.city_id = City.id
WHERE City.name = 'Halmstad'
AND Room.capacity >= 1
AND NOT EXISTS (
    SELECT 1
    FROM Booking
    WHERE Booking.room_id = Room.id
        AND Booking.check_in_date < '2026-12-13'
        AND Booking.check_out_date > '2026-12-10');

-- 2c test 2: Samma sökning, men för datumen 13-16 dec.
-- BEVIS FÖR ATT utcheckningsdagen inte upptar rummet.
-- Demacia Palace 101 är bokat 10-13 dec, men blir nu ledigt för sökningen 13-16 dec
-- eftersom en ny gäst kan checka in samma dag.

SELECT Accommodation.name, City.name, Room.room_number, Room.capacity, Room.price_per_night
FROM Room
JOIN Accommodation ON Room.accommodation_id = Accommodation.id
JOIN City          ON Accommodation.city_id = City.id
WHERE City.name = 'Halmstad'
  AND Room.capacity >= 1
  AND NOT EXISTS (
      SELECT 1
      FROM Booking
      WHERE Booking.room_id = Room.id
        AND Booking.check_in_date  < '2026-12-16'
        AND Booking.check_out_date > '2026-12-13');


-- 2c Kontroll: vem har bokat rummen i Halmstad och när (facit för test A och B)
SELECT Accommodation.name, Room.room_number,
       Customer.first_name, Customer.last_name,
       Booking.check_in_date, Booking.check_out_date
FROM Booking
JOIN Room          ON Booking.room_id = Room.id
JOIN Accommodation ON Room.accommodation_id = Accommodation.id
JOIN City          ON Accommodation.city_id = City.id
JOIN Customer      ON Booking.customer_id = Customer.id
WHERE City.name = 'Halmstad'
ORDER BY Accommodation.name, Room.room_number;


-- Dubbelkolla: vilka bokningar KROCKAR med 10–13 dec i Halmstad? Får fram Demacia Palace 101.
SELECT Accommodation.name, Room.room_number,
       Booking.check_in_date, Booking.check_out_date
FROM Booking
JOIN Room          ON Booking.room_id = Room.id
JOIN Accommodation ON Room.accommodation_id = Accommodation.id
JOIN City          ON Accommodation.city_id = City.id
WHERE City.name = 'Halmstad'
  AND Booking.check_in_date  < '2026-12-13'
  AND Booking.check_out_date > '2026-12-10';

-- 2d. Sökning med frivilliga filter. Samt

SELECT Accommodation.name, City.name, Room.room_number, Room.capacity, Room.price_per_night,
        Accommodation.distance_to_beach_meters, Accommodation.has_pool
FROM Room
JOIN Accommodation   ON Room.accommodation_id = Accommodation.id
JOIN City           ON Accommodation.city_id = City.id
WHERE City.name = 'Halmstad'
    AND Room.capacity >= 1
-- Filter för om det finns pool + 100 meters avstånd eller närmre.
AND Accommodation.has_pool = TRUE
AND Accommodation.distance_to_beach_meters <= 100

 -- Krock kontroll, la även till ASC på price per night för sortera minsta priset först.
  AND NOT EXISTS (
      SELECT 1
      FROM Booking
      WHERE Booking.room_id = Room.id
        AND Booking.check_in_date  < '2026-12-16'
        AND Booking.check_out_date > '2026-12-13')
    ORDER BY Room.price_per_night ASC;

-- 2e Sortering av filtrering av sökresultatet.

SELECT Accommodation.name, City.name, Room.capacity, Room.room_number, Room.price_per_night,
       Accommodation.rating, Accommodation.distance_to_beach_meters, Accommodation.has_pool
FROM Room
JOIN Accommodation ON Room.accommodation_id = Accommodation.id
JOIN City       ON Accommodation.city_id = City.id
WHERE City.name = 'Halmstad'
AND Room.capacity >= 1

-- Testa utan pool och längre än 100m.
  --
AND Accommodation.has_pool = FALSE
AND Accommodation.distance_to_beach_meters >= 100

-- Testade även att använda SELECT * i krock-kontrollen vilket också fungerar,
-- -- men jag väljer SELECT 1 eftersom det är tydligare: bara att raden finns spelar roll, inte vad den innehåller.
AND NOT EXISTS (
        SELECT *
        FROM Booking
        WHERE Booking.room_id = Room.id
        AND Booking.check_in_date < '2026-12-16'
        AND Booking.check_out_date > '2026-12-13')

-- Använder DESC på omdöme eftersom det sorterar resultatet från högst till lägst.
    ORDER BY Accommodation.rating DESC;

-- 3. Skapa en bokning


-- 3. Skapar bokning bara om rummet är ledigt (NOT EXISTS) och gästerna ryms (capacity).
-- Testad: rum 1, 13–16 dec → 1 row affected, bokningen sparades id.26
-- Körde samma igen, bara completed, inget row affected = 0 rader sparade.
-- Vilket betyder att dubbelbokningen nekades.

INSERT INTO Booking (customer_id, room_id, number_of_guests, check_in_date, check_out_date)
SELECT 1, 1, 1, '2026-12-13', '2026-12-16'
FROM Room
WHERE Room.id = 1
  AND Room.capacity >= 1
  AND NOT EXISTS (
      SELECT 1
      FROM Booking
      WHERE Booking.room_id = 1
        AND Booking.check_in_date  < '2026-12-16'
        AND Booking.check_out_date > '2026-12-13');

-- Jag använder LAST_INSERT_ID() för att säkert hämta det nyss skapade boknings-id. Alltså nästa lediga nummer.
-- Med det numret kan jag hämta rätt rad och göra prisuträkningen direkt i databasen.
-- Detta garanterar att jag får rätt kvitto även om det sker flera bokningar samtidigt.
SELECT Booking.id, DATEDIFF(Booking.check_out_date, Booking.check_in_date) * Room.price_per_night AS total_price
FROM Booking
JOIN Room ON Booking.room_id = Room.id
WHERE Booking.id = LAST_INSERT_ID();

-- Hämta en översikt över alla aktuella bokningar
SELECT Booking.id,
       Booking.customer_id AS kund,
       Room.room_number AS rum,
       Room.capacity AS kapacitet,
       Booking.number_of_guests AS gäster,
       Booking.check_in_date AS incheckning,
       Booking.check_out_date AS utcheckning
FROM Booking
JOIN Room ON Booking.room_id = Room.id
ORDER BY Booking.id DESC;



-- 4. Visa Kundens bokningar

-- 4a. Bokningar för kund 1
SELECT id, customer_id, check_in_date, check_out_date, number_of_guests
FROM Booking
WHERE customer_id = 1;

-- 4b. + rummets nummer och pris (JOIN room med pris)

SELECT Booking.id, customer_id, room_number, price_per_night,
       Check_in_date, check_out_date, number_of_guests
FROM Booking
JOIN Room ON Booking.room_id = Room.id
WHERE Booking.customer_id = 1;

-- 4c + boendets namn (Join accommodation alltså hotellets namn.)

SELECT Booking.id, Booking.customer_id, Accommodation.name, room_number, price_per_night,
       check_in_date, check_out_date, number_of_guests
FROM Booking
JOIN Room ON Booking.room_id = Room.id
JOIN Accommodation ON Room.accommodation_id = Accommodation.id
WHERE Booking.customer_id = 1;

-- 4d. Checka kundens bokningar med totalpris (antal nätter * pris) sorterade på datum.

SELECT Booking.id, Accommodation.name,Room.room_number, Room.price_per_night,
        Booking.check_in_date, check_out_date, number_of_guests,
        DATEDIFF(check_out_date, check_in_date) *  price_per_night AS total_price,
        DATEDIFF(check_out_date, check_in_date) AS number_of_nights
FROM Booking
JOIN Room   ON Booking.room_id = Room.id
JOIN Accommodation  ON Room.accommodation_id = Accommodation.id
WHERE Booking.customer_id  = 1
ORDER BY check_in_date;

-- 5 Ändra antal personer, bara om det ryms i rummet (Room.capacity >= antal).
-- Testad: booking 3 (rum för 1) -> 2 gäster nekas, Ingen rad ändrades


UPDATE Booking
JOIN Room ON Booking.room_id = Room.id
SET Booking.number_of_guests = 2
WHERE Booking.id = 3
    AND Room.capacity >= 2;


-- Kontroll: antal gäster jämfört med rummets kapacitet
SELECT Booking.id, Accommodation.name, Room.room_number,
       Booking.number_of_guests, Room.capacity,
       Room.capacity - Booking.number_of_guests AS free_places
FROM Booking
JOIN Room          ON Booking.room_id = Room.id
JOIN Accommodation ON Room.accommodation_id = Accommodation.id
ORDER BY Booking.id;






-- 6 Avboka (Ta bort bokning)
-- Raderade ett par bokningar då det va för 26,27, 28 och 29. Alltså BookingsID.
DELETE FROM Booking Where id = 27;