
DROP DATABASE IF EXISTS holiday_maker;
CREATE DATABASE holiday_maker;
USE holiday_maker;

CREATE TABLE `Accommodation`(
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `city_id` INT NOT NULL,
    `name` VARCHAR(255) NOT NULL,
    `has_pool` BOOLEAN NOT NULL,
    `has_restaurant` BOOLEAN NOT NULL,
    `distance_to_center_meters` INT NOT NULL,
    `distance_to_beach_meters` INT NOT NULL,
    `rating` INT NOT NULL,
    `description` VARCHAR(255) NULL
);
CREATE TABLE `Room`(
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `accommodation_id` INT NOT NULL,
    `room_number` VARCHAR(255) NOT NULL,
    `capacity` INT NOT NULL,
    `price_per_night` DECIMAL(8, 2) NOT NULL
);
ALTER TABLE
    `Room` ADD UNIQUE `room_accommodation_id_room_number_unique`(`accommodation_id`, `room_number`);
CREATE TABLE `Customer`(
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `first_name` VARCHAR(255) NOT NULL,
    `last_name` VARCHAR(255) NOT NULL,
    `phone` VARCHAR(255) NOT NULL,
    `email` VARCHAR(255) NULL
);
ALTER TABLE
    `Customer` ADD UNIQUE `customer_email_unique`(`email`);
CREATE TABLE `Booking`(
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `customer_id` INT NOT NULL,
    `room_id` INT NOT NULL,
    `number_of_guests` INT NOT NULL,
    `check_in_date` DATE NOT NULL,
    `check_out_date` DATE NOT NULL
);
ALTER TABLE
    `Booking` ADD INDEX `booking_room_id_check_in_date_check_out_date_index`(
        `room_id`,
        `check_in_date`,
        `check_out_date`
    );
CREATE TABLE `City`(
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(255) NOT NULL
);
ALTER TABLE
    `City` ADD UNIQUE `city_name_unique`(`name`);
ALTER TABLE
    `Accommodation` ADD CONSTRAINT `accommodation_city_id_foreign` FOREIGN KEY(`city_id`) REFERENCES `City`(`id`);
ALTER TABLE
    `Room` ADD CONSTRAINT `room_accommodation_id_foreign` FOREIGN KEY(`accommodation_id`) REFERENCES `Accommodation`(`id`);
ALTER TABLE
    `Booking` ADD CONSTRAINT `booking_room_id_foreign` FOREIGN KEY(`room_id`) REFERENCES `Room`(`id`);
ALTER TABLE
    `Booking` ADD CONSTRAINT `booking_customer_id_foreign` FOREIGN KEY(`customer_id`) REFERENCES `Customer`(`id`);

-- CHECK villkor (anteckning från ER diagrammet)

ALTER TABLE Accommodation
    ADD CONSTRAINT check_rating          CHECK (rating BETWEEN 1 AND 5),
    ADD CONSTRAINT check_distance_center CHECK (distance_to_center_meters >= 0),
    ADD CONSTRAINT check_distance_beach  CHECK (distance_to_beach_meters >= 0);

ALTER TABLE Room
    ADD CONSTRAINT check_capacity CHECK (capacity > 0),
    ADD CONSTRAINT check_price    CHECK (price_per_night > 0);

ALTER TABLE Booking
    ADD CONSTRAINT check_guest CHECK (number_of_guests > 0),
    ADD CONSTRAINT check_date  CHECK (check_out_date > check_in_date);

