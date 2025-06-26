# Bust N' Blast – Proiect Unity (Lucrare de Licență)

Acest proiect reprezintă implementarea practică a jocului tip shooter multiplayer, Bust N' Blast.

## Cuprins
- [Cerințe de sistem](#cerințe-de-sistem)
- [Tehnologii utilizate](#tehnologii-utilizate)
- [Instrucțiuni de compilare](#instrucțiuni-de-compilare)
- [Instrucțiuni de instalare și rulare](#instrucțiuni-de-instalare-și-rulare)
- [Structura proiectului](#structura-proiectului)

## Cerințe de sistem

Pentru a compila și rula aplicația local, este necesar:

- Unity Editor versiunea `2022.3.50f1`
- .NET SDK (implicit instalat cu Unity)
- Sistem de operare: Windows 10+ / macOS / Linux (cu suport Unity)
- Spațiu liber pe disc: minim 2 GB
- 8 GB RAM sau mai mult (recomandat)

## Tehnologii utilizate

- **Game Engine**: Unity (versiunea exactă: `2022.3.50f1`)
- **Limbaj de programare**: C# cu .NET versiunea 8+
- **Sistem de control al versiunii**: Git

## Instrucțiuni de compilare

1. **Clonarea repository-ului**

```bash
git clone https://github.com/AdrianCDS/bust_n_blast.git
```

2. **Deschiderea proiectului în Unity**

- Accesăm Unity Hub.
- Se selectează "Add project" și se navighează la directorul în care a fost clonat repository-ul.
- Se selectează versiunea corectă de Unity (menționată anterior).
- Deschidem proiectul.

3. **Compilarea aplicației**

- Se navighează la File → Build Settings în cadrul Unity.
- Este selectată platforma țintă (ex: Windows, MacOS).
- Dacă scena principală nu este deja adăugată, se apasă pe "Add Open Scenes".
- Facem click pe "Build" și se selectează un director de ieșire.
- Se așteaptă finalizarea procesului.

> [!IMPORTANT]
> Dacă scena principală sau toate scenele nu sunt deja adăugate în listă, se apasă pe "Add Open Scenes" la fiecare scenă deschisă.
> - Scena de start este găsită la Assets/Level/Devs/Goran/MainMenuCopy.unity
> - Scena de joc este găsită la Assets/Level/Devs/Adrian/ArenaExtended.unity

## Instrucțiuni de instalare și rulare

### Variante disponibile:

1. **Rulare directă din editor** (pentru dezvoltatori):
- Se apasă Play în Unity pentru a lansa jocul în modul de testare.

2. **Executabil compilat** (pentru utilizatori finali):
- După compilarea jocului, în directorul specificat va fi generat un fișier executabil (.exe pentru Windows).
- Se rulează acest fișier pentru a lansa aplicația.

## Structura proiectului

```bash
Assets/                 # Resursele jocului (scripturi, modele, sunete, etc.)
ProjectSettings/        # Setările proiectului Unity
Packages/               # Pachetele utilizate prin Unity Package Manager
README.md               # Documentația proiectului
```

## Autori

- [**Adrian-Mihai Codăuși**](https://github.com/AdrianCDS)
- [**Goran-Gabriel Codăuși**](https://github.com/goran-cds)
