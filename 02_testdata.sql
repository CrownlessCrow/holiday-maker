USE holiday_maker;

INSERT INTO City (name) VALUES
        ('Halmstad'),
        ('Malmö');



INSERT INTO Accommodation (city_id, name, has_pool, has_restaurant,
                        distance_to_center_meters, distance_to_beach_meters,
                        rating, description) VALUES
(1, 'Demacia Palace Hotell', TRUE, TRUE, 1200, 50, 4, 'Nära havet'),
(1, 'Thousand Sunny Hotell', FALSE, TRUE, 100, 3000, 5, 'Nära staden'),
(1, 'Hidden Leaf Hotell', FALSE, FALSE, 300, 2500, 2, 'Enkelt och billigt'),
(2, 'Poke Hotell', TRUE, FALSE, 2000, 400, 4, 'Lugnt vid sjöstranden'),
(2, 'Howls Moving Hotell', TRUE, FALSE, 200, 800, 3, 'Vila och ta det lugnt');

INSERT INTO Room (accommodation_id, room_number, capacity, price_per_night) VALUES
(1, '101', 1, 1095.00),
(1, '102', 2, 1495.00),
(1, '103', 2, 1495.00),
(1, '104', 4, 3495.00),

(2, '101', 1, 1095.00),
(2, '102', 2, 1495.00),
(2, '103', 2, 1595.00),
(2, '104', 4, 2395.00),

(3, '101', 1,  595.00),
(3, '102', 2,  795.00),
(3, '103', 2,  845.00),
(3, '104', 4, 1195.00),

(4, '101', 1, 1195.00),
(4, '102', 2, 1595.00),
(4, '103', 2, 1695.00),
(4, '104', 4, 2495.00),

(5, '101', 1,  895.00),
(5, '102', 2, 1195.00),
(5, '103', 2, 1295.00),
(5, '104', 4, 1895.00);

-- Kunddata genererad med Mockaroo (mockaroo.com), fiktiva personer

insert into Customer (first_name, last_name, email, phone) values ('Ailis', 'Logue', 'alogue0@pbs.org', '442-826-4077');
insert into Customer (first_name, last_name, email, phone) values ('Lauren', 'Pettko', 'lpettko1@fda.gov', '336-130-1951');
insert into Customer (first_name, last_name, email, phone) values ('Vivien', 'Ditchfield', null, '848-605-0747');
insert into Customer (first_name, last_name, email, phone) values ('Liliane', 'Baumer', 'lbaumer3@woothemes.com', '380-756-3427');
insert into Customer (first_name, last_name, email, phone) values ('Cristabel', 'Boddis', 'cboddis4@youtu.be', '316-399-8253');

INSERT into Booking (customer_id, room_id, number_of_guests, check_in_date, check_out_date) VALUES
-- De första 10 är planerade för att kunna demonstrera testfallen.
-- Test: utcheckningsdag (13–16 ska gå), överlapp (11–12 nekas), kapacitet (3 pers nekas, rummet tar 1)
(1,  1, 1, '2026-12-10', '2026-12-13'),
-- Test: omslutande bokning nekas (t.ex. 8–12)
(2,  2, 2, '2026-12-05', '2026-12-20'),
-- Test: sökning utan resultat, alla rum i Malmö upptagna 1–3 dec
(3, 13, 1, '2026-12-01', '2026-12-03'),
(4, 14, 2, '2026-12-01', '2026-12-03'),
(5, 15, 2, '2026-12-01', '2026-12-03'),
(3, 16, 4, '2026-12-01', '2026-12-03'),
(4, 17, 1, '2026-12-01', '2026-12-03'),
(5, 18, 2, '2026-12-01', '2026-12-03'),
(1, 19, 1, '2026-12-01', '2026-12-03'),
(2, 20, 3, '2026-12-01', '2026-12-03'),
-- Övriga bokningar, varierade kunder, hotell, längd och datum
(1,  3, 2, '2026-12-01', '2026-12-04'),
(3,  4, 4, '2026-12-18', '2026-12-24'),
(2,  5, 1, '2026-12-07', '2026-12-09'),
(4,  6, 2, '2026-12-12', '2026-12-15'),
(5,  7, 2, '2026-12-20', '2026-12-27'),
(1,  8, 4, '2026-12-23', '2026-12-27'),
(2,  9, 1, '2026-12-02', '2026-12-05'),
(3, 10, 2, '2026-12-14', '2026-12-16'),
(4, 11, 2, '2026-12-26', '2026-12-31'),
(5, 12, 3, '2026-12-08', '2026-12-11'),
(1, 14, 2, '2026-12-10', '2026-12-12'),
(2, 16, 4, '2026-12-22', '2026-12-26'),
(3, 17, 1, '2026-12-15', '2026-12-17'),
(4, 19, 2, '2026-12-28', '2026-12-31'),
(5, 20, 4, '2026-12-05', '2026-12-08');

