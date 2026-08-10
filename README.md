# Near Assist

Ein kleines Dalamud-Plugin für einen manuellen, makrofähigen **Assist auf den nächsten brauchbaren Ally**.

`/nearassist` sucht genau einmal den nächsten lebenden Party-/Alliance-Mitspieler innerhalb der eingestellten Reichweite, dessen aktuelles hartes Kampfziel gültig, lebend, targetbar und kein anderer Ally ist. Anschließend wird dieses Ziel zu deinem normalen Ziel.

Es gibt keine dauerhafte Verfolgung, keine automatisch ausgelöste Fähigkeit, keine Hooks und keine Eingabesimulation.

## Ability-Makro

Beispiel für BRD:

```text
/micon "Burst Shot"
/nearassist
/ac "Burst Shot" <t>
```

Für einen anderen Skill ersetzt du lediglich `Burst Shot` durch den exakten Namen der Fähigkeit. Standardmäßig löscht `/nearassist` bei einem Fehlschlag das alte Ziel, damit die folgende Makrozeile nicht versehentlich auf ein veraltetes Ziel feuert.

Wichtig: FFXIV-Aktionsmakros besitzen nicht dieselbe native Action Queue wie normale Hotbar-Skills. Wenn dir die Queue wichtiger ist, lege nur `/nearassist` auf eine eigene Taste und drücke danach den unveränderten Skill.

## Befehle

| Befehl | Wirkung |
| --- | --- |
| `/nearassist` | Ziel des nächsten brauchbaren Allies übernehmen |
| `/nearassist keep` | Dasselbe, aber altes Ziel bei Fehlschlag behalten |
| `/nearassist range 30` | Maximale Ally-Distanz zwischen 5 und 60 Yalm |
| `/nearassist clear on/off` | Ziel bei Fehlschlag löschen oder behalten |
| `/nearassist feedback on/off` | Chatmeldungen ein-/ausschalten |
| `/nearassist status` | Aktuelle Einstellungen anzeigen |

## Auswahlregeln

- nur aktuelle Party-/Alliance-Mitglieder
- eigener Charakter ausgeschlossen
- tote, nicht geladene oder nicht targetbare Allies ausgeschlossen
- Allies ohne gültiges Ziel werden übersprungen
- Ally-Ziele und tote/nicht targetbare Ziele werden nicht übernommen
- gegnerische Spieler werden in PvP auch dann erkannt, wenn das Spiel ihr Hostile-Flag nicht zuverlässig liefert
- Controller-Softtargets werden beim Wechsel gelöscht, damit das Makro nicht auf ein anderes Ziel ausweicht
- bei mehreren Kandidaten gewinnt die kleinste Weltentfernung

## Build

```powershell
.\Build-Release.ps1
```

Voraussetzungen: .NET 10, Dalamud API 15 und ein aktueller Dalamud-Entwicklungsordner.

## Hinweis zu PvP

Das Plugin verändert ausschließlich dein lokales Ziel nach einem manuellen Befehl. Es ist dennoch für ein persönliches Custom Repository gedacht und nicht für das offizielle Dalamud-Repository. Square Enix untersagt Drittanbieter-Tools grundsätzlich; Nutzung auf eigenes Risiko.

## Lizenz

MIT
