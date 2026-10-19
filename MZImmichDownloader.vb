' ==================================================================================
' MZImmichDownloader
' Fast Immich asset downloader for Windows.
'
' Copyright (C) 2026 Martin Zinßer
'
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License
' along with this program. If not, see <https://www.gnu.org/licenses/>.
'
' Project:
' https://github.com/mzed2k/MZImmichDownloader
'
' ==================================================================================
' Laedt Originaldateien aus Immich v3 extrem schnell herunter (aehnlich wie ein PowerShell-Script)
' - Nutzt curl.exe fuer den eigentlichen Download
' - Prueft Integritaet ueber Dateigroesse (exifInfo.fileSizeInByte) - abschaltbar per INI/CLI
' - Unvollstaendige oder fehlerhafte Downloads werden geloescht, damit sie beim naechsten Lauf neu geladen werden
' - Ausgabe-Verbosity ist in drei Stufen steuerbar: SILENT / NOTICE (Standard) / DEBUG
'
' Konfigurierbar ueber:
'   1) MZImmichDownloader.ini (gleicher Dateiname wie die EXE, nur mit .ini-Endung)
'   2) Kommandozeilen-Parameter (ueberschreiben die INI-Werte)
'
' INI-Beispiel:
'   ImmichBaseUrl   = https://immich.example.com
'   ApiKey          = dein_api_key
'   OutputDir       = D:\ImmichSync
'   PageSize        = 100
'   DownloadDays    = 14
'   DateFilterMode  = created
'   CheckFileSize   = true          ; false = Groessenpruefung komplett abschalten
'   LogLevel        = NOTICE        ; SILENT | NOTICE | DEBUG
'
' Kommandozeilen-Parameter:
'   Positionsparameter (wie bisher, optional, ueberschreiben die INI):
'     MZImmichDownloader.exe <ImmichBaseUrl> <ApiKey> <OutputDir> <PageSize>
'
'   Zusaetzliche benannte Schalter (in beliebiger Reihenfolge, ueberall einsetzbar):
'     --loglevel:DEBUG|NOTICE|SILENT   Verbosity explizit setzen
'     --debug                          Kurzform fuer --loglevel:DEBUG
'     --notice                         Kurzform fuer --loglevel:NOTICE
'     --silent  / --quiet              Kurzform fuer --loglevel:SILENT
'     --sizecheck:true|false           Groessenpruefung ein-/ausschalten
'     --nosizecheck / --no-sizecheck   Kurzform fuer --sizecheck:false
'     --help / -h / -?                 Kurzhilfe anzeigen und beenden
'
' ==================================================================================
' KOMPILIEREN MIT VBC (Visual Basic Compiler .NET 4.0 / VB 2010):
'
' "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\vbc.exe" /target:exe /out:MZImmichDownloader.exe /reference:System.Web.Extensions.dll /win32icon:mz-fimo-logo-blau.ico MZImmichDownloader.vb AssemblyInfo.vb
' ==================================================================================

Imports System
Imports System.IO
Imports System.Net
Imports System.Diagnostics
Imports System.Web.Script.Serialization
Imports System.Collections             ' fuer ArrayList
Imports System.Collections.Generic     ' fuer Dictionary(Of String, Object) und List(Of String)


