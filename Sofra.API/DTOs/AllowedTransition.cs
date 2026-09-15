namespace Sofra.API.DTOs;

/// <summary>Jedan status u koji prijavljeni korisnik smije prevesti narudzbu/rezervaciju upravo sada -
/// vec provjereno naspram uloge, vlasnistva i (za narudzbe) placanja. Klijent iz ove liste crta dugmad
/// i ne racuna dozvole sam, da nikad ne ponudi prelaz koji bi backend odbio.</summary>
public record AllowedTransition(int Status, string Label);
