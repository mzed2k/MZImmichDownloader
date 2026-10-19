# Changelog

All notable changes to this project will be documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

### Added

- Future enhancements and fixes will be listed here.

---

## [1.0.0] - 2026-10-19

### Added

- Initial public release
- Immich API v3 support
- Download of original assets
- Fast downloads using curl.exe
- Automatic folder organization by year and month
- Timestamp-based filenames
- Configuration via INI file
- Command-line configuration overrides
- Adjustable page size
- Date-based asset filtering
- Created-date filtering
- Taken-date filtering
- File size verification using exifInfo.fileSizeInByte
- Optional disabling of file size verification
- Automatic removal of incomplete downloads
- Retry capability for failed downloads
- Duplicate asset protection
- Configurable logging levels:
  - SILENT
  - NOTICE
  - DEBUG
- Immich server version check
- Help screen and command-line usage information

### Security

- Support for TLS 1.2 connections

### Technical

- Compatible with .NET Framework 4.x
- No external libraries required
- Uses native Windows curl.exe for downloading