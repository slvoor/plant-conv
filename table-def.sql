
--
-- Tabelstructuur voor tabel `Soorten`
--

CREATE TABLE `Soorten` (
  `id` int(11) NOT NULL,
  `naamN` text COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `naamW` text COLLATE utf8mb4_unicode_ci DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Tabelstructuur voor tabel `Waarnemingen`
--

CREATE TABLE `Waarnemingen` (
  `id` int(11) NOT NULL,
  `soort` int(11) DEFAULT NULL,
  `x1` int(11) DEFAULT NULL,
  `y1` int(11) DEFAULT NULL,
  `x2` int(11) DEFAULT NULL,
  `y2` int(11) DEFAULT NULL,
  `datum1` datetime DEFAULT NULL,
  `datum2` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Indexen voor geëxporteerde tabellen
--

--
-- Indexen voor tabel `Soorten`
--
ALTER TABLE `Soorten`
  ADD PRIMARY KEY (`id`);

--
-- Indexen voor tabel `Waarnemingen`
--
ALTER TABLE `Waarnemingen`
  ADD PRIMARY KEY (`id`),
  ADD KEY `IxWaarnemingSoort` (`soort`),
  ADD KEY `IxWaarnemingD1` (`datum1`),
  ADD KEY `IxWaarnemingD2` (`datum2`);

--
-- AUTO_INCREMENT voor geëxporteerde tabellen
--

--
-- AUTO_INCREMENT voor een tabel `Waarnemingen`
--
ALTER TABLE `Waarnemingen`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;
COMMIT;
