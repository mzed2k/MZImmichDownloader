# MZImmichDownloader

Schneller Kommandozeilen-Downloader für Immich.

MZImmichDownloader lädt Originaldateien direkt von einem Immich-Server herunter und speichert sie in einer chronologischen Ordnerstruktur (`YYYY\MM`).

Der Download erfolgt über `curl.exe`, wodurch deutlich höhere Geschwindigkeiten erzielt werden können als bei vielen klassischen API-Downloadern oder Skriptlösungen.

## Features

- Sehr schneller Download über `curl.exe`
- Unterstützt Immich API v3
- Speicherung nach Jahr und Monat (`YYYY\MM`)
- Dateinamen mit Zeitstempel
- Integritätsprüfung über Dateigröße (`exifInfo.fileSizeInByte`)
- Automatische Wiederholung fehlerhafter Downloads
- Fehlerhafte oder unvollständige Dateien werden gelöscht
- Duplikat-Schutz bei mehrfach referenzierten Assets
- Konfiguration über INI-Datei oder Kommandozeile
- Mehrstufiges Logging:
  - SILENT
  - NOTICE
  - DEBUG
- Keine zusätzlichen Drittanbieter-Bibliotheken erforderlich

## Motivation

Ich brauche eine einfache Möglichkeit, Originaldateien von einem Immich-Server unter Windows herunterzuladen.

Bestehende Lösungen waren oft entweder zu komplex, erforderten zusätzliche Software-Stacks oder ließen sich nicht gut in automatisierte Backup-Workflows integrieren. Ich wollte ein Tool, das einfach auf einen Rechner kopiert, einmalig konfiguriert und dann über ein Skript oder eine geplante Aufgabe ausgeführt werden kann.

Daher wurde MZImmichDownloader mit den folgenden Zielen entwickelt:

- Eine einzige ausführbare Datei
- Keine Installation
- Skriptfähig
- Portabel
- Zuverlässig
- Schnell

Solange Windows die ausführbare Datei ausführen und auf den Immich-Server zugreifen kann, sollte das Tool funktionieren.

Übersetzt mit DeepL.com (kostenlose Version)

## Voraussetzungen

- Windows
- .NET Framework 4.x
- curl.exe im Systempfad
- Immich Server mit gültigem API-Key

## Konfiguration

Die Anwendung sucht automatisch nach einer Datei:

```text
MZImmichDownloader.ini
```

im selben Verzeichnis wie die EXE.

### Beispiel

```ini
ImmichBaseUrl   = https://immich.example.com
ApiKey          = YOUR_API_KEY
OutputDir       = D:\ImmichBackup
PageSize        = 100
DownloadDays    = 14
DateFilterMode  = created
CheckFileSize   = true
LogLevel        = NOTICE
```

## Kommandozeile

### Positionsparameter

```cmd
MZImmichDownloader.exe <ImmichBaseUrl> <ApiKey> <OutputDir> <PageSize>
```

Beispiel:

```cmd
MZImmichDownloader.exe https://immich.local abc123 D:\Fotos 200
```

### Optionen

```text
--debug
--notice
--silent
--quiet

--loglevel:DEBUG
--loglevel:NOTICE
--loglevel:SILENT

--sizecheck:true
--sizecheck:false

--nosizecheck
--no-sizecheck

--help
```

### Beispiele

Debug-Ausgabe aktivieren:

```cmd
MZImmichDownloader.exe --debug
```

Dateigrößenprüfung deaktivieren:

```cmd
MZImmichDownloader.exe --nosizecheck
```

Komplette Konfiguration über Kommandozeile:

```cmd
MZImmichDownloader.exe ^
 https://immich.local ^
 APIKEY ^
 D:\Immich ^
 200 ^
 --debug
```

## Zielstruktur

Beispiel:

```text
D:\ImmichBackup
 ├─ 2024
 │   ├─ 01
 │   ├─ 02
 │   └─ 03
 └─ 2025
```

Dateiname:

```text
2025-08-14_17-52-31_IMG_1234.JPG
```

## Integritätsprüfung

Standardmäßig wird die vom Immich-Server gelieferte Dateigröße mit der heruntergeladenen Datei verglichen.

Bei Abweichungen:

- Datei wird verworfen
- Datei wird gelöscht
- Download wird beim nächsten Lauf erneut versucht

Die Prüfung kann deaktiviert werden:

```ini
CheckFileSize=false
```

oder

```cmd
--nosizecheck
```

## Kompilierung

Visual Basic .NET 4.0:

```cmd
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\vbc.exe" ^
 /target:exe ^
 /out:MZImmichDownloader.exe ^
 /reference:System.Web.Extensions.dll ^
 /win32icon:mz-fimo-logo-blau.ico ^
 MZImmichDownloader.vb AssemblyInfo.vb
```

## Lizenz

Dieses Projekt steht unter der GNU General Public License v3.0 (GPL-3.0).

Details siehe Datei:

```text
LICENSE
```

## Haftungsausschluss

Die Nutzung erfolgt auf eigenes Risiko. Vor produktiven Einsätzen wird ein Testlauf empfohlen.