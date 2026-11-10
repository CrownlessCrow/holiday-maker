// kvittot efter en lyckad bokning: bokningsnummer och pris
public sealed record BookingConfirmation(int BookingId, int Nights, decimal PricePerNight, decimal TotalPrice);