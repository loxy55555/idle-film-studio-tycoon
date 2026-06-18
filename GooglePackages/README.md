# GooglePackages — Firebase Unity SDK 13.12.0

Coloca aquí los `.tgz` antes de abrir Unity (o tras actualizar `Packages/manifest.json`).

## Archivos requeridos

| Archivo | URL directa |
|---------|-------------|
| `com.google.external-dependency-manager-1.2.187.tgz` | https://dl.google.com/games/registry/unity/com.google.external-dependency-manager/com.google.external-dependency-manager-1.2.187.tgz |
| `com.google.firebase.app-13.12.0.tgz` | https://dl.google.com/games/registry/unity/com.google.firebase.app/com.google.firebase.app-13.12.0.tgz |
| `com.google.firebase.analytics-13.12.0.tgz` | https://dl.google.com/games/registry/unity/com.google.firebase.analytics/com.google.firebase.analytics-13.12.0.tgz |
| `com.google.firebase.crashlytics-13.12.0.tgz` | https://dl.google.com/games/registry/unity/com.google.firebase.crashlytics/com.google.firebase.crashlytics-13.12.0.tgz |

## Descarga rápida (PowerShell, desde la raíz del proyecto)

```powershell
New-Item -ItemType Directory -Force -Path GooglePackages | Out-Null
$baseEdm = "https://dl.google.com/games/registry/unity/com.google.external-dependency-manager"
@(
  "$baseEdm/com.google.external-dependency-manager-1.2.187.tgz",
  "https://dl.google.com/games/registry/unity/com.google.firebase.app/com.google.firebase.app-13.12.0.tgz",
  "https://dl.google.com/games/registry/unity/com.google.firebase.analytics/com.google.firebase.analytics-13.12.0.tgz",
  "https://dl.google.com/games/registry/unity/com.google.firebase.crashlytics/com.google.firebase.crashlytics-13.12.0.tgz"
) | ForEach-Object {
  $name = Split-Path $_ -Leaf
  curl.exe -fL $_ -o "GooglePackages/$name"
}
```

Alternativa: descargar el ZIP completo  
https://dl.google.com/firebase/sdk/unity/firebase_unity_sdk_13.12.0.zip  
y copiar los `.tgz` de la carpeta `tgz/` a `GooglePackages/`.

## Después de copiar los tgz

1. Abrir Unity → resolver paquetes automáticamente.
2. **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
3. Añadir `google-services.json` en `Assets/Firebase/` (ver `Assets/Firebase/README_SETUP.md`).
