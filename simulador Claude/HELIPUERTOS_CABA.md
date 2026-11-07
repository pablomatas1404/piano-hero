# Helipuertos y helipuntos reales de CABA (para misiones de helicóptero)

Investigado 2026-09-20. Fuente oficial: [Gobierno de la Ciudad de Buenos Aires -- Helipuertos y Helipuntos del SAME](https://buenosaires.gob.ar/gcaba_historico/salud/same/infraestructura-same/recurso-edilicio/helipuertos).

Coordenadas geocodificadas con OpenStreetMap Nominatim a partir de la dirección postal -- son una BUENA aproximación (acertaron el hospital/manzana correcta), pero no son el punto exacto de la plataforma de aterrizaje dentro del predio. Cuando armemos cada misión, conviene mirar una vez en un mapa satelital para afinar el punto final si hace falta.

## Base central del SAME (de acá "sale" el helicóptero en las misiones)
- **Helipuerto Monasterio** (sede/base SAME) -- Amancio Alcorta 2195, Nueva Pompeya
  - lat: -34.6563491, lon: -58.4157287

## Helipuertos (con plataforma propia, categoría más grande)
- **Baires Madero** -- Av. España 3250, Puerto Madero/Costanera Sur
  - lat: -34.6177430, lon: -58.3564971
- **Hospital Churruca Visca** -- Uspallata 3400, Parque Patricios
  - lat: -34.6405022, lon: -58.4105220
- **Hospital Santojanni** -- Pilar 950, Mataderos
  - lat: -34.6486894, lon: -58.5150399

## Helipuntos (hospitales con punto de aterrizaje, sin infraestructura de helipuerto completo)
- **Hospital Argerich** -- Av. Almirante Brown y Av. Martín García, La Boca
  - lat: -34.6274564, lon: -58.3653575
- **Hospital Durand** -- Av. Díaz Vélez 5044, Caballito
  - lat: -34.6088669, lon: -58.4380107
- **Hospital Fernández** -- Av. Las Heras 3357, Recoleta/Palermo
  - lat: -34.5814137, lon: -58.4068613
- **Hospital Grierson** -- Av. Gral. Fernández de la Cruz 4402, Villa Lugano
  - lat: -34.6719302, lon: -58.4557883
- **Hospital Penna** -- Pedro Chutro 3380, Nueva Pompeya
  - lat: -34.6432427, lon: -58.4113340
- **Hospital Elizalde** -- Av. Montes de Oca y Finochietto, Barracas
  - lat: -34.6291834, lon: -58.3774437
- **Hospital Pirovano** -- Monroe 3555, Coghlan
  - lat: -34.5641411, lon: -58.4718644
- **Hospital Ramos Mejía** -- Urquiza 609, Balvanera
  - lat: -34.6180444, lon: -58.4101818
- **Hospital Tornú** -- Av. Combatientes de Malvinas 3002, Villa Pueyrredón
  - lat: -34.5860192, lon: -58.4717026
- **Hospital Vélez Sarsfield** -- Pedro Calderón de la Barca 1550, Flores
  - lat: -34.6254601, lon: -58.5078105

## Idea de misiones (para cuando lo armemos)
Cada misión sería: salís de la base SAME (Monasterio) o de un hospital, "levantás" un accidentado (o llegás a buscar uno) y lo llevás a otro hospital -- por ejemplo "Accidente en Av. 9 de Julio, trasladar del lugar al Hospital Argerich" o "Traslado urgente: Hospital Pirovano → Hospital Fernández". Como todos estos puntos ya están geocodificados, se pueden usar directo como origen/destino en el mismo sistema de `destinos`/`aeropuertos_lla` que ya tiene el juego -- misma lógica, otra lista de lugares.

## Próximo paso
Decidime cómo querés que se vea la "misión" en el juego (¿un cartel como el de la Torre pero con guita de recompensa? ¿un menú aparte de "Misiones SAME"?) y lo armamos.
