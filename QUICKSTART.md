# Quick Start Guide

## 1. Create an API Key

In Immich:

1. Open your profile settings
2. Navigate to API Keys
3. Create a new API key
4. Copy the generated key

## 2. Create Configuration File

Create a file named:

```text
MZImmichDownloader.ini
```

Example:

```ini
ImmichBaseUrl=https://immich.example.com
ApiKey=YOUR_API_KEY
OutputDir=D:\ImmichBackup
```

## 3. Start the Downloader

```cmd
MZImmichDownloader.exe
```

## 4. Verify Connection

You should see a message similar to:

```text
Connection successful! Immich Server Version: x.y.z
```

followed by download progress information.

## 5. Automate with Windows Task Scheduler

Example:

```cmd
MZImmichDownloader.exe --silent
```

Recommended schedule:

- Daily
- LogLevel=NOTICE or SILENT
- Dedicated backup directory

## 6. Verify Results

Downloaded files will be stored in:

```text
OutputDir\YYYY\MM
```

Example:

```text
D:\ImmichBackup\2025\08
```

The downloader automatically skips files that have already been successfully downloaded and verified.