# sg-ms-forgot-information

Microservicio responsable de los flujos de **recuperación de contraseña**, **cambio de
contraseña**, **cambio de correo** y **cambio de teléfono** de Guardian Escolar, todos
protegidos por códigos de verificación (OTP) enviados por **correo** o **SMS**.

Este servicio **no** almacena datos de usuario ni contraseñas: es dueño únicamente de las
solicitudes de verificación (`ForgotInformation.VerificationRequest`). Los datos de cuenta
siguen siendo propiedad de `iam-service` (contraseña) y `user-management-service`
(correo/teléfono). Ver el diseño completo en
[`schoo-guardian/sg-docs/09-microservices/services/12-forgot-information/`](../schoo-guardian/sg-docs/09-microservices/services/12-forgot-information/).

> **¿Quieres probarlo de punta a punta sin montar `iam-service`/`user-management-service` reales?**
> Ver [TESTING.md](./TESTING.md) — entorno autocontenido con MailHog y WireMock, con cada
> comando ya verificado contra el código real.

---

## Cómo funciona una actualización (resumen de seguridad)

1. **Verificar, luego aplicar, luego consumir.** El código solo se marca como usado
   (`Consumed`) *después* de que la actualización en el servicio dueño de los datos haya
   tenido éxito. Si esa llamada falla, el código sigue siendo válido y el usuario puede
   reintentar sin pedir uno nuevo.
2. **Nunca se confía en el `profileId` del body.** Todos los endpoints autenticados toman el
   perfil desde el claim `sub` del JWT — no es posible modificar la cuenta de otro usuario.
3. **Sin enumeración de usuarios.** `POST /password/forgot` siempre responde igual, exista o
   no el correo.
4. **Códigos de un solo uso, con expiración y límite de intentos**, separados por propósito
   (`PasswordReset`, `ChangePassword`, `ChangeEmail`, `ChangePhone` — un código nunca sirve
   para un propósito distinto al que fue emitido).
5. **Nada sensible en logs ni en eventos**: ni el código, ni la contraseña, ni el correo/teléfono
   nuevo viajan en los eventos de Kafka (ver `events.md` en la carpeta de diseño).

---

## Endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/v1/password/forgot` | Pública | Solicita un código para recuperar contraseña (respuesta genérica siempre) |
| POST | `/api/v1/password/forgot/verify` | Pública | Verifica el código y devuelve un `resetToken` de un solo uso |
| POST | `/api/v1/password/reset` | Pública | Aplica la nueva contraseña usando el `resetToken` |
| POST | `/api/v1/password/change/request-code` | JWT | Envía un código al correo actual (para quien olvidó su contraseña estando logueado) |
| POST | `/api/v1/password/change` | JWT | Cambia la contraseña con la actual **o** con un código (nunca ambos, nunca ninguno) |
| POST | `/api/v1/email/change/request` | JWT | Envía un código al **nuevo** correo |
| POST | `/api/v1/email/change/confirm` | JWT | Verifica el código y aplica el cambio de correo |
| POST | `/api/v1/phone/change/request` | JWT | Envía un código por SMS al **nuevo** teléfono |
| POST | `/api/v1/phone/change/confirm` | JWT | Verifica el código y aplica el cambio de teléfono |
| GET | `/health` | Pública | Health check |

---

## Variables de entorno

Copia `.env.example` a `.env` y completa los valores. Resumen:

| Variable | Propósito |
|----------|-----------|
| `ConnectionStrings__DefaultConnection` | SQL Server compartido (ADR-002) |
| `Jwt__Issuer` / `Jwt__Audience` / `Jwt__PublicKey` | Validación JWT RS256 (ADR-008) — solo la clave **pública** |
| `Otp__*` | Longitud, expiración, intentos máximos y límite de solicitudes del código |
| `Otp__HashPepper` | Secreto del servidor mezclado en el hash del código — generar con `openssl rand -base64 32` |
| `IdentityDirectory__IamServiceBaseUrl` / `UserManagementServiceBaseUrl` | URLs internas de los servicios dueños de los datos |
| `Smtp__*` | Credenciales SMTP para el envío real de códigos por correo |
| `Sms__*` | Credenciales de Twilio para el envío real de códigos por SMS |

---

## Cómo ejecutarlo localmente

```bash
# Con Docker (se une a la red compartida de schoo-guardian)
docker compose up -d --build

# Verificar
curl http://localhost:8091/health
```

```bash
# Sin Docker (requiere .NET 10 SDK instalado)
cd src/ms-forgot-information.Api
dotnet run
```

## Tests

```bash
cd tests/ms-forgot-information.Tests
dotnet test
```

Cubren: emisión y verificación de códigos, expiración, bloqueo por intentos, rate limiting,
no reutilización de un código consumido, anti-enumeración en `password/forgot`, y la regla de
"exactamente uno de contraseña actual o código" en el cambio de contraseña autenticado.

---

## Integración pendiente (fuera del alcance de este servicio)

- `iam-service` y `user-management-service` deben exponer los endpoints REST que
  `IdentityDirectoryHttpClient` consume hoy (`/api/profiles/by-email`,
  `/api/profiles/{id}/validate-password`, `/api/profiles/{id}/password`,
  `/api/persons/by-profile/{id}/contact`, `/api/persons/by-profile/{id}/email`,
  `/api/persons/by-profile/{id}/phone`, `/api/persons/email-exists`, `/api/persons/phone-exists`).
  Según DEC-002 esto debería migrar a gRPC una vez existan los `.proto` — solo esta clase
  cambiaría.
- Integración de los flujos ya existentes en el frontend web (`forgot-password`,
  `change-password`, `change-email`, `change-contact`) y la app móvil (`ForgotPassword`,
  `UpdatePassword`, `UpdateEmail`, `UpdatePhone`), reemplazando la navegación simulada por
  llamadas reales a estos endpoints.
