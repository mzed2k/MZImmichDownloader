# MZImmichDownloader – Bedienungsanleitung

## Überblick

MZImmichDownloader ist ein schneller Kommandozeilen-Downloader für Immich. Er lädt Originaldateien direkt vom Immich-Server herunter und speichert sie in einer chronologischen Ordnerstruktur.

Besondere Merkmale:

- Schneller Download über `curl.exe`
- Unterstützung für Immich API v3
- Automatische Ordnerstruktur nach Jahr und Monat
- Integritätsprüfung über Dateigröße
- Wiederaufnahme bereits heruntergeladener Bibliotheken
- Unterstützung für INI-Datei und Kommandozeilenparameter
- Mehrstufiges Logging (SILENT, NOTICE, DEBUG)

---

# Voraussetzungen

Für den Betrieb werden benötigt:

- Windows
- .NET Framework 4.x
- `curl.exe` im Systempfad
- Immich Server
- Gültiger Immich API-Key

---

# API-Key erstellen

1. Immich öffnen
2. Benutzerprofil öffnen
3. "API Keys" auswählen
4. Neuen API-Key erstellen
5. API-Key kopieren

---

# Installation

## Dateien bereitstellen

Folgende Dateien in ein Verzeichnis kopieren:

```text
MZImmichDownloader.exe
MZImmichDownloader.ini
```

Beispiel:

```text
D:\Tools\MZImmichDownloader\
```

---

# Konfiguration

Die Anwendung liest automatisch eine INI-Datei mit dem gleichen Namen wie die EXE.

Datei:

```text
MZImmichDownloader.ini
```

## Beispielkonfiguration

```ini
ImmichBaseUrl=https://immich.example.com
ApiKey=DEIN_API_KEY
OutputDir=D:\ImmichBackup
PageSize=100
DownloadDays=14
DateFilterMode=created
CheckFileSize=true
LogLevel=NOTICE
```

---

# Konfigurationsparameter

| Parameter | Beschreibung |
|------------|-------------|
| ImmichBaseUrl | URL des Immich-Servers |
| ApiKey | Immich API-Key |
| OutputDir | Zielverzeichnis für Downloads |
| PageSize | Anzahl der Assets pro API-Abfrage |
| DownloadDays | Nur Dateien der letzten X Tage herunterladen |
| DateFilterMode | `created` oder `taken` |
| CheckFileSize | Dateigrößenprüfung aktivieren/deaktivieren |
| LogLevel | SILENT, NOTICE oder DEBUG |

---

# Erster Programmstart

Programm einfach starten:

```cmd
MZImmichDownloader.exe
```

Beispielausgabe:

```text
ImmichBaseUrl : https://immich.example.com
OutputDir     : D:\ImmichBackup
Verbindung erfolgreich! Immich Server Version: 1.x.x
```

Danach beginnt der Download der gefundenen Assets.

---

# Kommandozeilenparameter

INI-Werte können über die Kommandozeile überschrieben werden.

## Positionsparameter

```cmd
MZImmichDownloader.exe <ImmichBaseUrl> <ApiKey> <OutputDir> <PageSize>
```

Beispiel:

```cmd
MZImmichDownloader.exe https://immich.local abc123 D:\Fotos 200
```

---

# Verfügbare Schalter

## Log-Level

Debug-Ausgabe aktivieren:

```cmd
MZImmichDownloader.exe --debug
```

Normale Ausgabe:

```cmd
MZImmichDownloader.exe --notice
```

Minimale Ausgabe:

```cmd
MZImmichDownloader.exe --silent
```

Alternativ:

```cmd
MZImmichDownloader.exe --loglevel:DEBUG
```

---

## Dateigrößenprüfung deaktivieren

```cmd
MZImmichDownloader.exe --nosizecheck
```

oder

```cmd
MZImmichDownloader.exe --sizecheck:false
```

---

## Hilfe anzeigen

```cmd
MZImmichDownloader.exe --help
```

---

# Ordnerstruktur

Die heruntergeladenen Dateien werden automatisch nach Jahr und Monat sortiert.

Beispiel:

```text
D:\ImmichBackup
│
├─ 2024
│  ├─ 01
│  ├─ 02
│  └─ 03
│
└─ 2025
   ├─ 07
   ├─ 08
   └─ 09
```

---

# Dateinamensschema

Dateien werden mit Zeitstempel gespeichert:

```text
2025-08-14_17-52-31_IMG_1234.JPG
```

Dadurch bleiben Aufnahmen chronologisch sortiert.

---

# Integritätsprüfung

Standardmäßig prüft MZImmichDownloader die Dateigröße jeder heruntergeladenen Datei.

Ablauf:

1. Dateigröße vom Immich-Server abrufen
2. Download durchführen
3. Dateigröße vergleichen

Bei Abweichungen:

- Download wird als fehlerhaft erkannt
- Datei wird gelöscht
- Beim nächsten Lauf wird die Datei erneut geladen

---

# Duplikate

Falls Immich mehrere Assets auf dieselbe Zieldatei abbildet, verhindert MZImmichDownloader unnötige Neu-Downloads.

Dadurch werden sogenannte Download-Ping-Pong-Effekte vermieden.

---

# Regelmäßige Ausführung

Für automatische Synchronisation wird die Windows Aufgabenplanung empfohlen.

Beispiel:

```cmd
MZImmichDownloader.exe --silent
```

Tägliche Ausführung genügt in den meisten Umgebungen.

---

# Fehlerbehebung

## Verbindung fehlgeschlagen

Prüfen:

- Ist die Immich-URL korrekt?
- Ist der Server erreichbar?
- Ist der API-Key gültig?
- Funktioniert HTTPS/TLS?

---

## curl.exe nicht gefunden

In einer Eingabeaufforderung testen:

```cmd
curl --version
```

Falls ein Fehler erscheint, muss curl installiert oder zum PATH hinzugefügt werden.

---

## Keine Dateien gefunden

Prüfen:

```ini
DownloadDays=14
```

Möglicherweise liegen keine Dateien im gewählten Zeitraum.

Testweise:

```ini
DownloadDays=0
```

setzen, um alle Assets einzubeziehen.

---

# Best Practices

Empfohlene Einstellungen:

```ini
PageSize=100
CheckFileSize=true
LogLevel=NOTICE
```

Für tägliche automatische Läufe:

```ini
LogLevel=SILENT
```

Für Fehlersuche:

```ini
LogLevel=DEBUG
```

---

# Lizenz

Dieses Programm wird unter der GNU General Public License v3.0 (GPL-3.0) veröffentlicht.

Weitere Informationen befinden sich in der Datei:

```text
LICENSE
```