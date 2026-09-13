# Sofra — informacioni sistem za upravljanje poslovanjem restorana

Seminarski rad · Razvoj softvera II · Fakultet informacijskih tehnologija Mostar · 2025/2026
Sadžid Marić, IB230074

## Komponente

| Folder | Opis |
|---|---|
| `Sofra.API` | ASP.NET Core Web API (.NET 10) — REST, JWT, SignalR, Stripe, PDF izvještaji, RabbitMQ publisher |
| `Sofra.Worker` | .NET Worker Service — RabbitMQ consumer (e-mail, notifikacije, recommender job), zaseban kontejner |
| `Sofra.Shared` | Dijeljeni modeli događaja |
| `sofra_desktop` | Flutter (Windows) — aplikacija za osoblje i menadžment |
| `sofra_mobile` | Flutter (Android) — aplikacija za goste |

## Pokretanje

1. Kopiraj `.env.example` u `.env` i popuni vrijednosti (SQL lozinka, JWT ključ, Stripe test ključevi, SMTP).
2. `docker compose up --build` — podiže SQL Server (baza `230074`), RabbitMQ (UI: http://localhost:15672), API (http://localhost:5000, Swagger: `/swagger`) i Worker. Migracije i seed se izvršavaju automatski pri startu API-ja.
3. Desktop: `cd sofra_desktop && flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000`
4. Mobilna (Android emulator): `cd sofra_mobile && flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000`
5. Stripe webhook lokalno: `stripe listen --forward-to localhost:5000/api/payments/webhook`

## Korisnički podaci

| Kontekst | Korisničko ime | Lozinka |
|---|---|---|
| Desktop | `desktop` | `test` |
| Mobilna | `mobile` | `test` |
| Admin | `admin` | `test` |
| Konobar | `konobar` | `test` |
| Kuhar | `kuhar` | `test` |

Stripe test kartica: `4242 4242 4242 4242`, budući datum, bilo koji CVC.

## Sistem preporuke

Opis algoritma, ulaznih signala i upravljanja modelom: [`recommender-dokumentacija.md`](recommender-dokumentacija.md).