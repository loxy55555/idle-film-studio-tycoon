# Firebase setup — Film Producer Tycoon

## Paquetes (FASE 16.0B)

- Firebase Unity SDK **13.12.0**
- Analytics + Crashlytics + External Dependency Manager **1.2.187**

## Antes del build Android de producción

1. Crear proyecto en [Firebase Console](https://console.firebase.google.com/).
2. Registrar la app Android con el **Application ID** de Unity (*Project Settings → Player → Android*).
3. Descargar `google-services.json` y colocarlo en esta carpeta (`Assets/Firebase/`).
4. En Unity: **Assets → External Dependency Manager → Android Resolver → Force Resolve**.
5. Build Android y verificar en Firebase Console que aparecen eventos y sesiones.

## iOS (si aplica)

Añadir `GoogleService-Info.plist` en `Assets/Firebase/` y resolver dependencias iOS en EDM4U.

## Sin configuración

El juego arranca con normalidad; Firebase registra un warning y la observabilidad queda desactivada hasta que se añada la configuración.