Module MZImmichDownloader

    ' ==================================================================================
    ' Verbosity-Stufen fuer die Konsolenausgabe.
    ' Reihenfolge/Werte sind bewusst so gewaehlt, dass "hoeherer Wert = mehr Ausgabe":
    '   SILENT (0) < NOTICE (1) < DEBUG (2)
    ' So kann man einfach per ">=" pruefen, ob eine Meldung ausgegeben werden soll.
    ' ==================================================================================
    Private Enum LogLevel
        SILENT = 0
        NOTICE = 1
        DEBUG = 2
    End Enum


    ' --- Konfigurationswerte (Defaults, werden von INI und CLI ueberschrieben) -------

    Private ImmichBaseUrl As String = "https://immich.tld"
    Private ApiKey As String = "DEIN_TOKEN_HIER"
    Private OutputDir As String = "C:\test"
    Private PageSize As Integer = 100
    Private DownloadDays As Integer = 14
    Private DateFilterMode As String = "created"
    ' Moegliche Werte: DateFilterMode=created oder DateFilterMode=taken

    ' NEU: Dateigroessenpruefung kann komplett abgeschaltet werden (INI: CheckFileSize=false)
    Private CheckFileSize As Boolean = True

    ' NEU: Aktuelle Verbosity-Stufe der Konsolenausgabe (INI: LogLevel=... / CLI: --loglevel:...)
    Private CurrentLogLevel As LogLevel = LogLevel.NOTICE


    ' ==================================================================================
    ' Logging-Hilfsfunktionen
    ' ==================================================================================

    ' Zentrale Ausgaberoutine: schreibt formatierten Text optional in einer Konsolenfarbe.
    ' Faengt ab, dass bei fehlenden Format-Argumenten geschweifte Klammern (z. B. aus
    ' Exception-Meldungen) faelschlicherweise als Format-Platzhalter interpretiert werden.
    Private Sub WriteFormatted(ByVal color As Nullable(Of ConsoleColor), ByVal format As String, ByVal args() As Object)
        If color.HasValue Then
            Console.ForegroundColor = color.Value
        End If

        If args Is Nothing OrElse args.Length = 0 Then
            Console.WriteLine(format)
        Else
            Console.WriteLine(format, args)
        End If

        If color.HasValue Then
            Console.ResetColor()
        End If
    End Sub

    ' Nur sichtbar bei LogLevel=DEBUG. Fuer technische Detailausgaben (Rohantworten,
    ' interne Zwischenwerte, etc.), die im Normalbetrieb stoeren wuerden.
    Private Sub LogDebug(ByVal format As String, ByVal ParamArray args() As Object)
        If CurrentLogLevel >= LogLevel.DEBUG Then
            WriteFormatted(Nothing, "[DEBUG] " & format, args)
        End If
    End Sub

    ' Sichtbar bei LogLevel=NOTICE (Standard) und DEBUG. Normale Statusmeldungen.
    Private Sub LogNotice(ByVal format As String, ByVal ParamArray args() As Object)
        If CurrentLogLevel >= LogLevel.NOTICE Then
            WriteFormatted(Nothing, format, args)
        End If
    End Sub

    ' Wie LogNotice, aber gelb hervorgehoben fuer Warnungen (z. B. Datei wird neu geladen).
    Private Sub LogWarning(ByVal format As String, ByVal ParamArray args() As Object)
        If CurrentLogLevel >= LogLevel.NOTICE Then
            WriteFormatted(ConsoleColor.Yellow, format, args)
        End If
    End Sub

    ' Wie LogNotice, aber gruen hervorgehoben fuer Erfolgsmeldungen.
    Private Sub LogSuccess(ByVal format As String, ByVal ParamArray args() As Object)
        If CurrentLogLevel >= LogLevel.NOTICE Then
            WriteFormatted(ConsoleColor.Green, format, args)
        End If
    End Sub

    ' Fehler werden IMMER ausgegeben - auch bei LogLevel=SILENT.
    ' SILENT unterdrueckt nur Status-/Debug-Meldungen, niemals Fehler, damit ein
    ' automatisierter Lauf (z. B. Scheduled Task) Probleme nicht stillschweigend verschluckt.
    Private Sub LogError(ByVal format As String, ByVal ParamArray args() As Object)
        WriteFormatted(ConsoleColor.Red, format, args)
    End Sub


    ' ==================================================================================
    ' Parsing-Hilfsfunktionen fuer INI- und Kommandozeilenwerte
    ' ==================================================================================

    ' Versucht, einen Text in eine LogLevel-Stufe umzuwandeln (Gross-/Kleinschreibung egal).
    Private Function TryParseLogLevel(ByVal value As String, ByRef level As LogLevel) As Boolean
        If value Is Nothing Then Return False

        Select Case value.Trim().ToUpperInvariant()
            Case "DEBUG"
                level = LogLevel.DEBUG
                Return True
            Case "NOTICE"
                level = LogLevel.NOTICE
                Return True
            Case "SILENT"
                level = LogLevel.SILENT
                Return True
            Case Else
                Return False
        End Select
    End Function

    ' Versucht, einen Text in einen Boolean umzuwandeln. Akzeptiert gaengige Schreibweisen,
    ' damit die INI-Datei auch von Hand bequem gepflegt werden kann.
    Private Function TryParseFlexibleBool(ByVal value As String, ByRef result As Boolean) As Boolean
        If value Is Nothing Then Return False

        Select Case value.Trim().ToLowerInvariant()
            Case "true", "1", "yes", "ja", "on"
                result = True
                Return True
            Case "false", "0", "no", "nein", "off"
                result = False
                Return True
            Case Else
                Return False
        End Select
    End Function


    ' Laedt Konfiguration aus einer INI-Datei (gleiches Basename wie EXE, aber .ini)
    Private Sub LoadConfigFromIni()
        Try
            Dim exePath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            Dim iniPath As String = System.IO.Path.ChangeExtension(exePath, ".ini")

            If Not System.IO.File.Exists(iniPath) Then
                ' Keine INI vorhanden -> Defaults bleiben
                Return
            End If

            Dim lines() As String = System.IO.File.ReadAllLines(iniPath)

            For Each line As String In lines
                If line Is Nothing Then Continue For
                line = line.Trim()
                If line.Length = 0 Then Continue For
                If line.StartsWith("#") OrElse line.StartsWith(";") Then Continue For

                Dim eqPos As Integer = line.IndexOf("="c)
                If eqPos <= 0 Then Continue For

                Dim key As String = line.Substring(0, eqPos).Trim()
                Dim value As String = line.Substring(eqPos + 1).Trim()

                ' Eventuellen Inline-Kommentar hinter dem Wert abschneiden (z. B. "true ; Kommentar")
                Dim commentPos As Integer = value.IndexOf(";"c)
                If commentPos >= 0 Then
                    value = value.Substring(0, commentPos).Trim()
                End If

                Select Case key.ToLowerInvariant()
                    Case "immichbaseurl"
                        If value.Length > 0 Then
                            ImmichBaseUrl = value.TrimEnd("/"c)
                        End If
                    Case "apikey"
                        If value.Length > 0 Then
                            ApiKey = value
                        End If
                    Case "outputdir"
                        If value.Length > 0 Then
                            OutputDir = value
                        End If
                    Case "pagesize"
                        Dim tmpInt As Integer
                        If Integer.TryParse(value, tmpInt) AndAlso tmpInt > 0 Then
                            PageSize = tmpInt
                        End If
                    Case "downloaddays"
                        Dim tmpInt As Integer
                        If Integer.TryParse(value, tmpInt) AndAlso tmpInt >= 0 Then
                            DownloadDays = tmpInt
                        End If
                    Case "datefiltermode"
                        If value.Length > 0 Then
                            DateFilterMode = value.ToLowerInvariant()
                        End If

                    ' NEU: Dateigroessenpruefung per INI abschaltbar
                    Case "checkfilesize"
                        Dim tmpBool As Boolean
                        If TryParseFlexibleBool(value, tmpBool) Then
                            CheckFileSize = tmpBool
                        Else
                            Console.WriteLine("Warnung: Ungueltiger Wert fuer CheckFileSize in INI: '{0}' (erwartet true/false)", value)
                        End If

                    ' NEU: Verbosity per INI steuerbar (Alias "Verbosity" wird ebenfalls akzeptiert)
                    Case "loglevel", "verbosity"
                        Dim tmpLevel As LogLevel
                        If TryParseLogLevel(value, tmpLevel) Then
                            CurrentLogLevel = tmpLevel
                        Else
                            Console.WriteLine("Warnung: Ungueltiger Wert fuer LogLevel in INI: '{0}' (erwartet DEBUG/NOTICE/SILENT)", value)
                        End If
                End Select
            Next
        Catch ex As Exception
            ' Bei INI-Fehlern: still weiter mit Defaults / bisherigen Werten.
            ' Hier bewusst Console.WriteLine statt LogError, da CurrentLogLevel an dieser
            ' Stelle evtl. noch nicht final gesetzt ist und ein INI-Fehler immer sichtbar sein soll.
            Console.WriteLine("Warnung: Fehler beim Lesen der INI-Datei: " & ex.Message)
        End Try
    End Sub


    ' Gibt eine kurze Hilfe zu den verfuegbaren Kommandozeilen-Parametern aus.
    Private Sub PrintUsage()
        Console.WriteLine("MZImmichDownloader - Laedt Originaldateien aus Immich herunter")
        Console.WriteLine()
        Console.WriteLine("Verwendung:")
        Console.WriteLine("  MZImmichDownloader.exe [ImmichBaseUrl] [ApiKey] [OutputDir] [PageSize] [Schalter...]")
        Console.WriteLine()
        Console.WriteLine("Schalter:")
        Console.WriteLine("  --loglevel:DEBUG|NOTICE|SILENT   Verbosity der Ausgabe setzen")
        Console.WriteLine("  --debug                          Kurzform fuer --loglevel:DEBUG")
        Console.WriteLine("  --notice                         Kurzform fuer --loglevel:NOTICE")
        Console.WriteLine("  --silent / --quiet                Kurzform fuer --loglevel:SILENT")
        Console.WriteLine("  --sizecheck:true|false            Dateigroessenpruefung ein-/ausschalten")
        Console.WriteLine("  --nosizecheck / --no-sizecheck     Kurzform fuer --sizecheck:false")
        Console.WriteLine("  --help / -h / -?                  Diese Hilfe anzeigen")
        Console.WriteLine()
        Console.WriteLine("Alle Werte koennen alternativ dauerhaft in MZImmichDownloader.ini hinterlegt werden.")
    End Sub


    ' Verarbeitet die Kommandozeilen-Argumente:
    '  - erkennt und verarbeitet benannte Schalter (--loglevel, --sizecheck, --help, ...)
    '  - alle uebrigen Argumente werden als Positionsparameter (BaseUrl/ApiKey/OutputDir/PageSize)
    '    in der uebergebenen Liste "positionalArgs" gesammelt
    ' Gibt True zurueck, wenn das Programm danach normal weiterlaufen soll,
    ' und False, wenn es sofort beendet werden soll (z. B. nach --help).
    Private Function ProcessCommandLineArgs(ByVal args() As String, ByVal positionalArgs As List(Of String)) As Boolean
        For Each rawArg As String In args
            If String.IsNullOrEmpty(rawArg) Then Continue For

            Dim lowerArg As String = rawArg.ToLowerInvariant()

            ' --help / -h / -? -> Hilfe anzeigen und Programm beenden
            If lowerArg = "--help" OrElse lowerArg = "-h" OrElse lowerArg = "-?" OrElse lowerArg = "/?" Then
                PrintUsage()
                Return False
            End If

            ' Schalter mit Wert, z. B. "--loglevel:DEBUG" oder "--sizecheck=false"
            Dim sepPos As Integer = rawArg.IndexOfAny(New Char() {":"c, "="c})
            If sepPos > 0 Then
                Dim switchName As String = rawArg.Substring(0, sepPos).ToLowerInvariant()
                Dim switchValue As String = rawArg.Substring(sepPos + 1)

                Select Case switchName
                    Case "--loglevel", "-loglevel", "/loglevel"
                        Dim tmpLevel As LogLevel
                        If TryParseLogLevel(switchValue, tmpLevel) Then
                            CurrentLogLevel = tmpLevel
                        Else
                            LogWarning("Warnung: Ungueltiger Wert fuer --loglevel: '{0}' (erwartet DEBUG/NOTICE/SILENT)", switchValue)
                        End If
                        Continue For

                    Case "--sizecheck", "-sizecheck", "/sizecheck", "--checkfilesize"
                        Dim tmpBool As Boolean
                        If TryParseFlexibleBool(switchValue, tmpBool) Then
                            CheckFileSize = tmpBool
                        Else
                            LogWarning("Warnung: Ungueltiger Wert fuer --sizecheck: '{0}' (erwartet true/false)", switchValue)
                        End If
                        Continue For
                End Select
                ' Kein bekannter Schalter mit "=" oder ":" -> als Positionsparameter durchreichen
                ' (z. B. falls eine URL zufaellig einen Doppelpunkt enthaelt, siehe Sonderfall unten)
            End If

            ' Schalter ohne Wert (reine Flags)
            Select Case lowerArg
                Case "--debug"
                    CurrentLogLevel = LogLevel.DEBUG
                    Continue For
                Case "--notice"
                    CurrentLogLevel = LogLevel.NOTICE
                    Continue For
                Case "--silent", "--quiet"
                    CurrentLogLevel = LogLevel.SILENT
                    Continue For
                Case "--nosizecheck", "--no-sizecheck"
                    CheckFileSize = False
                    Continue For
            End Select

            ' Alles andere ist ein normaler Positionsparameter (BaseUrl/ApiKey/OutputDir/PageSize).
            ' Hinweis: ImmichBaseUrl enthaelt "://" und wuerde sonst durch den obigen ":"-Check
            ' faelschlicherweise als benannter Schalter interpretiert - da switchName dort aber
            ' niemals einem der bekannten Schalternamen entspricht, landet die URL korrekt hier.
            positionalArgs.Add(rawArg)
        Next

        Return True
    End Function


    Sub Main(ByVal args() As String)
        ' TLS 1.2 aktivieren
        System.Net.ServicePointManager.SecurityProtocol = CType(3072, System.Net.SecurityProtocolType)

        ' 1) INI lesen (ueberschreibt Defaults, inkl. CheckFileSize und LogLevel)
        LoadConfigFromIni()

        ' 2) Kommandozeile auswerten: Schalter (--loglevel, --sizecheck, ...) verarbeiten
        '    und die restlichen Argumente als Positionsparameter sammeln.
        Dim positionalArgs As New List(Of String)()
        If Not ProcessCommandLineArgs(args, positionalArgs) Then
            ' z. B. --help wurde angegeben -> Programm sofort beenden
            Return
        End If

        ' 3) Positionsparameter (falls vorhanden) ueberschreiben die bisherigen Werte
        If positionalArgs.Count >= 1 AndAlso Not String.IsNullOrEmpty(positionalArgs(0)) Then
            ImmichBaseUrl = positionalArgs(0).TrimEnd("/"c)
        End If
        If positionalArgs.Count >= 2 AndAlso Not String.IsNullOrEmpty(positionalArgs(1)) Then
            ApiKey = positionalArgs(1)
        End If
        If positionalArgs.Count >= 3 AndAlso Not String.IsNullOrEmpty(positionalArgs(2)) Then
            OutputDir = positionalArgs(2)
        End If
        If positionalArgs.Count >= 4 Then
            Dim tmpInt As Integer
            If Integer.TryParse(positionalArgs(3), tmpInt) AndAlso tmpInt > 0 Then
                PageSize = tmpInt
            End If
        End If

        ' Konfigurationsuebersicht: Grunddaten immer bei NOTICE+, Details nur bei DEBUG
        LogNotice("ImmichBaseUrl : {0}", ImmichBaseUrl)
        LogNotice("OutputDir     : {0}", OutputDir)
        LogNotice("PageSize      : {0}", PageSize)
        LogNotice("DownloadDays  : {0}", DownloadDays)
        LogNotice("DateFilterMode: {0}", DateFilterMode)
        LogNotice("CheckFileSize : {0}", CheckFileSize)
        LogNotice("LogLevel      : {0}", CurrentLogLevel.ToString())
        LogDebug("ApiKey (gekuerzt): {0}...", If(ApiKey.Length > 6, ApiKey.Substring(0, 6), ApiKey))

        ' Zielordner sicherstellen
        If Not Directory.Exists(OutputDir) Then
            LogNotice("Erstelle Zielordner: {0}", OutputDir)
            Directory.CreateDirectory(OutputDir)
        End If

        ' Verbindungstest: /api/server/version
        Try
            Dim versionJson As String = HttpGetJson(ImmichBaseUrl & "/api/server/version")
            LogDebug("Rohantwort /api/server/version: {0}", versionJson)

            Dim serializer As New JavaScriptSerializer()
            Dim versionObj As Object = serializer.DeserializeObject(versionJson)
            Dim dict = CType(versionObj, Dictionary(Of String, Object))

            Dim major As Integer = CInt(dict("major"))
            Dim minor As Integer = CInt(dict("minor"))
            Dim patch As Integer = CInt(dict("patch"))

            LogSuccess("Verbindung erfolgreich! Immich Server Version: {0}.{1}.{2}", major, minor, patch)
        Catch ex As Exception
            LogError("Verbindungsfehler: " & ex.Message)
            Environment.Exit(1)
        End Try

        ' Assets via /search/metadata einlesen
        Dim page As Integer = 1
        Dim assetCountTotal As Integer = 0
        Dim serializer2 As New JavaScriptSerializer()

        ' NEU: Merkt sich pro Lauf, welche Zielpfade bereits erfolgreich heruntergeladen/verifiziert
        ' wurden (Zielpfad -> tatsaechliche Dateigroesse). Hintergrund: Manche Immich-Bibliotheken
        ' enthalten doppelte Assets (zwei unterschiedliche Asset-IDs, die auf denselben Dateinamen
        ' abgebildet werden) oder melden fuer dasselbe Asset zu unterschiedlichen Zeitpunkten
        ' unterschiedliche exifInfo.fileSizeInByte-Werte. Ohne diese Absicherung wuerde das Skript
        ' die Datei dann bei jedem Treffer erneut loeschen und neu laden ("Download-Ping-Pong").
        Dim processedTargetsThisRun As New Dictionary(Of String, Long)(StringComparer.OrdinalIgnoreCase)

        While True
            LogNotice("Hole Assets (Seite {0}, Size={1})...", page, PageSize)

            ' Request-Body: mit withExif = true, damit exifInfo.fileSizeInByte vorhanden ist
            Dim searchBodyDict As New Dictionary(Of String, Object)()

            ' WICHTIG: v3 verwendet "size" statt "take"
            searchBodyDict("size") = PageSize
            searchBodyDict("page") = page
            searchBodyDict("withExif") = True

            ' Optional, falls auch geloeschte/archivierte mitgezogen werden sollen:
            ' searchBodyDict("withDeleted") = True
            ' searchBodyDict("isTrashed") = Nothing
            If DownloadDays > 0 Then
                Dim cutoffUtc As String = DateTime.UtcNow.AddDays(-DownloadDays).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")

                If DateFilterMode = "taken" Then
                    searchBodyDict("takenAfter") = cutoffUtc
                Else
                    searchBodyDict("createdAfter") = cutoffUtc
                End If

                LogNotice("Serverfilter aktiv: {0}After = {1}", DateFilterMode, cutoffUtc)
            End If

            Dim searchBodyJson As String = serializer2.Serialize(searchBodyDict)
            LogDebug("Such-Request-Body: {0}", searchBodyJson)

            Dim responseJson As String
            Try
                responseJson = HttpPostJson(ImmichBaseUrl & "/api/search/metadata", searchBodyJson)
                LogDebug("Antwort erhalten ({0} Zeichen)", responseJson.Length)
            Catch ex As Exception
                LogError("Fehler beim Abrufen der Asset-Liste: " & ex.Message)
                Exit While
            End Try

            Dim responseObj As Object = serializer2.DeserializeObject(responseJson)

            ' Assets-Liste herausziehen (aehnliche Fallback-Logik wie im PowerShell-Skript)
            ' Assets als einfaches Objekt-Array
            Dim assetsItems() As Object = Nothing

            If TypeOf responseObj Is Dictionary(Of String, Object) Then
                Dim root = CType(responseObj, Dictionary(Of String, Object))

                If root.ContainsKey("assets") AndAlso TypeOf root("assets") Is Dictionary(Of String, Object) Then
                    Dim assetsObj = CType(root("assets"), Dictionary(Of String, Object))
                    If assetsObj.ContainsKey("items") Then
                        assetsItems = CType(assetsObj("items"), Object())
                    End If
                ElseIf root.ContainsKey("items") Then
                    assetsItems = CType(root("items"), Object())
                End If

            ElseIf TypeOf responseObj Is Object() Then
                assetsItems = CType(responseObj, Object())
            End If

            If assetsItems Is Nothing OrElse assetsItems.Length = 0 Then
                LogNotice("Keine weiteren Assets gefunden.")
                Exit While
            End If


            For Each assetObj As Object In assetsItems
                Dim asset = CType(assetObj, Dictionary(Of String, Object))
                assetCountTotal += 1

                ' Erwartete Dateigroesse aus exifInfo.fileSizeInByte (wird auch bei
                ' deaktivierter Pruefung ermittelt, ist aber dann nur informativ/fuer DEBUG-Logs)
                Dim expectedSizeNullable As Nullable(Of Long) = GetExpectedSize(asset)

                ' Dateiname & Extension
                Dim origName As String = GetStringField(asset, "originalFileName")
                If String.IsNullOrEmpty(origName) Then origName = "asset.jpg"

                Dim ext As String = Path.GetExtension(origName)
                If String.IsNullOrEmpty(ext) Then
                    Dim originalPath As String = GetStringField(asset, "originalPath")
                    If Not String.IsNullOrEmpty(originalPath) Then
                        ext = Path.GetExtension(originalPath)
                    End If
                End If
                If String.IsNullOrEmpty(ext) Then
                    ext = ".jpg"
                End If

                ' Zeitstempel: localDateTime -> fileCreatedAt -> createdAt
                Dim dateStr As String = GetStringField(asset, "localDateTime")
                If String.IsNullOrEmpty(dateStr) Then dateStr = GetStringField(asset, "fileCreatedAt")
                If String.IsNullOrEmpty(dateStr) Then dateStr = GetStringField(asset, "createdAt")

                Dim timeStamp As String
                Dim subFolder As String

                If Not String.IsNullOrEmpty(dateStr) Then
                    ' 'Z' entfernen, damit DateTime als lokale Zeit behandelt wird
                    Dim cleanDateStr As String = RemoveTrailingZ(dateStr)
                    Dim parsedDate As DateTime
                    Try
                        parsedDate = DateTime.Parse(cleanDateStr)
                    Catch
                        parsedDate = DateTime.Now
                    End Try

                    timeStamp = parsedDate.ToString("yyyy-MM-dd_HH-mm-ss")
                    subFolder = parsedDate.ToString("yyyy\\MM")
                Else
                    timeStamp = "UNBEKANNT"
                    subFolder = "UNBEKANNT"
                End If

                ' Ziel-Unterordner (z. B. D:\ImmichSync\2024\05)
                Dim targetDir As String = Path.Combine(OutputDir, subFolder)
                If Not Directory.Exists(targetDir) Then
                    Directory.CreateDirectory(targetDir)
                End If

                Dim baseName As String = Path.GetFileNameWithoutExtension(origName)
                Dim safeName As String = String.Format("{0}_{1}{2}", timeStamp, baseName, ext)

                ' Ungueltige Dateinamenzeichen durch '_' ersetzen
                Dim invalidChars As Char() = Path.GetInvalidFileNameChars()
                For Each ch As Char In invalidChars
                    safeName = safeName.Replace(ch, "_"c)
                Next

                Dim targetPath As String = Path.Combine(targetDir, safeName)

                ' Bereits vorhandene Datei pruefen
                If File.Exists(targetPath) Then
                    Dim fi As New FileInfo(targetPath)

                    If Not CheckFileSize Then
                        ' NEU: Groessenpruefung deaktiviert -> vorhandene Datei wird ohne
                        ' weitere Pruefung akzeptiert und uebersprungen.
                        LogNotice("[{0}] Bereits vorhanden (Groessenpruefung deaktiviert): {1}\{2}", assetCountTotal, subFolder, safeName)
                        Continue For
                    End If

                    If expectedSizeNullable.HasValue Then
                        Dim expectedSize As Long = expectedSizeNullable.Value
                        If fi.Length = expectedSize Then
                            LogNotice("[{0}] Bereits vorhanden (OK, Groesse passt): {1}\{2}", assetCountTotal, subFolder, safeName)
                            processedTargetsThisRun(targetPath) = fi.Length
                            Continue For
                        ElseIf processedTargetsThisRun.ContainsKey(targetPath) Then
                            ' NEU: Dieser Zielpfad wurde in DIESEM Lauf bereits von einem anderen
                            ' Asset erfolgreich heruntergeladen/verifiziert (typisch bei doppelten
                            ' Assets in Immich, die auf denselben Dateinamen abgebildet werden, oder
                            ' bei inkonsistent gemeldeter exifInfo.fileSizeInByte). Um ein staendiges
                            ' Loeschen/Neuladen derselben Datei zu vermeiden, wird die vorhandene,
                            ' bereits in diesem Lauf verifizierte Datei NICHT angetastet.
                            LogWarning("[{0}] Zielpfad wurde in diesem Lauf bereits von einem anderen Asset heruntergeladen (Groessenangabe weicht ab - vermutlich Duplikat in Immich): {1}\{2}", assetCountTotal, subFolder, safeName)
                            Continue For
                        Else
                            LogWarning("[{0}] Lokale Datei hat falsche Groesse, wird neu geladen: {1}\{2}", assetCountTotal, subFolder, safeName)
                            Try
                                File.Delete(targetPath)
                            Catch ex As Exception
                                LogError("Konnte defekte Datei nicht loeschen: " & ex.Message)
                                Continue For
                            End Try
                        End If
                    Else
                        ' Keine erwartete Groesse vom Server verfuegbar -> vorhandene Datei gilt als OK
                        LogNotice("[{0}] Bereits vorhanden (keine erwartete Groesse verfuegbar, ueberspringe): {1}\{2}", assetCountTotal, subFolder, safeName)
                        processedTargetsThisRun(targetPath) = fi.Length
                        Continue For
                    End If
                End If

                ' Download-URL
                Dim assetId As String = GetStringField(asset, "id")
                Dim downloadUrl As String = String.Format("{0}/api/assets/{1}/original?apiKey={2}", ImmichBaseUrl, assetId, ApiKey)

                LogNotice("[{0}] Lade herunter nach {1}\{2}", assetCountTotal, subFolder, safeName)
                LogDebug("Download-URL: {0}", downloadUrl)

                Try
                    RunCurlDownload(downloadUrl, targetPath)

                    ' Nach dem Download: Datei pruefen
                    If Not File.Exists(targetPath) Then
                        Throw New Exception("Zieldatei wurde nicht erstellt.")
                    End If

                    Dim fiAfter As New FileInfo(targetPath)

                    If CheckFileSize Then
                        If expectedSizeNullable.HasValue Then
                            Dim expectedSize As Long = expectedSizeNullable.Value
                            If fiAfter.Length <> expectedSize Then
                                Throw New Exception(String.Format("Download unvollstaendig: Erwartet {0} Bytes, erhalten {1} Bytes.", expectedSize, fiAfter.Length))
                            End If
                        Else
                            If fiAfter.Length <= 0 Then
                                Throw New Exception("Download scheint leer/unvollstaendig zu sein (0 Bytes).")
                            End If
                        End If
                    Else
                        ' NEU: Groessenpruefung deaktiviert. Es bleibt lediglich eine minimale
                        ' Plausibilitaetspruefung (Datei nicht leer), damit offensichtlich
                        ' kaputte 0-Byte-Downloads trotzdem erkannt werden.
                        If fiAfter.Length <= 0 Then
                            Throw New Exception("Download scheint leer/unvollstaendig zu sein (0 Bytes).")
                        End If
                        LogDebug("Groessenpruefung deaktiviert - Datei akzeptiert ohne Abgleich mit Server-Groesse ({0} Bytes).", fiAfter.Length)
                    End If

                    ' Erfolgreichen Download fuer diesen Lauf vormerken (siehe Duplikat-Absicherung oben)
                    processedTargetsThisRun(targetPath) = fiAfter.Length
                Catch ex As Exception
                    LogError("Fehler beim Download ({0}): {1}", assetId, ex.Message)

                    ' Fehlerhafte Datei loeschen, damit sie beim naechsten Lauf neu geladen wird
                    If File.Exists(targetPath) Then
                        Try
                            File.Delete(targetPath)
                        Catch ex2 As Exception
                            LogWarning("Konnte fehlerhafte Datei nicht loeschen: " & ex2.Message)
                        End Try
                    End If
                End Try
            Next

            If assetsItems.Length < PageSize Then
                Exit While
            End If

            page += 1
        End While

        LogSuccess("Sync abgeschlossen. Verarbeitete Assets: {0}", assetCountTotal)
    End Sub

    ' --- Hilfsfunktionen ----------------------------------------------------

    ' HTTP GET mit x-api-key, Antwort als JSON-String
    Private Function HttpGetJson(ByVal url As String) As String
        Dim req = CType(WebRequest.Create(url), HttpWebRequest)
        req.Method = "GET"
        req.Accept = "application/json"
        req.Headers.Add("x-api-key", ApiKey)

        Using resp = CType(req.GetResponse(), HttpWebResponse)
            Using reader As New StreamReader(resp.GetResponseStream())
                Return reader.ReadToEnd()
            End Using
        End Using
    End Function

    ' HTTP POST mit x-api-key, JSON-Body, Antwort als JSON-String
    Private Function HttpPostJson(ByVal url As String, ByVal jsonBody As String) As String
        Dim req = CType(WebRequest.Create(url), HttpWebRequest)
        req.Method = "POST"
        req.Accept = "application/json"
        req.ContentType = "application/json"
        req.Headers.Add("x-api-key", ApiKey)

        Dim bodyBytes() As Byte = System.Text.Encoding.UTF8.GetBytes(jsonBody)
        req.ContentLength = bodyBytes.Length

        Using reqStream As Stream = req.GetRequestStream()
            reqStream.Write(bodyBytes, 0, bodyBytes.Length)
        End Using

        Using resp = CType(req.GetResponse(), HttpWebResponse)
            Using reader As New StreamReader(resp.GetResponseStream())
                Return reader.ReadToEnd()
            End Using
        End Using
    End Function

    ' Erwartete Dateigroesse aus exifInfo.fileSizeInByte
    Private Function GetExpectedSize(ByVal asset As Dictionary(Of String, Object)) As Nullable(Of Long)
        If asset.ContainsKey("exifInfo") AndAlso asset("exifInfo") IsNot Nothing Then
            Dim exif = CType(asset("exifInfo"), Dictionary(Of String, Object))
            If exif.ContainsKey("fileSizeInByte") AndAlso exif("fileSizeInByte") IsNot Nothing Then
                Dim val As Long
                If Long.TryParse(exif("fileSizeInByte").ToString(), val) Then
                    Return val
                End If
            End If
        End If

        Return Nothing
    End Function

    ' Sicherer Zugriff auf String-Felder im Asset
    Private Function GetStringField(ByVal asset As Dictionary(Of String, Object), ByVal fieldName As String) As String
        If asset.ContainsKey(fieldName) AndAlso asset(fieldName) IsNot Nothing Then
            Return asset(fieldName).ToString()
        End If
        Return String.Empty
    End Function

    ' Entfernt ein abschliessendes 'Z' aus einem ISO-String
    Private Function RemoveTrailingZ(ByVal input As String) As String
        If input.EndsWith("Z") Then
            Return input.Substring(0, input.Length - 1)
        End If
        Return input
    End Function

    ' Startet curl.exe fuer den Download, Fortschritt direkt in der Konsole
    Private Sub RunCurlDownload(ByVal url As String, ByVal targetPath As String)

		Dim curlArgs As String

		If CurrentLogLevel > LogLevel.SILENT Then
			curlArgs =  String.Format("--progress-bar --remove-on-error -o ""{0}"" ""{1}""", targetPath, url)
		Else
			curlArgs = String.Format("-sS --remove-on-error -o ""{0}"" ""{1}""", targetPath, url)
		End If

		' Dim curlArgs As String = String.Format("--progress-bar --remove-on-error -o ""{0}"" ""{1}""", targetPath, url)

        Dim psi As New ProcessStartInfo()
        psi.FileName = "curl.exe"
        psi.Arguments = curlArgs

        ' WICHTIG: keine Redirects, damit die Progress-Bar sichtbar ist.
        ' Bei LogLevel=SILENT waere eine sichtbare curl-Progressbar zwar inkonsequent,
        ' curl schreibt seine Fortschrittsanzeige aber direkt nach stderr der Konsole und
        ' wird bewusst nicht ueber unser Logging geroutet (sonst muesste die Kindprozess-
        ' Ausgabe umgeleitet und zeilenweise geparst werden, was Performance kostet).
        psi.UseShellExecute = False
        psi.RedirectStandardOutput = False
        psi.RedirectStandardError = False
        psi.CreateNoWindow = False   ' damit ggf. kein neues Fenster entsteht, sondern die aktuelle Konsole genutzt wird

        Using proc As Process = Process.Start(psi)
            proc.WaitForExit()

            If proc.ExitCode <> 0 Then
                Throw New Exception(String.Format("curl.exe schlug fehl (ExitCode={0}).", proc.ExitCode))
            End If
        End Using
    End Sub

End Module
