name: TweakOS Release

on:
  push:
    branches:
      - main

permissions:
  contents: write

jobs:
  build:
    runs-on: windows-latest

    steps:

      # ========================================
      # REPOSITORY
      # ========================================

      - name: Checkout
        uses: actions/checkout@v4


      # ========================================
      # .NET 8
      # ========================================

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'


      # ========================================
      # VERSION AUS RELEASESERVICE LESEN
      # ========================================

      - name: Read Version
        id: version
        shell: pwsh
        run: |

          $file = "src/TweakOS/Services/ReleaseService.cs"

          if (!(Test-Path $file)) {
              throw "ReleaseService.cs wurde nicht gefunden."
          }

          $content = Get-Content $file -Raw

          Write-Host "Suche TweakOS Version..."

          $pattern = 'CurrentVersion\s*=\s*"([^"]+)"'

          if ($content -match $pattern) {

              $version = $matches[1].Trim()

          }
          else {

              Write-Host "CurrentVersion wurde nicht gefunden."
              Write-Host ""
              Write-Host "Gefundener Inhalt:"
              Write-Host $content

              throw "CurrentVersion wurde nicht gefunden."

          }

          if ($version -notmatch '^\d+\.\d+\.\d+$') {

              throw "Ungültige Version: $version"

          }

          Write-Host "TweakOS Version: $version"

          "version=$version" >> $env:GITHUB_OUTPUT


      # ========================================
      # PRÜFEN OB RELEASE BEREITS EXISTIERT
      # ========================================

      - name: Check existing release
        id: release_check
        shell: pwsh
        env:
          GH_TOKEN: ${{ github.token }}

        run: |

          $version = "${{ steps.version.outputs.version }}"

          $tag = "v$version"

          Write-Host "Prüfe Release $tag ..."

          $ErrorActionPreference = "Continue"

          gh release view $tag 2>$null

          if ($LASTEXITCODE -eq 0) {

              Write-Host "Release $tag existiert bereits."

              "exists=true" >> $env:GITHUB_OUTPUT

          }
          else {

              Write-Host "Release $tag existiert noch nicht."

              "exists=false" >> $env:GITHUB_OUTPUT

          }

          exit 0


      # ========================================
      # TWEAKOS BAUEN
      # EINE EINZELNE EXE
      # ========================================

      - name: Build TweakOS
        if: steps.release_check.outputs.exists != 'true'

        run: |

          dotnet publish src/TweakOS/TweakOS.csproj `
            -c Release `
            -r win-x64 `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:IncludeNativeLibrariesForSelfExtract=true `
            -p:EnableCompressionInSingleFile=true `
            -o publish


      # ========================================
      # EXE PRÜFEN
      # ========================================

      - name: Verify EXE
        if: steps.release_check.outputs.exists != 'true'

        shell: pwsh

        run: |

          $exe = "publish/TweakOS.exe"

          if (!(Test-Path $exe)) {

              Write-Host ""
              Write-Host "Dateien im Publish-Ordner:"
              Get-ChildItem publish

              throw "TweakOS.exe wurde nicht erstellt."

          }

          $size =
            (Get-Item $exe).Length / 1MB

          Write-Host ""
          Write-Host "================================"
          Write-Host "TweakOS.exe wurde erstellt!"
          Write-Host "Größe: $([math]::Round($size,2)) MB"
          Write-Host "================================"


      # ========================================
      # GITHUB RELEASE ERSTELLEN
      # ========================================

      - name: Create GitHub Release
        if: steps.release_check.outputs.exists != 'true'

        uses: softprops/action-gh-release@v2

        with:

          tag_name: v${{ steps.version.outputs.version }}

          name: TweakOS v${{ steps.version.outputs.version }}

          body: |

            ## TweakOS v${{ steps.version.outputs.version }}

            Neue Version von TweakOS.

            ### Download

            TweakOS wird als einzelne Windows-EXE ausgeliefert.

            Keine ZIP-Datei erforderlich.

            Dieses Release wurde automatisch von GitHub Actions erstellt.

          files: publish/TweakOS.exe

          generate_release_notes: true
