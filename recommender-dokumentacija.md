# Sofra — dokumentacija sistema preporuke

> Ovaj dokument mora biti u repozitoriju pri predaji i mora odgovarati implementaciji. Nacrt je preuzet iz odobrene prijave (poglavlje 7); ažurirati ga tokom faze 5 (nazivi klasa, tačne konstante, primjeri).

## 1. Cilj i pristup
Sistem preporučuje gostu jela s menija i za svaku preporuku vraća objašnjenje. Pristup je hibridni: **content-based filtering** (sličnost korisničkog profila i vektora osobina jela) kombinovan s **popularity-based** rangiranjem i prosječnom ocjenom; dodatno **co-occurrence** analiza za sekciju „Često se naručuje uz“.

## 2. Ulazni signali (svi se stvarno bilježe i svi ulaze u bodovanje)
| Signal | Tabela | Težina |
|---|---|---|
| Naručena jela (završene narudžbe) | `OrderItem` | 3,0 |
| Ocjene jela | `Review.Rating` | rating − 3 (−2 … +2) |
| Omiljena jela | `Favorite` | 2,0 |
| Pregled detalja jela | `UserInteraction (View)` | 0,5 |
| Pretraga | `UserInteraction (Search)` | 0,5 po kategoriji/oznaci na koju se pojam mapira |
| Prikaz / klik preporuke | `UserInteraction (RecommendationShown/Clicked)` | ne ulazi u profil; služi za CTR |

Vremenski faktor: interakcije starije od 90 dana imaju težinu × 0,5. Profil se računa iz posljednjih 200 interakcija i kešira 10 minuta (`IMemoryCache`, ključ `profile:{userId}`).

## 3. Vektor osobina jela
Komponente (sve 0/1 osim gdje je navedeno), normalizovane na jediničnu dužinu:
- kategorija — one-hot nad `MenuCategory`
- prehrambene oznake — one-hot nad `DietaryTag`
- cjenovni razred — 3 komponente (nisko / srednje / visoko po tercilima cijena dostupnih jela)
- period dana — `Breakfast`, `Lunch`, `Dinner` flagovi

Vektor se čuva u `MenuItemStats.FeatureVectorJson`. Isti job računa `OrdersLast30Days`, `PopularityScore` (min-max normalizacija broja narudžbi u 30 dana na 0–1) i `AvgRatingNormalized` (`AvgRating / 5`).

## 4. Korisnički profil
`profil = normalize( Σ_i  w_i · t_i · v_i )`, gdje je `v_i` vektor jela iz interakcije `i`, `w_i` težina signala, `t_i` vremenski faktor.

## 5. Bodovanje
Za svako **dostupno** jelo koje gost nije naručio u posljednja 24 h:

```
score(jelo) = 0,60 · cos(profil, v_jelo) + 0,25 · PopularityScore + 0,15 · AvgRatingNormalized
```

Rangiranje po `score`; bira se prvih 6 uz pravilo raznolikosti (najviše 2 jela iz iste kategorije). **Hladni start** (manje od 3 interakcije): `score = 0,6 · PopularityScore + 0,4 · AvgRatingNormalized`, objašnjenje „Popularno ove sedmice“.

## 6. Objašnjenja
Bira se prema komponenti koja je najviše doprinijela kosinusnoj sličnosti, odnosno prema dominantnom članu formule:
| Dominantni doprinos | Tekst |
|---|---|
| kategorija | „Jer često naručujete jela iz kategorije {Kategorija}“ |
| prehrambena oznaka | „Odgovara vašim oznakama: {oznaka}“ |
| najsličnije jelo koje je gost ocijenio 4–5 | „Slično jelu {Jelo} koje ste ocijenili sa {n}★“ |
| omiljeno jelo | „Slično vašim omiljenim jelima“ |
| popularnost | „Među 5 najnaručivanijih jela ove sedmice“ |
| ocjena | „Gosti ga ocjenjuju s {AvgRating} ★“ |
| hladni start | „Popularno ove sedmice“ |

## 7. „Često se naručuje uz“
`MenuItemPair` sadrži broj narudžbi u kojima se par jela pojavio zajedno (A < B). Na detaljima jela vraćaju se 3 para s najvećim `PairCount`, objašnjenje „Gosti koji naruče {X} često uzimaju i {Y}“. Ista matrica koristi se u korpi za prijedlog dopune.

## 8. Upravljanje modelom
- Preračun pokreće događaj `RecommenderRecalculate` (noćno iz `RecommenderScheduleHostedService` u API-ju u 03:00 UTC, ili ručno `POST /api/admin/recommender/recalculate`).
- `Sofra.Worker` obrađuje događaj: računa vektore, popularnost, ocjene i co-occurrence i upisuje `MenuItemStats` / `MenuItemPair` u jednoj transakciji.
- API učitava model u `IMemoryCache` (ključ `recommender:model`) i invalidira ga po završetku preračuna (Worker objavljuje `RecommenderRecalculated`, API osluškuje) ili po isteku 24 h.
- Preporuke se vraćaju bez skupih upita pri zahtjevu: samo profil (iz interakcija) + model iz keša.

## 9. Mjerenje učinka
Svaki prikaz i klik preporuke bilježi se (`RecommendationShown`, `RecommendationClicked`). Statistika prikazuje CTR i udio narudžbi koje sadrže bar jedno preporučeno jelo.

## 10. Implementacija (popuniti u fazi 5)
- Klase: `Recommender/FeatureVectorBuilder.cs`, `Recommender/UserProfileBuilder.cs`, `Recommender/RecommendationService.cs`, `Recommender/ExplanationBuilder.cs`, Worker: `Jobs/RecommenderRecalculateHandler.cs`.
- Endpointi: `GET /api/recommendations`, `POST /api/recommendations/{id}/click`, `GET /api/menu-items/{id}/pairs`.
- Primjer odgovora: _(dodati stvarni JSON)_.
