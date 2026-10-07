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

      # ==============================
      # CHECKOUT
      # ==============================

      - name: Checkout
        uses: actions/checkout@v4


      # ==============================
      # .NET
      # ==============================

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'


      # ==============================
      # VERSION AUSLESEN
      # ==============================

      - name: Read Version
        id: version
        shell: pwsh
        run: |

          $file = "src/TweakOS/Services/ReleaseService.cs"

          if (!(Test-Path $file)) {
              throw "ReleaseService.cs wurde nicht gefunden."
          }

          $content = Get-Content $file -Raw

          if ($content -match 'CurrentVersion\s*=\s*"([^"]+)"') {

              $version = $matches[1]

          }
          else {

              throw "CurrentVersion wurde nicht gefunden."

          }

          Write-Host "TweakOS Version: $version"

          "version=$version" >> $env:GITHUB_OUTPUT


      # ==============================
      # RELEASE PRÜFEN
      # ==============================

      - name: Check existing release
        id: release_check
        shell: pwsh
        env:
          GH_TOKEN: ${{ github.token }}

        run: |

          $version = "${{ steps.version.outputs.version }}"
          $tag = "v$version"

          Write-Host "Prüfe Release $tag ..."

          # Fehler von gh hier bewusst abfangen
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

          # Wichtig: Der Check darf niemals den Workflow beenden
          exit 0


      # ==============================
      # TWEAKOS ALS EINE EXE BAUEN
      # ==============================

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


      # ==============================
      # EXE PRÜFEN
      # ==============================

      - name: Verify EXE
        if: steps.release_check.outputs.exists != 'true'

        shell: pwsh

        run: |

          $exe = "publish/TweakOS.exe"

          if (!(Test-Path $exe)) {

              Write-Host "Dateien im Publish-Ordner:"

              Get-ChildItem publish

              throw "TweakOS.exe wurde nicht erstellt."

          }

          Write-Host "TweakOS.exe wurde erfolgreich erstellt."

          $size =
            (Get-Item $exe).Length / 1MB

          Write-Host "EXE Größe: $([math]::Round($size,2)) MB"


      # ==============================
      # RELEASE ERSTELLEN
      # ==============================

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

            **TweakOS.exe**

            TweakOS wird als einzelne Windows-EXE ausgeliefert.

            Keine ZIP-Datei erforderlich.

          files: publish/TweakOS.exe

          generate_release_notes: true
