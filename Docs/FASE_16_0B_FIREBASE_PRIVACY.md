# FASE 16.0B — Firebase: datos recopilados y requisitos legales

> Documentación interna para futura Política de Privacidad y Google Play Data Safety.  
> **No es la política publicada** — solo inventario técnico.

## Paquetes instalados (v13.12.0)

| Paquete UPM | Versión | Función |
|-------------|---------|---------|
| `com.google.firebase.app` | 13.12.0 | Core Firebase |
| `com.google.firebase.analytics` | 13.12.0 | Google Analytics for Firebase |
| `com.google.firebase.crashlytics` | 13.12.0 | Crash reporting |
| `com.google.external-dependency-manager` | 1.2.187 | Resolución dependencias Android/iOS |

**Dependencias nativas (vía EDM4U):**
- Android Firebase BoM ~34.14.0
- iOS Firebase CocoaPods ~12.14.0

---

## 1. Qué datos recopila Firebase Analytics

Datos **automáticos** (Google Analytics for Firebase, siempre que la recopilación esté habilitada):

| Categoría | Ejemplos |
|-----------|----------|
| Identificadores | Firebase Installation ID (FID), App Instance ID |
| Dispositivo | Modelo, OS, idioma, zona horaria, resolución |
| App | Versión, paquete, primera apertura, sesiones |
| Engagement | Tiempo en app, pantallas/eventos, fuente de instalación (si disponible) |
| Red | País/región aproximada (IP derivada, no IP almacenada en informes estándar) |

Datos **custom** enviados por Film Producer Tycoon (eventos mínimos):

| Evento | Parámetros | Contenido |
|--------|------------|-----------|
| `game_start` | — | Arranque tras init Firebase |
| `session_start` | — | Inicio de sesión |
| `session_end` | `duration_sec` | Duración sesión (segundos) |
| `city_unlocked` | `city_id` | Nivel de ciudad desbloqueado (1–8) |
| `star_earned` | `stars_total` | Total de estrellas/Oscars acumuladas |
| `movie_completed` | `movie_id` | ID interno del asset de película (`MovieConfig.name`) |
| `ad_rewarded` | `placement` | ID de placement (`free_diamonds`, `boost_income`, etc.) |
| `iap_purchase` | `product_id` | SKU IAP (`diamonds_100`, `no_ads`, etc.) |

**No se envían:** dinero, diamantes, reputación, progreso detallado, nombres de usuario, email, ubicación GPS, contactos, fotos, ni contenido del save.

---

## 2. Qué datos recopila Firebase Crashlytics

| Categoría | Contenido |
|-----------|-----------|
| Crash reports | Stack trace, tipo de excepción, línea/archivo (símbolos debug si subidos) |
| Dispositivo | Modelo, OS, RAM libre, orientación, batería (estado) |
| App | Versión, build, tiempo desde lanzamiento |
| Logs automáticos | Excepciones no controladas (managed + nativas) |
| Identificadores | Installation ID / sesión Crashlytics (no cuenta de usuario) |

**No se registran logs custom** en esta fase (sin `Crashlytics.Log` excesivo).

---

## 3. Identificadores que utiliza Firebase

| Identificador | Uso | ¿Vinculado a usuario real? |
|---------------|-----|----------------------------|
| Firebase Installation ID (FID) | Analytics, Crashlytics, instalación | No (por dispositivo/instalación) |
| App Instance ID | Analytics | No |
| Google Advertising ID (GAID) | Solo si Ads/attribution activos | **No usado** en esta build (sin AdMob) |
| ID de usuario custom | — | **No configurado** (sin cuentas) |

Los datos se asocian a la **instalación de la app**, no a una cuenta de Film Producer Tycoon.

---

## 4. Google Play Data Safety — declaraciones sugeridas

### Datos que probablemente hay que declarar como **recopilados**

| Tipo (Play Console) | Propósito | Compartido | Opcional |
|---------------------|-----------|------------|----------|
| **Identificadores de dispositivo u otros IDs** | Analytics, diagnóstico de crashes | Con Google (Firebase) | No |
| **Datos de diagnóstico** (crash logs) | Estabilidad / Crashlytics | Con Google | No |
| **Interacciones en la app** (eventos) | Analytics de producto | Con Google | No |
| **Otra actividad en la app** (sesiones, IAP event, ad placement) | Analytics | Con Google | No |

### Datos que **no** se recopilan en esta integración

- Nombre, email, teléfono, dirección
- Ubicación precisa (GPS)
- Fotos, vídeos, archivos del usuario
- Historial de búsqueda
- Mensajes
- Información financiera detallada (solo SKU de producto, no tarjeta ni recibo completo en Analytics)
- Save / progreso del juego en servidor
- Cuentas de usuario / login

### Seguridad y eliminación

- Transmisión: HTTPS (Google)
- Eliminación: política futura debe indicar contacto `support@idlefilmstudio.com` y enlace a Firebase/Google policies
- Los usuarios pueden limitar Analytics en ajustes del dispositivo (Android limit ad tracking / reset advertising ID no aplica sin ads)

### Encriptación en tránsito

Sí (TLS hacia servidores Google/Firebase).

---

## 5. Requisitos operativos antes de lanzamiento

1. **Firebase Console:** crear proyecto y app Android (`com.*` del package).
2. **`google-services.json`:** descargar de Firebase Console y colocar en `Assets/` (raíz o `Assets/Firebase/`).
3. **EDM4U:** en Unity, *Assets → External Dependency Manager → Android Resolver → Force Resolve*.
4. **Crashlytics Android:** verificar plugin Gradle aplicado tras primera build.
5. **Símbolos debug:** subir mapping/symbols para stack traces legibles (Play Console / Firebase).
6. **Política de Privacidad:** publicar URL y enlazar desde Ajustes (fila Privacidad — pendiente UI).
7. **Consentimiento (UE/UK):** evaluar CMP si se activa publicidad personalizada en el futuro; con solo Analytics/Crashlytics puede requerirse banner según jurisdicción.

---

## 6. Comportamiento sin red / sin Firebase

- Si `CheckAndFixDependenciesAsync` falla → warning en log, juego continúa.
- Sin `google-services.json` válido → init falla en dispositivo; juego continúa.
- Eventos se descartan silenciosamente si Analytics no está listo.
- Save, economía, IAP y gameplay no dependen de Firebase.
