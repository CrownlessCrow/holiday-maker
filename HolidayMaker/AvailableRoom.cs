// Ett ledigt rum i sökresultatet, inkl. betyg, faciliteter och avstånd
public sealed record AvailableRoom(int RoomId, string AccommodationName, string City, string RoomNumber, int Capacity,
decimal PricePerNight, int Rating, bool HasPool, bool HasRestaurant, int DistanceToBeach, int DistanceToCenter);