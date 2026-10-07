# TweakOS v5

Windows-WPF-Tweaking-App mit zentralem Katalog, öffentlicher Website und GitHub-Release-System.

## Release
Änderungen committen → Version erhöhen → Tag `v5.x.x` erstellen → GitHub Actions baut den Release automatisch. Die Website zeigt die veröffentlichten Releases und die App erkennt neue Versionen.

## Kontrolle
Es gibt absichtlich kein Passwort in `admin.html`: Ein Passwort im öffentlichen HTML wäre auslesbar. Nur dein GitHub-Konto mit Schreibrechten kann Releases veröffentlichen.

## Wichtig
Die vorhandenen Tweaks können weitreichende Windows-Änderungen ausführen. Kritische Legacy-Skripte sollten vor öffentlicher Verteilung einzeln geprüft und mit Rollback versehen werden.
