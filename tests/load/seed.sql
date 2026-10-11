-- Datos sintéticos de volumen para las pruebas de carga (RNF-02: 10.000 transformadores).
-- Se ejecuta sobre una base recién migrada. Reproducible: la semilla aleatoria es fija.
SELECT setseed(0.42);

INSERT INTO "Distributors" ("Name", "Type", "CreatedAt", "IsActive")
VALUES ('Edesur', 1, now(), true);

-- Zonas alrededor de ciudades: la mitad de los transformadores en el Gran Santo Domingo.
CREATE TEMP TABLE ciudades (zona text, lat double precision, lng double precision, peso int);
INSERT INTO ciudades VALUES
  ('Distrito Nacional', 18.4800, -69.9300, 25), ('Santo Domingo Este', 18.4900, -69.8500, 15),
  ('Santo Domingo Oeste', 18.4900, -70.0000, 10), ('Santiago', 19.4500, -70.6900, 12),
  ('San Cristóbal', 18.4170, -70.1070, 6), ('La Romana', 18.4270, -68.9730, 5),
  ('San Pedro de Macorís', 18.4610, -69.3090, 5), ('Puerto Plata', 19.7930, -70.6880, 5),
  ('La Vega', 19.2220, -70.5290, 5), ('Higüey', 18.6150, -68.7080, 4),
  ('Baní', 18.2790, -70.3310, 4), ('Barahona', 18.2080, -71.1000, 4);

INSERT INTO "ServiceZones" ("ZoneName", "Municipality", "CreatedAt", "IsActive")
SELECT zona, zona, now(), true FROM ciudades;

-- 10.000 transformadores repartidos según el peso de cada ciudad, a unos ±8 km del centro.
INSERT INTO "Transformers" ("Code", "DistributorId", "ServiceZoneId", "TotalCapacityKva", "AvailableCapacityKva",
                            "Status", "Location", "LastUpdated", "CreatedAt", "IsActive")
SELECT 'TR-' || lpad(n::text, 5, '0'),
       1,
       z."Id",
       total,
       disponible,
       CASE WHEN disponible * 100 / total >= 70 THEN 'Available'
            WHEN disponible * 100 / total >= 30 THEN 'Conditioned'
            WHEN disponible > 0 THEN 'Critical'
            ELSE 'Saturated' END,
       ST_SetSRID(ST_MakePoint(c.lng + (random() - 0.5) * 0.15, c.lat + (random() - 0.5) * 0.15), 4326)::geography,
       now(), now(), true
FROM (
  -- Cada ciudad aparece en la bolsa tantas veces como su peso: elegir al azar de la bolsa respeta los pesos.
  SELECT n,
         (ARRAY[50, 75, 100, 167, 250, 500])[1 + floor(random() * 6)::int]::numeric AS total,
         random() AS r,
         bolsa.zonas[1 + floor(random() * array_length(bolsa.zonas, 1))::int] AS zona_azar
  FROM generate_series(1, 10000) n,
       (SELECT array_agg(zona) AS zonas FROM ciudades, generate_series(1, peso)) bolsa
) t
CROSS JOIN LATERAL (SELECT round((t.total * t.r)::numeric, 2) AS disponible) d
JOIN ciudades c ON c.zona = t.zona_azar
JOIN "ServiceZones" z ON z."ZoneName" = c.zona;

-- 50 instaladoras validadas y 2.000 clientes.
INSERT INTO "InstallerCompanies" ("TradeName", "Rnc", "ValidationStatus", "CreatedAt", "IsActive")
SELECT 'Instaladora Solar ' || n, lpad((100000000 + n)::text, 9, '0'), 'Validated', now(), true
FROM generate_series(1, 50) n;

INSERT INTO customer ("ApplicationUserId", "FullName", "NationalIdOrRnc", "CreatedAt", "IsActive", "IsLegalEntity")
SELECT 'carga-' || n, 'Cliente ' || n, lpad(n::text, 11, '0'), now(), true, false
FROM generate_series(1, 2000) n;

-- 20.000 solicitudes del último año con una distribución de estados realista.
INSERT INTO "ConnectionRequests" ("RequestCode", "CustomerId", "InstallerCompanyId", "DistributorId", "TransformerId",
                                  "RequestedAt", "Status", "RequestType", "UsageType", "TariffType", "InterconnectionType",
                                  "PropertyRelation", "RequestedCapacityKva", "ProjectAddress", "CreatedAt", "IsActive")
SELECT 'REQ-C' || lpad(n::text, 6, '0'),
       (SELECT min("Id") FROM customer) + (n % 2000),
       CASE WHEN n % 3 = 0 THEN NULL ELSE (SELECT min("Id") FROM "InstallerCompanies") + (n % 50) END,
       1,
       (SELECT min("Id") FROM "Transformers") + (n % 10000),
       now() - (random() * 365 || ' days')::interval,
       CASE WHEN p < 0.55 THEN 'Approved' WHEN p < 0.65 THEN 'Rejected' WHEN p < 0.70 THEN 'Cancelled'
            WHEN p < 0.80 THEN 'Received' WHEN p < 0.90 THEN 'UnderReview' WHEN p < 0.95 THEN 'Observed'
            ELSE 'InCorrection' END,
       CASE WHEN n % 4 = 0 THEN 'Connection' ELSE 'NetMetering' END,
       'Residential', 'BT', 'Parallel', 'Owner',
       (ARRAY[3, 5, 8, 10, 15, 25])[1 + (n % 6)],
       'Calle ' || (n % 300) || ' #' || n || ', ' || (ARRAY['Naco', 'Piantini', 'Gazcue', 'Los Prados', 'Bella Vista', 'Arroyo Hondo'])[1 + (n % 6)],
       now(), true
FROM (SELECT n, random() AS p FROM generate_series(1, 20000) n) s;

-- Historial mínimo de las que pasaron por revisión, para los tiempos de los reportes.
INSERT INTO "StateHistories" ("ConnectionRequestId", "PreviousState", "NewState", "CreatedAt", "IsActive")
SELECT "Id", 'Received', 'UnderReview', "RequestedAt" + (1 + random() * 5 || ' days')::interval, true
FROM "ConnectionRequests" WHERE "Status" <> 'Received' AND "Status" <> 'Cancelled';

INSERT INTO "StateHistories" ("ConnectionRequestId", "PreviousState", "NewState", "CreatedAt", "IsActive")
SELECT "Id", 'UnderReview', "Status", "RequestedAt" + (6 + random() * 25 || ' days')::interval, true
FROM "ConnectionRequests" WHERE "Status" IN ('Approved', 'Rejected');

VACUUM ANALYZE;
